using Ultima5Redux.Maps;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class PromptMantra : CutOrIntroSceneScriptLineTurnResult {
        public ShrineReference ShrineReference { get; private set; }

        public PromptMantra(CutOrIntroSceneScriptLine scriptLine, VirtueReference virtueReference) : base(
            TurnResultType.Script_PromptMantra, TurnResulActionType.ActionRequired, scriptLine) =>
            //Script_PromptMantra
            ShrineReference =
                GameReferences.Instance.ShrineReferences.GetShrineReferenceByVirtue(virtueReference.Virtue);
    }
}