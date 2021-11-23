using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class SpecialItem : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum ItemTypeEnum
        {
            Carpet = 0x20A, Grapple = 0x209, Spyglass = 0x214, HMSCape = 0x215, PocketWatch = 0, BlackBadge = 0x218,
            WoodenBox = 0x219, Sextant = 0x216
        }

        [JsonConverter(typeof(StringEnumConverter))] public enum ItemTypeSpriteEnum
        {
            Carpet = 170, Grapple = 12, Spyglass = 89, HMSCape = 260, PocketWatch = 232, BlackBadge = 281,
            WoodenBox = 270, Sextant = 256
        }

        [IgnoreDataMember] public override string FindDescription => InvRef.FriendlyItemName;
        
        [IgnoreDataMember] public override bool HideQuantity => ItemType != ItemTypeSpriteEnum.Carpet;

        [IgnoreDataMember] public override string InventoryReferenceString => ItemType.ToString();

        [IgnoreDataMember] public override bool IsSellable => false;

        [IgnoreDataMember] public bool HasOneOfMore => Quantity > 0;

        [DataMember] public ItemTypeSpriteEnum ItemType { get; private set; }

        [JsonConstructor] private SpecialItem()
        {
        }
        
        public SpecialItem(ItemTypeSpriteEnum itemType, int quantity) : base(quantity, (int)itemType)
        {
            ItemType = itemType;
        }

        public static ItemTypeEnum GetItemOffset(ItemTypeSpriteEnum itemTypeSpriteEnum)
        {
            ItemTypeEnum itemType = (ItemTypeEnum)Enum.Parse(typeof(ItemTypeEnum), itemTypeSpriteEnum.ToString());
            return itemType;
        }
    }
}