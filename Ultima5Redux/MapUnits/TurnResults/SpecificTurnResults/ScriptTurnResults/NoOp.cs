using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class NoOp : CutOrIntroSceneScriptLineTurnResult {
        public NoOp(CutOrIntroSceneScriptLine scriptLine) : base(TurnResultType.Script_NoOp,
            TurnResulActionType.ActionAlreadyPerformed, scriptLine) {
        }
    }
}