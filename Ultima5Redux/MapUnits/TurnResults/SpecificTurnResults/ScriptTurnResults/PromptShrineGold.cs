using Ultima5Redux.Maps;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class PromptShrineGold : CutOrIntroSceneScriptLineTurnResult {
        public ShrineReference ShrineReference { get; }

        public PromptShrineGold(CutOrIntroSceneScriptLine scriptLine, ShrineReference shrineReference) : base(
            TurnResultType.Script_PromptShrineGold,
            TurnResulActionType.ActionRequired, scriptLine) =>
            ShrineReference = shrineReference;
    }
}