using Ultima5Redux.Maps;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class BoostStats : CutOrIntroSceneScriptLineTurnResult {
        public ShrineReference ShrineReference { get; }

        public BoostStats(CutOrIntroSceneScriptLine scriptLine, ShrineReference shrineReference) : base(
            TurnResultType.Script_BoostStats,
            TurnResulActionType.ActionRequired, scriptLine) =>
            ShrineReference = shrineReference;
    }
}