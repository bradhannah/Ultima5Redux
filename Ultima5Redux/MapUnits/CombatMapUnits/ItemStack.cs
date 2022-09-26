using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    /// <summary>
    ///     Represents a stack of items that can be picked up
    /// </summary>
    [DataContract] public sealed class ItemStack : NonAttackingUnit
    {
        [DataMember] private readonly Stack<StackableItem> _stackableItems = new();
        public override bool ExposeInnerItemsOnOpen => false;
        public override bool ExposeInnerItemsOnSearch => true;
        public override string FriendlyName { get; }
        public override bool HasInnerItemStack => false;

        public override bool IsActive => HasStackableItems;

        public override bool IsLocked { get; set; } = false;
        public override bool IsOpenable => false;
        public override bool IsSearchable => false;

        public override TileReference KeyTileReference
        {
            get
            {
                if (!HasStackableItems) return GameReferences.SpriteTileReferences.GetTileReference(256);
                return GameReferences.SpriteTileReferences.GetTileReference(_stackableItems.Peek().InvItem.SpriteNum);
            }
            set => throw new NotImplementedException("Cannot assign KeyTileReference in ItemStack");
        }

        public override string Name { get; }
        public override string PluralName { get; }
        public override string SingularName { get; }

        public bool HasStackableItems => TotalItems > 0;

        public int TotalItems => _stackableItems.Count;

        public string ThouFindStr
        {
            get
            {
                string foundStr = "";
                foreach (StackableItem item in _stackableItems)
                {
                    foundStr += item.InvItem.FindDescription + "\n";
                }

                return U5StringRef.ThouDostFind(foundStr.TrimEnd());
            }
        }

        //public enum StackableType { DeadBody, BloodSpatter, }

        [JsonConstructor] public ItemStack()
        {
        }
        
        
        public ItemStack(MapUnitPosition mapUnitPosition) => MapUnitPosition = mapUnitPosition;

        public override bool DoesTriggerTrap(PlayerCharacterRecord record) => false;

        public StackableItem PopStackableItem()
        {
            if (_stackableItems.Count == 0)
                throw new Ultima5ReduxException("Tried to pop a StackableItem but non were left");
            //if (_stackableItems.Count == 1) 
            return _stackableItems.Pop();
        }

        public void PushStackableItem(StackableItem item)
        {
            _stackableItems.Push(item);
        }

        protected override bool CanMoveToDumb(VirtualMap virtualMap, Point2D mapUnitPosition) =>
            throw new NotImplementedException();
    }
}