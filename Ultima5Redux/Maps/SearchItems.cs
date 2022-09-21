using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract] public class SearchItem
    {
        [DataMember] public bool IsDiscovered { get; set; }
        [IgnoreDataMember] public SearchItemReference TheSearchItemReference { get; private set; }
        [DataMember] public int SearchItemIndex { get; }

        public SearchItem(int nSearchItemIndex, bool bIsDiscovered, SearchItemReference theSearchItemReference)
        {
            SearchItemIndex = nSearchItemIndex;
            IsDiscovered = bIsDiscovered;
            TheSearchItemReference = theSearchItemReference;
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            TheSearchItemReference =
                GameReferences.SearchLocationReferences.GetSearchItemReferenceByIndex(SearchItemIndex);
        }
    }

    [DataContract] public class SearchItems
    {
        [DataMember] private readonly List<SearchItem> _searchItems = new();

        public const int N_TOTAL_SEARCH_ITEMS = 0x72;

        public SearchItems()
        {
        }

        public void Initialize()
        {
            for (int i = 0; i < N_TOTAL_SEARCH_ITEMS; i++)
            {
                SearchItemReference searchItemReference =
                    GameReferences.SearchLocationReferences.GetSearchItemReferenceByIndex(i);

                var item = new SearchItem(i, false, searchItemReference);

                _searchItems.Add(item);
            }
        }

        public SearchItems(List<bool> searchItems)
        {
            if (searchItems.Count < N_TOTAL_SEARCH_ITEMS)
                throw new Ultima5ReduxException($"Too few search items in the list: {searchItems.Count}");

            for (int i = 0; i < N_TOTAL_SEARCH_ITEMS; i++)
            {
                bool bIsDiscovered = searchItems[i];
                SearchItemReference searchItemReference =
                    GameReferences.SearchLocationReferences.GetSearchItemReferenceByIndex(i);

                var item = new SearchItem(i, bIsDiscovered, searchItemReference);

                _searchItems.Add(item);
            }
        }

        public bool IsAvailableSearchItemByLocation(SmallMapReferences.SingleMapReference.Location location, int nFloor,
            Point2D position)
        {
            if (!GameReferences.SearchLocationReferences.IsSearchItemAtLocation(location, nFloor, position))
            {
                return false;
            }

            return GameReferences.SearchLocationReferences
                .GetListOfSearchItemReferences
                    (location, nFloor, position).Any(searchItemReference =>
                    !_searchItems[searchItemReference.Index].IsDiscovered);
        }

        public List<SearchItem> GetUnDiscoveredSearchItemsByLocation(
            SmallMapReferences.SingleMapReference.Location location)
        {
            List<SearchItemReference> searchItemReferences =
                GameReferences.SearchLocationReferences.GetListOfSearchItemReferences(location);

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
                GameReferences.SearchLocationReferences.GetListOfSearchItemReferences(location, nFloor, position);

            List<SearchItem> searchItems = new();
            foreach (SearchItemReference searchItemReference in searchItemsReferences)
            {
                if (!_searchItems[searchItemReference.Index].IsDiscovered)
                    searchItems.Add(_searchItems[searchItemReference.Index]);
            }

            return searchItems;
        }
    }
}