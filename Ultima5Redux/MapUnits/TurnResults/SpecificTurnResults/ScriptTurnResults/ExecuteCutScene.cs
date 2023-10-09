using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults
{
    public class ExecuteCutScene : TurnResult
    {
        public CutOrIntroSceneScript Script { get; private set; }
        public SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType MapType { get; private set; }

        public ExecuteCutScene(SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType mapType,
            CutOrIntroSceneScript script) : base(TurnResultType.ExecuteCutScene, TurnResulActionType.ActionRequired) {
            Script = script;
            MapType = mapType;
        }
    }
}