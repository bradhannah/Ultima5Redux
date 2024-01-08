using System;
using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class Goto : CutOrIntroSceneScriptLineTurnResult {
        public GotoCondition TheGotoCondition { get; }

        public int LineNumber { get; }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum GotoCondition {
            None, BadMantra, ShrineStatus_QuestNotStarted, ShrineStatus_ShrineOrdainedNoCodex,
            ShrineStatus_ShrineOrdainedWithCodex, ShrineStatus_ShrineCompleted, HasNotEnoughMoney, GaveNoMoney,
            ShrineStatus_QuestNotStarted_All, ShrineStatus_ShrineOrdainedNoCodex_Any,
            ShrineStatus_ShrineOrdainedWithCodex_Any, ShrineStatus_ShrineCompleted_All
        }

        public Goto(CutOrIntroSceneScriptLine scriptLine) :
            base(TurnResultType.Script_GotoIf, TurnResulActionType.ActionRequired, scriptLine) {
            if (scriptLine.Command is not CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Goto
                and not CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.GotoIf) {
                throw new Ultima5ReduxException($"Excepted a Goto or GotoIf, but got {scriptLine.Command}");
            }

            if (scriptLine.Command == CutOrIntroSceneScriptLine.CutOrIntroSceneScriptLineCommand.Goto) {
                TheGotoCondition = GotoCondition.None;
                LineNumber = scriptLine.IntParam;
                return;
            }

            bool bSuccess = Enum.TryParse(scriptLine.StrParam, out GotoCondition gotoCondition);
            if (!bSuccess) {
                throw new Ultima5ReduxException($"Received unexpected GotoCondition = {scriptLine.StrParam}");
            }

            TheGotoCondition = gotoCondition;
            LineNumber = scriptLine.IntParam;
        }
    }
}