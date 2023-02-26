using System.Runtime.Serialization;
using Ultima5Redux.References;

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
}