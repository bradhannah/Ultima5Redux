using Ultima5Redux.Maps;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class BoostKarmaByMoney : CutOrIntroSceneScriptLineTurnResult {
        public ShrineReference ShrineReference { get; }

        public BoostKarmaByMoney(CutOrIntroSceneScriptLine scriptLine, ShrineReference shrineReference) : base(
            TurnResultType.Script_BoostKarmaByMoney,
            TurnResulActionType.ActionAlreadyPerformed, scriptLine) =>
            ShrineReference = shrineReference;
    }
}