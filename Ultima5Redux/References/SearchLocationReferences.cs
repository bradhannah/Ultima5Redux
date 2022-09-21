using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References
{
    public class SearchItemReference
    {
        public SearchItemReference(int index, int id, int quality,
            SmallMapReferences.SingleMapReference.Location location, int floor, Point2D position)
        {
            RawId = id;
            CalcId = id + 0x100;
            CalcTileReference = GameReferences.SpriteTileReferences.GetTileReference(CalcId);
            Position = position;
            Index = index;
            Location = location;
            Floor = floor;
            Quality = quality;
        }

        public int Index { get; }
        public int Quality { get; }
        private int RawId { get; }
        public int CalcId { get; }
        public TileReference CalcTileReference { get; }
        public int Floor { get; }
        public SmallMapReferences.SingleMapReference.Location Location { get; }
        public Point2D Position { get; }
    }

    // public class SearchItemReference
    // {
    //     
    // }

    public class SearchLocationReferences
    {
        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location,
                Dictionary<int, Dictionary<Point2D, List<SearchItemReference>>>>
            _searchItems = new();

        private readonly List<SearchItemReference> _searchItemsList = new();

        public bool IsSearchItemAtLocation(SmallMapReferences.SingleMapReference.Location location,
            int nFloor, Point2D position) =>
            _searchItems.ContainsKey(location) && _searchItems[location].ContainsKey(nFloor)
                                               && _searchItems[location][nFloor].ContainsKey(position);

        public SearchItemReference GetSearchItemReferenceByIndex(int nIndex)
        {
            if (nIndex >= _searchItemsList.Count)
                throw new Ultima5ReduxException(
                    $"Tried to get search item index {nIndex}, but there are only {_searchItemsList.Count} in the list");

            return _searchItemsList[nIndex];
        }

        public List<SearchItemReference> GetListOfSearchItemReferences(
            SmallMapReferences.SingleMapReference.Location location,
            int nFloor, Point2D position)
        {
            if (!IsSearchItemAtLocation(location, nFloor, position)) return new List<SearchItemReference>();

            return _searchItems[location][nFloor][position];
        }

        public List<SearchItemReference> GetListOfSearchItemReferences(
            SmallMapReferences.SingleMapReference.Location location)
        {
            List<SearchItemReference> searchItemReferences = new();

            if (!_searchItems.ContainsKey(location)) return searchItemReferences;

            foreach (KeyValuePair<int, Dictionary<Point2D, List<SearchItemReference>>> floors in _searchItems[location])
            {
                foreach (KeyValuePair<Point2D, List<SearchItemReference>> searchItem in floors.Value)
                {
                    searchItemReferences.AddRange(searchItem.Value);
                }
            }

            return searchItemReferences;
        }

        public SearchLocationReferences(DataOvlReference dataOvlReference, TileReferences tileReferences)
        {
            DataChunk ids = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.SEARCH_OBJECT_ID);
            DataChunk qualities = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.SEARCH_OBJECT_QUALITY);
            DataChunk locations = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.SEARCH_OBJECT_LOCATION);
            DataChunk floors = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.SEARCH_OBJECT_FLOOR);
            DataChunk xPositions = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.SEARCH_OBJECT_X);
            DataChunk yPositions = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.SEARCH_OBJECT_Y);

            for (int i = 0; i < 0x72; i++)
            {
                int id = ids.GetByte(i);
                int quality = qualities.GetByte(i);
                var location =
                    (SmallMapReferences.SingleMapReference.Location)locations.GetByte(i);
                int floor = floors.GetByte(i);
                // adjustment because basements and the underworld come in as 0xff
                if (floor == 0xff) floor = -1;
                int xPosition = xPositions.GetByte(i);
                int yPosition = yPositions.GetByte(i);

                var position = new Point2D(xPosition, yPosition);

                SearchItemReference searchItemReference =
                    new(i, id, quality, location, floor, position);

                if (!_searchItems.ContainsKey(location))
                    _searchItems.Add(location, new Dictionary<int, Dictionary<Point2D, List<SearchItemReference>>>());
                if (!_searchItems[location].ContainsKey(floor))
                    _searchItems[location].Add(floor, new Dictionary<Point2D, List<SearchItemReference>>());
                if (!_searchItems[location][floor].ContainsKey(position))
                    _searchItems[location][floor].Add(position, new List<SearchItemReference>());
                _searchItems[location][floor][position].Add(searchItemReference);
                _searchItemsList.Add(searchItemReference);
            }

            _ = "";
        }
    }
}