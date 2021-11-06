using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.Maps
{
    [DataContract]
    public class MapOverrides
    {
        private readonly TileReferences _tileReferences;

        /// <summary>
        ///     Exposed searched or loot items
        /// </summary>
        [DataMember(Name="ExposedSearchItems")] private Queue<InventoryItem>[][] _exposedSearchItems;

        /// <summary>
        ///     override map is responsible for overriding tiles that would otherwise be static
        /// </summary>
        [DataMember(Name="OverrideMap")] private int[][] _overrideMap;

        [IgnoreDataMember] public int NumOfRows => TheMap.NumOfXTiles;
        [IgnoreDataMember] public int NumOfCols => TheMap.NumOfYTiles;

        [IgnoreDataMember] internal Map TheMap { get; }
        
        public MapOverrides(Map map, TileReferences tileReferences)
        {
            _tileReferences = tileReferences;
            TheMap = map;
            ClearOverridenTiles();
        }

        public TileReference GetOverrideTileReference(int x, int y)
        {
            if (_overrideMap[x][y] == 0) return null;
            return _tileReferences.GetTileReference(_overrideMap[x][y]);
        }

        public int GetOverrideTileIndex(Point2D xy) => GetOverrideTileReference(xy.X, xy.Y).Index;

        public TileReference GetOverrideTileReference(Point2D xy) => GetOverrideTileReference(xy.X, xy.Y);

        public void SetOverrideTile(int x, int y, int nIndex)
        {
            _overrideMap[x][y] = nIndex;
        }

        public void SetOverrideTile(int x, int y, TileReference tileReference) =>
            SetOverrideTile(x, y, tileReference.Index);

        public void SetOverrideTile(Point2D xy, TileReference tileReference) => SetOverrideTile(xy.X, xy.Y, tileReference.Index);

        public bool HasOverrideTile(int x, int y) => GetOverrideTileReference(x, y) != null;
        public bool HasOverrideTile(Point2D xy) => HasOverrideTile(xy.X, xy.Y);
        
        public bool HasExposedSearchItems(int x, int y) => GetExposedSearchItems(x, y).Count > 0;
        public bool HasExposedSearchItems(Point2D xy) => HasExposedSearchItems(xy.X, xy.Y);
        
        public Queue<InventoryItem> GetExposedSearchItems(int x, int y)
        {
            return _exposedSearchItems[x][y] ?? new Queue<InventoryItem>();
        }

        public Queue<InventoryItem> GetExposedSearchItems(Point2D xy) => GetExposedSearchItems(xy.X, xy.Y);

        public void EnqueueSearchItem(int x, int y, InventoryItem inventoryItem)
        {
            if (!HasExposedSearchItems(x, y))
            {
                _exposedSearchItems[x][y] = new Queue<InventoryItem>();
            }
            _exposedSearchItems[x][y].Enqueue(inventoryItem);
        }

        public void EnqueueSearchItem(Point2D xy, InventoryItem inventoryItem) =>
            EnqueueSearchItem(xy.X, xy.Y, inventoryItem);

        public InventoryItem DequeueSearchItem(int x, int y)
        {
            if (!HasExposedSearchItems(x, y))
                throw new Ultima5ReduxException("Tried to dequeue search item, but non were in the queue. ");

            return GetExposedSearchItems(x, y).Dequeue();
        }

        public InventoryItem DequeueSearchItem(Point2D xy) => DequeueSearchItem(xy.X, xy.Y);

        private void ClearOverridenTiles()
        {
            _overrideMap = Utils.Init2DArray<int>(NumOfCols, NumOfRows);
            _exposedSearchItems = Utils.Init2DArray<Queue<InventoryItem>>(NumOfCols, NumOfRows);
            TheMap.ClearOpenDoors();
        }

    }
}