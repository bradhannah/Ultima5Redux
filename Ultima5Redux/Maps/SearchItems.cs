using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract]
    public class SearchItem
    {
        [DataMember] public bool IsDiscovered { get; set; }
        [DataMember] public int SearchItemIndex { get; private set; }
        [IgnoreDataMember] public SearchItemReference TheSearchItemReference { get; private set; }

        public SearchItem(int nSearchItemIndex, bool bIsDiscovered, SearchItemReference theSearchItemReference)
        {
            SearchItemIndex = nSearchItemIndex;
            IsDiscovered = bIsDiscovered;
            TheSearchItemReference = theSearchItemReference;
        }

        [OnDeserialized]
        private void PostDeserialize(StreamingContext context)
        {
            TheSearchItemReference =
                GameReferences.Instance.SearchLocationReferences.GetSearchItemReferenceByIndex(SearchItemIndex);
        }
    }

    [DataContract]
    public class SearchItems
    {
        public const int MAX_TOTAL_SEARCH_ITEMS = 0x72;
        [DataMember] private readonly List<SearchItem> _searchItems = new();

        public SearchItems()
        {
        }

        public SearchItems(List<bool> searchItems)
        {
            if (searchItems.Count == 0) //< N_TOTAL_SEARCH_ITEMS)
                throw new Ultima5ReduxException($"Too few search items in the list: {searchItems.Count}");

            for (int i = 0; i < GameReferences.Instance.SearchLocationReferences.TotalReferences; i++)
            {
                bool bIsDiscovered = searchItems[i];
                SearchItemReference searchItemReference =
                    GameReferences.Instance.SearchLocationReferences.GetSearchItemReferenceByIndex(i);

                var item = new SearchItem(i, bIsDiscovered, searchItemReference);

                _searchItems.Add(item);
            }
        }

        public Dictionary<Point2D, List<SearchItem>> GetUnDiscoveredSearchItemsByLocation(
            SmallMapReferences.SingleMapReference.Location location,
            int nFloor)
        {
            List<SearchItemReference> searchItemReferences =
                GameReferences.Instance.SearchLocationReferences.GetListOfSearchItemReferences(location, nFloor);

            if (searchItemReferences == null || searchItemReferences.Count == 0)
                return new Dictionary<Point2D, List<SearchItem>>();

            Dictionary<Point2D, List<SearchItem>> searchItemsDictionary = new();
            foreach (SearchItemReference searchItemReference in searchItemReferences)
            {
                // we now have all the search references for the exact floor and location
                // we ignore items that are already discovered, keeps it cleaner
                SearchItem searchItem = _searchItems[searchItemReference.Index];
                if (searchItem.IsDiscovered) continue;
                if (!searchItemsDictionary.ContainsKey(searchItemReference.Position))
                    searchItemsDictionary.Add(searchItemReference.Position, new List<SearchItem>());

                searchItemsDictionary[searchItemReference.Position].Add(searchItem);
            }

            return searchItemsDictionary;
        }

        public List<SearchItem> GetUnDiscoveredSearchItemsByLocation(
            SmallMapReferences.SingleMapReference.Location location)
        {
            List<SearchItemReference> searchItemReferences =
                GameReferences.Instance.SearchLocationReferences.GetListOfSearchItemReferences(location);

            List<SearchItem> searchItems = new();

            foreach (SearchItemReference searchItemReference in searchItemReferences)
            {
                SearchItem item = _searchItems[searchItemReference.Index];
                if (item == null)
                    throw new Ultima5ReduxException(
                        $"Expected item at index {searchItemReference.Index} or type {searchItemReference.CalcTileReference.Description} but got null");

                // if it's already discovered then we filter it out
                if (item.IsDiscovered) continue;

                searchItems.Add(item);
            }

            return searchItems;
        }

        public List<SearchItem> GetUnDiscoveredSearchItemsByLocation(
            SmallMapReferences.SingleMapReference.Location location, int nFloor, Point2D position)
        {
            if (!IsAvailableSearchItemByLocation(location, nFloor, position)) return new List<SearchItem>();

            List<SearchItemReference> searchItemsReferences =
                GameReferences.Instance.SearchLocationReferences.GetListOfSearchItemReferences(location, nFloor,
                    position);

            List<SearchItem> searchItems = new();
            foreach (SearchItemReference searchItemReference in searchItemsReferences)
            {
                if (!_searchItems[searchItemReference.Index].IsDiscovered)
                    searchItems.Add(_searchItems[searchItemReference.Index]);
            }

            return searchItems;
        }

        public void Initialize()
        {
            for (int i = 0; i < GameReferences.Instance.SearchLocationReferences.TotalReferences; i++)
            {
                SearchItemReference searchItemReference =
                    GameReferences.Instance.SearchLocationReferences.GetSearchItemReferenceByIndex(i);

                var item = new SearchItem(i, false, searchItemReference);

                _searchItems.Add(item);
            }
        }

        public bool IsAvailableSearchItemByLocation(SmallMapReferences.SingleMapReference.Location location, int nFloor,
            Point2D position)
        {
            if (!GameReferences.Instance.SearchLocationReferences.IsSearchItemAtLocation(location, nFloor, position))
            {
                return false;
            }

            return GameReferences.Instance.SearchLocationReferences
                .GetListOfSearchItemReferences
                    (location, nFloor, position).Any(searchItemReference =>
                    !_searchItems[searchItemReference.Index].IsDiscovered);
        }
    }
}