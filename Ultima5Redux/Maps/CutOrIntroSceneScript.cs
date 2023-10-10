using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults;
using Ultima5Redux.Properties;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public class CutOrIntroSceneScriptLine
    {
        public enum CutOrIntroSceneScriptLineCommand
        {
            CreateMapunit, MoveMapunit, PromptVirtueMeditate, EndSequence, Comment, Output
        }

        // "FrameNum": 0,
        // "Command": "Comment",
        // "StrParam": "",
        // "IntParam": null,
        // "X": 0,
        // "Y": 0,
        // "Visible": false,
        // "Comment": "---- TEST Shrine of Virtue Cut Scene"
        [DataMember] public int FrameNum { get; private set; }
        [DataMember] public CutOrIntroSceneScriptLineCommand Command { get; private set; }
        [DataMember] public string StrParam { get; private set; } = string.Empty;
        [DataMember(EmitDefaultValue = true)] public int IntParam { get; private set; } = -1;
        [DataMember] public int X { get; private set; }
        [DataMember] public int Y { get; private set; }
        [DataMember] public bool Visible { get; private set; }
        [DataMember] public string Comment { get; private set; }

        [IgnoreDataMember]
        public TileReference TileReference => GameReferences.Instance.SpriteTileReferences.GetTileReference(IntParam);

        [IgnoreDataMember] public Point2D Position => new(X, Y);
    }

    [DataContract] public class CutOrIntroSceneScripts
    {
        private readonly Dictionary<SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType, CutOrIntroSceneScript>
            _scripts = new();

        public CutOrIntroSceneScript GetScriptByCutOrIntroSceneMapType(
            SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType cutOrIntroSceneMapType) =>
            _scripts[cutOrIntroSceneMapType];

        public CutOrIntroSceneScripts() {
            Dictionary<SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType, List<CutOrIntroSceneScriptLine>>
                scripts =
                    JsonConvert.DeserializeObject<Dictionary<SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType
                        , List<CutOrIntroSceneScriptLine>>>(Resources.CutSceneScripts);
            //SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType
            foreach (KeyValuePair<SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType,
                         List<CutOrIntroSceneScriptLine>> kvp in scripts) {
                _scripts.Add(kvp.Key, new CutOrIntroSceneScript(kvp.Key, kvp.Value));
            }
        }
    }

    [DataContract] public class CutOrIntroSceneScript
    {
        public SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType MapType { get; }

        public IEnumerable<CutOrIntroSceneScriptLine> ScriptLines => _scriptLines;

        private readonly List<CutOrIntroSceneScriptLine> _scriptLines;

        public TurnResults GenerateTurnResultsFromFrame(int nFrame, ShrineReference shrineReference = null) {
            TurnResults turnResults = new();
            IEnumerable<CutOrIntroSceneScriptLine> scriptLinesInFrame = _scriptLines.Where(i => i.FrameNum == nFrame);
            foreach (CutOrIntroSceneScriptLine scriptLine in scriptLinesInFrame) {
                switch (scriptLine.Command) {
                    case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.CreateMapunit:
                        turnResults.PushTurnResult(new CreateMapUnit(scriptLine));
                        break;
                    case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.MoveMapunit:
                        turnResults.PushTurnResult(new MapUnitMove(scriptLine));
                        break;
                    case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.PromptVirtueMeditate:
                        turnResults.PushTurnResult(new PromptVirtueMeditate(scriptLine,
                            shrineReference.TheVirtue));
                        break;
                    case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.EndSequence:
                        turnResults.PushTurnResult(new ExitBuilding(scriptLine));
                        break;
                    case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Comment:
                        // do nothing - this is just to keep track
                        break;
                    case CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Output:
                        turnResults.PushOutputToConsole(scriptLine.StrParam, false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return turnResults;
        }

        internal CutOrIntroSceneScript(SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType mapType,
            List<CutOrIntroSceneScriptLine> scriptLines) {
            MapType = mapType;
            _scriptLines = scriptLines;
        }
    }
}