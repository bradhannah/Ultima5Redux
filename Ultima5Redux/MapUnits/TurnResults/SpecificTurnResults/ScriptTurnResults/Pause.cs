using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults
{
    public class Pause : CutOrIntroSceneScriptLineTurnResult
    {
        public Pause(CutOrIntroSceneScriptLine scriptLine) : base(TurnResultType.Script_Pause,
            TurnResulActionType.ActionRequired, scriptLine) {
        }
    }
}