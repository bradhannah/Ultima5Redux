using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class BoostStats : CutOrIntroSceneScriptLineTurnResult {
        public BoostStats(CutOrIntroSceneScriptLine scriptLine) : base(TurnResultType.Script_BoostStats,
            TurnResulActionType.ActionRequired, scriptLine) {
        }
    }
}