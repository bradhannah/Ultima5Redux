using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults
{
    public class ExitBuilding : CutOrIntroSceneScriptLineTurnResult
    {
        public ExitBuilding(CutOrIntroSceneScriptLine scriptLine) : base(TurnResultType.Script_ExitBuilding,
            TurnResulActionType.ActionRequired, scriptLine) {
        }
    }
}