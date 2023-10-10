using Ultima5Redux.Maps;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults
{
    public class ExecuteCutScene : TurnResult
    {
        public CutOrIntroSceneScript Script { get; private set; }
        public SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType MapType { get; private set; }

        public ShrineReference ShrineReference { get; private set; }

        public ExecuteCutScene(SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType mapType,
            CutOrIntroSceneScript script, ShrineReference shrineReference = null) : base(TurnResultType.ExecuteCutScene,
            TurnResulActionType.ActionRequired) {
            Script = script;
            MapType = mapType;
            ShrineReference = shrineReference;
        }
    }
}