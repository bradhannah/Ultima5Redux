using System;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class SpecialItem : InventoryItem
    {
        public override bool HideQuantity
        {
            get
            {
                if (ItemType == ItemTypeSpriteEnum.Carpet) return false;
                return true;
            }
        }

        public override bool IsSellable => false;

        public enum ItemTypeEnum
        {
            Carpet = 0x20A, Grapple = 0x209, Spyglass = 0x214, HMSCape = 0x215, PocketWatch = 0, BlackBadge = 0x218,
            WoodenBox = 0x219, Sextant = 0x216
        };

        public enum ItemTypeSpriteEnum
        {
            Carpet = 170, Grapple = 12, Spyglass = 89, HMSCape = 260, PocketWatch = 232, BlackBadge = 281,
            WoodenBox = 270, Sextant = 256
        }

        public ItemTypeSpriteEnum ItemType { get; }

        public static ItemTypeEnum GetItemOffset(ItemTypeSpriteEnum itemTypeSpriteEnum)
        {
            ItemTypeEnum itemType = (ItemTypeEnum)Enum.Parse(typeof(ItemTypeEnum), itemTypeSpriteEnum.ToString());
            return itemType;
        }

        public SpecialItem(ItemTypeSpriteEnum itemType, int quantity, string longName, string shortName) : 
            base(quantity, longName, shortName, (int)itemType)
        {
            ItemType = itemType;
        }
    }
}