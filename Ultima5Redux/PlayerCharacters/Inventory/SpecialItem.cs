using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class SpecialItem : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpecificItemType
        {
            Carpet = 0x20A, Grapple = 0x209, Spyglass = 0x214, HMSCape = 0x215, PocketWatch = 0, BlackBadge = 0x218,
            WoodenBox = 0x219, Sextant = 0x216
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpecificItemTypeSprite
        {
            Carpet = 170, Grapple = 12, Spyglass = 89, HMSCape = 260, PocketWatch = 232, BlackBadge = 88,
            WoodenBox = 270, Sextant = 256
        }

        [DataMember] public SpecificItemType ItemType { get; private set; }

        [IgnoreDataMember] public override string FindDescription => InvRef.FriendlyItemName;

        [IgnoreDataMember] public override bool HideQuantity => ItemType != SpecificItemType.Carpet;

        [IgnoreDataMember] public override string InventoryReferenceString => ItemType.ToString();

        [IgnoreDataMember] public override bool IsSellable => false;

        [IgnoreDataMember] public bool HasOneOrMore => Quantity > 0;

        [JsonConstructor] private SpecialItem()
        {
        }

        public SpecialItem(SpecificItemType itemType, SpecificItemTypeSprite specificItemTypeSprite, int quantity) :
            base(quantity, (int)specificItemTypeSprite,
                InventoryReferences.InventoryReferenceType.Item)
        {
            ItemType = itemType;
        }

        public static SpecificItemType GetItemOffset(SpecificItemTypeSprite specificItemTypeSprite)
        {
            SpecificItemType specificItemType =
                (SpecificItemType)Enum.Parse(typeof(SpecificItemType), specificItemTypeSprite.ToString());
            return specificItemType;
        }
    }
}