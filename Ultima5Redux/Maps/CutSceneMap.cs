using System;
using System.Collections.Generic;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.State;

namespace Ultima5Redux.Maps
{
    public enum GoldDonationState { GoldDonatedSuccessfully, ZeroGoldDonated, AttemptedGoldDonationNotEnoughMoney }
    
    public class ShrineCutSceneState {
        public ShrineReference CurrentShrine { get; set; }
        public bool WasMantraCorrect { get; set; } = false;
        public int HundredsOfGoldDonated { get; set; } = 0;
        public GoldDonationState GoldDonationState { get; set; }
    }
    
    public class CutSceneMap : Map
    {
        public ShrineCutSceneState ShrineCutSceneState { get; }

        public override SmallMapReferences.SingleMapReference CurrentSingleMapReference =>
            GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(
                SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine, 0);

        public override MapUnitPosition CurrentPosition { get; set; } = new(0, 0, 0);

        public override bool IsRepeatingMap => false;

        public override int NumOfXTiles => TheSingleCutOrIntroSceneMapReference.N_MAP_COLS_PER_ROW;
        public override int NumOfYTiles => TheSingleCutOrIntroSceneMapReference.N_MAP_ROWS_PER_MAP;

        public override byte[][] TheMap { get; protected set; }

        public override Maps TheMapType => Maps.CutScene;

        public SingleCutOrIntroSceneMapReference TheSingleCutOrIntroSceneMapReference { get; }

        public CutSceneMap(SingleCutOrIntroSceneMapReference theSingleCutOrIntroSceneMapReference,
            ShrineReference shrineReference) : this(theSingleCutOrIntroSceneMapReference) =>
            ShrineCutSceneState = new ShrineCutSceneState {
                CurrentShrine = shrineReference
            };

        public CutSceneMap(SingleCutOrIntroSceneMapReference theSingleCutOrIntroSceneMapReference) : base(
            SmallMapReferences.SingleMapReference.Location.Combat_resting_shrine, 0) {
            TheSingleCutOrIntroSceneMapReference = theSingleCutOrIntroSceneMapReference;

            TheMap = theSingleCutOrIntroSceneMapReference.GetMap();

            CurrentMapUnits ??= new MapUnitCollection();
            CurrentMapUnits.Clear();
        }

        private readonly Dictionary<string, CutSceneNonPlayerCharacter> _mapUnitsByIdentifier = new();

        public ScriptLineResult ProcessScriptLine(CutOrIntroSceneScriptLine scriptLine) {
            switch (scriptLine.Command) {
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.BoostKarmaByMoney:
                    int nTotalGold = ShrineCutSceneState.HundredsOfGoldDonated * 100;
                    if (nTotalGold == 0) {
                        // donated, but entered zero gp
                        ShrineCutSceneState.GoldDonationState = GoldDonationState.ZeroGoldDonated;
                    }
                    else if (GameStateReference.State.PlayerInventory.Gold < nTotalGold) {
                        ShrineCutSceneState.GoldDonationState = GoldDonationState.AttemptedGoldDonationNotEnoughMoney;
                    }
                    else {
                        GameStateReference.State.ChangeKarma(ShrineCutSceneState.HundredsOfGoldDonated,
                            new TurnResults());
                        ShrineCutSceneState.GoldDonationState = GoldDonationState.GoldDonatedSuccessfully;
                    }
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.PromptShrineGold:
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.ChangeShrineState:
                    // change it man
                    var newShrineStatus =
                        (ShrineState.ShrineStatus)Enum.Parse(typeof(ShrineState.ShrineStatus), scriptLine.StrParam);
                    if (TheSingleCutOrIntroSceneMapReference.TheCutOrIntroSceneMapType ==
                        SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType.ShrineOfTheCodexInterior) {
                        // we are in codex, so we will select the appropriate shrine
                        ShrineState shrineState = GetNextToBeOrdainedWithCodexShrine();
                        shrineState.TheShrineStatus = newShrineStatus;
                    }
                    else {
                        ShrineState shrineState = GetShrineState(ShrineCutSceneState.CurrentShrine
                            .VirtueRef.Virtue);
                        shrineState.TheShrineStatus = newShrineStatus;
                    }

                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.CreateMapunit:
                    var mapUnit = new CutSceneNonPlayerCharacter(scriptLine.Visible, scriptLine.TileReference) {
                        MapUnitPosition = {
                            XY = scriptLine.Position
                        }
                    };

                    mapUnit.SetActive(scriptLine.Visible);
                    // we maintain a specific identifier for the mapunit going forward
                    _mapUnitsByIdentifier.Add(scriptLine.StrParam, mapUnit);
                    CurrentMapUnits.Add(mapUnit);
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.MoveMapunit:
                    if (!_mapUnitsByIdentifier.ContainsKey(scriptLine.StrParam)) {
                        throw new Ultima5ReduxException(
                            $"{scriptLine.Command} command issued for mapunit \"{scriptLine.StrParam}\", but does not appear to be created yet.");
                    }

                    CutSceneNonPlayerCharacter mapUnitToMove = _mapUnitsByIdentifier[scriptLine.StrParam];
                    mapUnitToMove.MapUnitPosition.XY = scriptLine.Position;
                    mapUnitToMove.SetActive(scriptLine.Visible);
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.BoostStats:
                    ShrineCutSceneState.CurrentShrine.BoostStats(GameStateReference.State.CharacterRecords.AvatarRecord
                        .Stats);
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.ScreenEffect:
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.SoundEffect:
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Pause:
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.PromptVirtueMeditate:
                    if (ShrineCutSceneState?.CurrentShrine == null) {
                        throw new Ultima5ReduxException(
                            "Unexpected null value for _shrineCutSceneState when trying to PromptVirtueMeditate");
                    }
                    // Something must be done externally for this...
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.EndSequence:
                    // Close the map and return to whence you came
                    return new ScriptLineResult(ScriptLineResult.Result.EndSequence);
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Comment:
                    // we will always do nothing here, it's just for the readability of the script
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Output:
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Goto:
                    return new ScriptLineResult(ScriptLineResult.Result.Goto, scriptLine.IntParam);
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.GotoIf:
                    var gotoDetails = new Goto(scriptLine);
                    int nGotoLine = ProcessGoto(gotoDetails);
                    if (nGotoLine != -1) return new ScriptLineResult(ScriptLineResult.Result.GotoIf, nGotoLine);
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.PromptMantra:
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.NoOp:
                    break;
                case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.OutputModalText:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new ScriptLineResult(ScriptLineResult.Result.Continue);
        }

        private static ShrineState GetNextToBeOrdainedWithCodexShrine() {
            ShrineState nextShrine = GameStateReference.State.TheShrineStates.GetNextOrdainedButNotWithCodex();
            return nextShrine ?? GameStateReference.State.TheShrineStates.GetNextOrdainedWithCodex();
        }
        
        private ShrineState GetShrineState(VirtueReference.VirtueType virtueType) =>
            ShrineCutSceneState?.CurrentShrine != null
                ? GameStateReference.State.TheShrineStates.GetShrineStateByVirtue(virtueType)
                : null;

        private int ProcessGoto(Goto gotoResult) {
            string gotoConditionStr = gotoResult.TheGotoCondition.ToString();
            bool bStartsValidPrefix = gotoConditionStr.StartsWith("ShrineStatus_");
            bool bEndRequiresNoShrine = gotoConditionStr.ToLower().EndsWith("_any") ||
                                        gotoConditionStr.ToLower().EndsWith("_all");

            ShrineState shrineState = null;
            // TODO: ewwww
            if (bStartsValidPrefix && bEndRequiresNoShrine) {
                _ = "";
            }
            else if ((bStartsValidPrefix && ShrineCutSceneState == null) || ShrineCutSceneState.CurrentShrine == null) {
                throw new Ultima5ReduxException(
                    "For ShrineStatus_ type GotoCondition, expected shrine state to have already been tracked.");
            }
            else {
                shrineState = GetShrineState(ShrineCutSceneState.CurrentShrine.VirtueRef.Virtue);
            }

            switch (gotoResult.TheGotoCondition) {
                case Goto.GotoCondition.None:
                    return gotoResult.LineNumber;
                case Goto.GotoCondition.GaveNoMoney:
                    if (ShrineCutSceneState.GoldDonationState == GoldDonationState.ZeroGoldDonated) {
                        return gotoResult.LineNumber;
                    }
                    break;
                case Goto.GotoCondition.HasNotEnoughMoney:
                    // if gold was NOT donated, then we know we didn't have enough
                    if (ShrineCutSceneState.GoldDonationState ==
                        GoldDonationState.AttemptedGoldDonationNotEnoughMoney) {
                        return gotoResult.LineNumber;
                    }
                    break;
                case Goto.GotoCondition.BadMantra:
                    if (ShrineCutSceneState is { WasMantraCorrect: false }) return gotoResult.LineNumber;
                    break;
                case Goto.GotoCondition.ShrineStatus_QuestNotStarted:
                    if (shrineState?.TheShrineStatus == ShrineState.ShrineStatus.QuestNotStarted) {
                        return gotoResult.LineNumber;
                    }
                    break;
                case Goto.GotoCondition.ShrineStatus_ShrineOrdainedNoCodex:
                    if (shrineState?.TheShrineStatus == ShrineState.ShrineStatus.ShrineOrdainedNoCodex) {
                        return gotoResult.LineNumber;
                    }
                    break;
                case Goto.GotoCondition.ShrineStatus_ShrineOrdainedWithCodex:
                    if (shrineState?.TheShrineStatus == ShrineState.ShrineStatus.ShrineOrdainedWithCodex) {
                        return gotoResult.LineNumber;
                    }
                    break;
                case Goto.GotoCondition.ShrineStatus_ShrineCompleted:
                    if (shrineState?.TheShrineStatus == ShrineState.ShrineStatus.ShrineCompleted) {
                        return gotoResult.LineNumber;
                    }
                    break;
                case Goto.GotoCondition.ShrineStatus_QuestNotStarted_All:
                    if (GameStateReference.State.TheShrineStates.AreNoQuestsStartedYet)
                        return gotoResult.LineNumber;
                    break;
                case Goto.GotoCondition.ShrineStatus_ShrineOrdainedNoCodex_Any:
                    if (GameStateReference.State.TheShrineStates.AtLeastOneOrdainedNoCodex)
                        return gotoResult.LineNumber;
                    break;
                case Goto.GotoCondition.ShrineStatus_ShrineOrdainedWithCodex_Any:
                    if (GameStateReference.State.TheShrineStates.AtLeastOneOrdainedWithCodex)
                        return gotoResult.LineNumber;
                    break;
                case Goto.GotoCondition.ShrineStatus_ShrineCompleted_All:
                    if (GameStateReference.State.TheShrineStates.AllShrinesCompleted)
                        return gotoResult.LineNumber;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return -1;
        }

        protected override Dictionary<Point2D, TileOverrideReference> XyOverrides { get; } = new();
        internal override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit) => throw new NotImplementedException();

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit) {
        }

        protected override float GetAStarWeight(in Point2D xy) => throw new NotImplementedException();

        protected override void SetMaxVisibleArea(in Point2D startPos, int nVisibleTiles) {
            _ = "";
        }
    }
}