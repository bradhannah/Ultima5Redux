using Ultima5Redux.Maps;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults
{
    public class PromptVirtueMeditate : CutOrIntroSceneScriptLineTurnResult
    {
        public ShrineReference ShrineReference { get; private set; }

        public PromptVirtueMeditate(CutOrIntroSceneScriptLine scriptLine, VirtueReference virtue) : base(
            TurnResultType.Script_PromptVirtueMeditate, TurnResulActionType.ActionRequired, scriptLine) =>
            ShrineReference = GameReferences.Instance.ShrineReferences.GetShrineReferenceByVirtue(virtue.Virtue);
    }
}