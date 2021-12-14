using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public class MapOverrides
    {
        /// <summary>
        ///     Exposed searched or loot items
        /// </summary>
        [DataMember(Name = "ExposedSearchItems")]
        private Dictionary<Point2D, Queue<InventoryItem>> _exposedSearchItems =
            new Dictionary<Point2D, Queue<InventoryItem>>();

        /// <summary>
        ///     override map is responsible for overriding tiles that would otherwise be static
        /// </summary>
        [DataMember(Name = "OverrideMap")]
        private Dictionary<Point2D, int> _overrideMap = new Dictionary<Point2D, int>();

        [IgnoreDataMember] internal Map TheMap { get; set; }

        [IgnoreDataMember] public int NumOfCols => TheMap.NumOfYTiles;

        [IgnoreDataMember] public int NumOfRows => TheMap.NumOfXTiles;

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
            _exposedSearchItems.Clear();

            TheMap.ClearOpenDoors();
        }

        public InventoryItem DequeueSearchItem(int x, int y) => DequeueSearchItem(new Point2D(x, y));

        public InventoryItem DequeueSearchItem(Point2D xy)
        {
            if (!HasExposedSearchItems(xy))
                throw new Ultima5ReduxException("Tried to dequeue search item, but non were in the queue. ");

            return GetExposedSearchItems(xy).Dequeue();
        }

        public void EnqueueSearchItem(int x, int y, InventoryItem inventoryItem) =>
            EnqueueSearchItem(new Point2D(x, y), inventoryItem);

        public void EnqueueSearchItem(Point2D xy, InventoryItem inventoryItem)
        {
            if (!_exposedSearchItems.ContainsKey(xy))
                _exposedSearchItems.Add(xy, new Queue<InventoryItem>());

            _exposedSearchItems[xy].Enqueue(inventoryItem);
        }

        public Queue<InventoryItem> GetExposedSearchItems(int x, int y) => GetExposedSearchItems(new Point2D(x, y));

        public Queue<InventoryItem> GetExposedSearchItems(Point2D xy)
        {
            if (!_exposedSearchItems.ContainsKey(xy)) return new Queue<InventoryItem>();
            return _exposedSearchItems[xy];
        }

        public int GetOverrideTileIndex(Point2D xy)
        {
            if (!_overrideMap.ContainsKey(xy)) return -1;
            return _overrideMap[xy];
        }

        public TileReference GetOverrideTileReference(int x, int y) => GetOverrideTileReference(new Point2D(x, y));

        public TileReference GetOverrideTileReference(Point2D xy)
        {
            int nIndex = GetOverrideTileIndex(xy);
            if (nIndex == -1) return null;
            return GameReferences.SpriteTileReferences.GetTileReference(nIndex);
        }

        // SEARCH ITEMS

        public bool HasExposedSearchItems(int x, int y) => GetExposedSearchItems(x, y).Count > 0;
        public bool HasExposedSearchItems(Point2D xy) => HasExposedSearchItems(xy.X, xy.Y);

        public bool HasOverrideTile(int x, int y) => GetOverrideTileReference(x, y) != null;
        public bool HasOverrideTile(Point2D xy) => HasOverrideTile(xy.X, xy.Y);

        public void SetOverrideTile(Point2D xy, int nIndex)
        {
            if (!_overrideMap.ContainsKey(xy))
                _overrideMap.Add(xy, nIndex);
            else
            {
                _overrideMap[xy] = nIndex;
            }
        }

        public void SetOverrideTile(int x, int y, int nIndex) => SetOverrideTile(new Point2D(x, y), nIndex);

        public void SetOverrideTile(int x, int y, TileReference tileReference) =>
            SetOverrideTile(new Point2D(x, y), tileReference.Index);

        public void SetOverrideTile(Point2D xy, TileReference tileReference) =>
            SetOverrideTile(xy, tileReference.Index);
    }
}