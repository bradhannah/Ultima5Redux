using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Properties;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class CutOrIntroSceneScriptLine
    {
        public enum CutOrIntroSceneScriptLineCommand
        {
            CreateMapunit, MoveMapunit, PromptVirtueMeditate, EndSequence, Comment
        }

        [DataMember] public int FrameNum { get; private set; }
        [DataMember] public CutOrIntroSceneScriptLineCommand Command { get; private set; }
        [DataMember] public string StrParam { get; private set; } = string.Empty;
        [DataMember(EmitDefaultValue = true)] public int IntParam { get; private set; } = -1;
        [DataMember] public int X { get; private set; }
        [DataMember] public int Y { get; private set; }
        [DataMember] public bool Visible { get; private set; }
        [DataMember] public string Comment { get; private set; }

        [IgnoreDataMember]
        public TileReference TileReference => GameReferences.Instance.SpriteTileReferences.GetTileReference(IntParam);

        [IgnoreDataMember] public Point2D Position => new(X, Y);
    }

    [DataContract] public class CutOrIntroSceneScripts
    {
        private readonly Dictionary<SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType, CutOrIntroSceneScript>
            _scripts = new();

        public CutOrIntroSceneScript GetScriptByCutOrIntroSceneMapType(
            SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType cutOrIntroSceneMapType) =>
            _scripts[cutOrIntroSceneMapType];

        public CutOrIntroSceneScripts() {
            Dictionary<SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType, List<CutOrIntroSceneScriptLine>>
                scripts =
                    JsonConvert
                        .DeserializeObject<
                            Dictionary<SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType,
                                List<CutOrIntroSceneScriptLine>>>(Resources.CutSceneScripts);

            foreach (KeyValuePair<SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType,
                         List<CutOrIntroSceneScriptLine>> kvp in scripts) {
                _scripts.Add(kvp.Key, new CutOrIntroSceneScript(kvp.Key, kvp.Value));
            }
        }
    }

    [DataContract] public class CutOrIntroSceneScript
    {
        public SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType MapType { get; }

        public IEnumerable<CutOrIntroSceneScriptLine> ScriptLines => _scriptItems;

        private readonly List<CutOrIntroSceneScriptLine> _scriptItems;

        internal CutOrIntroSceneScript(SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType mapType,
            List<CutOrIntroSceneScriptLine> scriptLines) {
            MapType = mapType;
            _scriptItems = scriptLines;
        }
    }
}