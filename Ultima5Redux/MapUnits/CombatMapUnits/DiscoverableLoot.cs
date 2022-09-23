using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    /// <summary>
    ///     Represents an invisible mapunit that includes inner items
    ///     This a secret that can be found by searching - such as the axe in Jhelom
    /// </summary>
    public sealed class DiscoverableLoot : NonAttackingUnit
    {
        public override string FriendlyName => "Loot";
        public override string PluralName => "Loot";
        public override string SingularName => "Loot";
        public override string Name => "Loot";
        public override bool ExposeInnerItemsOnOpen => false;
        public override bool ExposeInnerItemsOnSearch => true;
        public override bool IsOpenable => false;
        public override bool IsSearchable => true;
        public override bool DoesTriggerTrap(PlayerCharacterRecord record) => false;
        public override bool IsInvisible => true;
        public override bool IsActive => HasInnerItemStack && InnerItemStack.HasStackableItems;

        [DataMember]
        private List<SearchItem> _listOfSearchItems;

        /// <summary>
        ///     This needs to give us a stack of all the goods!
        /// </summary>
        [DataMember]
        public override ItemStack InnerItemStack { get; protected set; }

        public override TileReference KeyTileReference { get; set; } =
            GameReferences.SpriteTileReferences.GetTileReference(0);

        public DiscoverableLoot()
        {
        }
        
        public DiscoverableLoot(MapUnitPosition mapUnitPosition, List<SearchItem> searchItems)
        {
            _listOfSearchItems = searchItems ??
                                 throw new Ultima5ReduxException(
                                     "Cannot create DiscoverableLoot with null searchItems");

            MapUnitPosition = mapUnitPosition;
            Initialize();
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            InnerItemStack = new ItemStack(MapUnitPosition);
            foreach (SearchItem item in _listOfSearchItems)
            {
                StackableItem stackableItem = NonAttackingUnitFactory.CreateStackableItem(
                    item.TheSearchItemReference.CalcTileReference.Index,
                    item.TheSearchItemReference.Quality);
                InnerItemStack.PushStackableItem(stackableItem);
            }
        }
    }
}