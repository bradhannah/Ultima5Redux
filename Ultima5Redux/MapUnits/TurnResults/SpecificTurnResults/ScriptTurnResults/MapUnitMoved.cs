using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults
{
    public class MapUnitMove : CutOrIntroSceneScriptLineTurnResult
    {
        public MapUnitMove(CutOrIntroSceneScriptLine scriptLine) : base(TurnResultType.Script_MapUnitMove,
            TurnResulActionType.ActionRequired, scriptLine) {
        }
    }
}