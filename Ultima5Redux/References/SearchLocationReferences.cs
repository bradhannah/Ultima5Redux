using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References
{
    public class SearchItemReference
    {
        private int RawId { get; }
        public int CalcId { get; }
        public TileReference CalcTileReference { get; }
        public int Floor { get; }

        public int Index { get; }
        public SmallMapReferences.SingleMapReference.Location Location { get; }
        public Point2D Position { get; }
        public int Quality { get; }

        public SearchItemReference(int index, int id, int quality,
            SmallMapReferences.SingleMapReference.Location location, int floor, Point2D position)
        {
            RawId = id;
            CalcId = id + 0x100;
            CalcTileReference = GameReferences.Instance.SpriteTileReferences.GetTileReference(CalcId);
            Position = position;
            Index = index;
            Location = location;
            Floor = floor;
            Quality = quality;
        }
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
        public int TotalReferences => _searchItemsList.Count;

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

                // there are zero positions which are not actually used
                if ((position.X == 0 && position.Y == 0) || id == 0) continue;

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

        public List<SearchItemReference> GetListOfSearchItemReferences(
            SmallMapReferences.SingleMapReference.Location location, int nFloor)
        {
            List<SearchItemReference> searchItemReferences = new();

            if (!_searchItems.ContainsKey(location)) return searchItemReferences;

            foreach (KeyValuePair<int, Dictionary<Point2D, List<SearchItemReference>>> floors in _searchItems[location])
            {
                foreach (KeyValuePair<Point2D, List<SearchItemReference>> searchItem in floors.Value)
                {
                    searchItemReferences.AddRange(searchItem.Value.Where(searchItemReference =>
                        searchItemReference.Floor == nFloor));
                }
            }

            return searchItemReferences;
        }

        public SearchItemReference GetSearchItemReferenceByIndex(int nIndex)
        {
            if (nIndex >= _searchItemsList.Count)
                throw new Ultima5ReduxException(
                    $"Tried to get search item index {nIndex}, but there are only {_searchItemsList.Count} in the list");

            return _searchItemsList[nIndex];
        }

        public bool IsSearchItemAtLocation(SmallMapReferences.SingleMapReference.Location location,
            int nFloor, Point2D position) =>
            _searchItems.ContainsKey(location) && _searchItems[location].ContainsKey(nFloor)
                                               && _searchItems[location][nFloor].ContainsKey(position);

        public void PrintCsvOutput()
        {
            Debug.WriteLine("Index,Location,Floor,X,Y,CalcId,TileName,Quality");
            foreach (SearchItemReference searchItemReference in _searchItemsList)
            {
                Debug.WriteLine(
                    $"{searchItemReference.Index},{searchItemReference.Location},{searchItemReference.Floor},{searchItemReference.Position.X},{searchItemReference.Position.Y},{searchItemReference.CalcId},{searchItemReference.CalcTileReference.Name},{searchItemReference.Quality}");
            }
        }
    }
}