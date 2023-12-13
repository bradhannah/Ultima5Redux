using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class BoostKarmaByMoney : CutOrIntroSceneScriptLineTurnResult {
        public BoostKarmaByMoney(CutOrIntroSceneScriptLine scriptLine) : base(TurnResultType.Script_BoostKarmaByMoney,
            TurnResulActionType.ActionAlreadyPerformed, scriptLine) {
        }
    }
}