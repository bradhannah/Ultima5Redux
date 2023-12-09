using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class ScreenEffect : CutOrIntroSceneScriptLineTurnResult {
        public ScreenEffect(CutOrIntroSceneScriptLine scriptLine) : base(TurnResultType.Script_ScreenEffect,
            TurnResulActionType.ActionRequired, scriptLine) {
        }
    }
}