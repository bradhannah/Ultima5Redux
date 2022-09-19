using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class SearchItem
    {
        public bool IsDiscovered { get; set; }
        public SearchItemReference TheSearchItemReference { get; }
        public int SearchItemIndex { get; }

        public SearchItem(int nSearchItemIndex, bool bIsDiscovered)
        {
            SearchItemIndex = nSearchItemIndex;
            IsDiscovered = bIsDiscovered;
        }
    }

    public class SearchItems
    {
        [DataMember] private readonly List<SearchItem> _searchItems = new();

        public SearchItems(List<bool> searchItems)
        {
            if (searchItems.Count < 0x72)
                throw new Ultima5ReduxException($"Too few search items in the list: {searchItems.Count}");

            for (int i = 0; i < searchItems.Count; i++)
            {
                bool bIsDiscovered = searchItems[i];
                _searchItems.Add(new SearchItem(i, bIsDiscovered));
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