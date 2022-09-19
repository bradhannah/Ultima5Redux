using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References
{
    public class SearchItemReference
    {
        public SearchItemReference(int index, int id, int quality,
            SmallMapReferences.SingleMapReference.Location location,
            int floor, Point2D position, TileReferences tileReferences)
        {
            RawId = id;
            CalcId = id + 0x100;
            CalcTileReference = tileReferences.GetTileReference(CalcId);
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

        public List<SearchItemReference> GetListOfSearchItemReferences(
            SmallMapReferences.SingleMapReference.Location location,
            int nFloor, Point2D position)
        {
            if (!IsSearchItemAtLocation(location, nFloor, position)) return new List<SearchItemReference>();

            return _searchItems[location][nFloor][position];
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
                int xPosition = xPositions.GetByte(i);
                int yPosition = yPositions.GetByte(i);

                var position = new Point2D(xPosition, yPosition);

                SearchItemReference searchItemReference =
                    new(i, id, quality, location, floor, position, tileReferences);

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