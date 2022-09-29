using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    /// <summary>
    ///     Represents an invisible mapunit that includes inner items
    ///     This a secret that can be found by searching - such as the axe in Jhelom
    /// </summary>
    [DataContract] public sealed class DiscoverableLoot : NonAttackingUnit
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

        public override bool IsActive
        {
            get
            {
                if (HasInnerItemStack && InnerItemStack.HasStackableItems) return true;

                return IsDeadBody || IsBloodSpatter;
            }
        }

        [DataMember] private List<SearchItem> _listOfSearchItems;
        [DataMember] private List<InventoryItem> _inventoryItems;
        [DataMember] private LootType _lootType;

        private enum LootType { SearchItems, InventoryItems }
        /// <summary>
        ///     This needs to give us a stack of all the goods!
        /// </summary>
        [DataMember]
        public override ItemStack InnerItemStack { get; protected set; }

        public bool IsDeadBody => AlternateNonAttackingUnit is DeadBody;
        public bool IsBloodSpatter => AlternateNonAttackingUnit is BloodSpatter;

        /// <summary>
        ///     This represents what this will become if searched - such as a dead body
        /// </summary>
        [IgnoreDataMember]
        public NonAttackingUnit AlternateNonAttackingUnit { get; private set; }

        public override TileReference KeyTileReference { get; set; } =
            GameReferences.SpriteTileReferences.GetTileReference(0);

        public DiscoverableLoot()
        {
        }

        public DiscoverableLoot(SmallMapReferences.SingleMapReference.Location location,
            MapUnitPosition mapUnitPosition, List<InventoryItem> inventoryItems)
        {
            _lootType = LootType.InventoryItems;

            _inventoryItems = inventoryItems;

            MapUnitPosition = mapUnitPosition;
            MapLocation = location;

            InitializeInventoryItems();
        }
        
        public DiscoverableLoot(SmallMapReferences.SingleMapReference.Location location,
            MapUnitPosition mapUnitPosition, List<SearchItem> searchItems)
        {
            _listOfSearchItems = searchItems ??
                                 throw new Ultima5ReduxException(
                                     "Cannot create DiscoverableLoot with null searchItems");

            _lootType = LootType.SearchItems;
            
            MapUnitPosition = mapUnitPosition;
            MapLocation = location;
            InitializeSearchItems();
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            InitializeSearchItems();
        }

        private void InitializeInventoryItems()
        {
            InnerItemStack = new ItemStack(MapUnitPosition);

            foreach (InventoryItem inventoryItem in _inventoryItems)
            {
                var stackableItem = new StackableItem(inventoryItem);
                InnerItemStack.PushStackableItem(stackableItem);
            }
        }

        private void InitializeSearchItems()
        {
            InnerItemStack = new ItemStack(MapUnitPosition);
            foreach (SearchItem item in _listOfSearchItems)
            {
                // if it is a dead body or splat then handle special

                if ((TileReference.SpriteIndex)item.TheSearchItemReference.CalcTileReference.Index is
                    TileReference.SpriteIndex.DeadBody or TileReference.SpriteIndex.BloodSpatter)
                {
                    AlternateNonAttackingUnit =
                        NonAttackingUnitFactory.Create(item.TheSearchItemReference.CalcTileReference.Index,
                            MapLocation, MapUnitPosition);
                    if (AlternateNonAttackingUnit is DeadBody or BloodSpatter)
                    {
                        AlternateNonAttackingUnit.ClearTrapAndInnerStack();
                    }
                }
                else
                {
                    StackableItem stackableItem = NonAttackingUnitFactory.CreateStackableItem(
                        item.TheSearchItemReference.CalcTileReference.Index,
                        item.TheSearchItemReference.Quality);
                    InnerItemStack.PushStackableItem(stackableItem);
                }
            }
        }
    }
}