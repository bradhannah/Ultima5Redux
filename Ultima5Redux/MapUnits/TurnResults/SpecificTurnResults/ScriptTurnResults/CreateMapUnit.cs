using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults
{
    public class CreateMapUnit : CutOrIntroSceneScriptLineTurnResult
    {
        public CreateMapUnit(CutOrIntroSceneScriptLine scriptLine) : base(TurnResultType.Script_CreateMapUnit,
            TurnResulActionType.ActionRequired, scriptLine) {
        }
    }
}