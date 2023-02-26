using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract]
    public class SearchItems
    {
        //public const int MAX_TOTAL_SEARCH_ITEMS = 0x72;
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
        
    }
}