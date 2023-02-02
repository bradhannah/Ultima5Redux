using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public class MapOverrides
    {
        /// <summary>
        ///     override map is responsible for overriding tiles that would otherwise be static
        /// </summary>
        [DataMember(Name = "OverrideMap")] private Dictionary<Point2D, int> _overrideMap = new();

        [IgnoreDataMember] internal Map TheMap { get; set; }

        [JsonConstructor] private MapOverrides()
        {
        }

        public MapOverrides(Map map)
        {
            TheMap = map;
            ClearOverridenTiles();
        }

        private void ClearOverridenTiles()
        {
            _overrideMap.Clear();

            TheMap.ClearOpenDoors();
        }

        public int GetOverrideTileIndex(in Point2D xy)
        {
            if (!_overrideMap.ContainsKey(xy)) return -1;
            return _overrideMap[xy];
        }

        public TileReference GetOverrideTileReference(int x, int y) => GetOverrideTileReference(new Point2D(x, y));

        public TileReference GetOverrideTileReference(in Point2D xy)
        {
            int nIndex = GetOverrideTileIndex(xy);
            if (nIndex == -1) return null;
            return GameReferences.Instance.SpriteTileReferences.GetTileReference(nIndex);
        }

        public bool HasOverrideTile(in Point2D xy) => _overrideMap.ContainsKey(xy);

        public void SetOverrideTile(in Point2D xy, int nIndex)
        {
            if (!_overrideMap.ContainsKey(xy))
                _overrideMap.Add(xy, nIndex);
            else
            {
                _overrideMap[xy] = nIndex;
            }
        }

        public void SetOverrideTile(in Point2D xy, TileReference tileReference)
        {
            SetOverrideTile(xy, tileReference.Index);
        }
    }
}