using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults
{
    public abstract class CutOrIntroSceneScriptLineTurnResult : TurnResult
    {
        public CutOrIntroSceneScriptLine ScriptLine { get; }

        protected CutOrIntroSceneScriptLineTurnResult(TurnResultType theTurnResultType,
            TurnResulActionType theTurnResulActionType, CutOrIntroSceneScriptLine scriptLine) : base(theTurnResultType,
            theTurnResulActionType) => ScriptLine = scriptLine;
    }
}