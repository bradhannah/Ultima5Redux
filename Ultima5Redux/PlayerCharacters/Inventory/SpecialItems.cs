using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class SpecialItems : InventoryItems<SpecialItem.ItemTypeSpriteEnum, SpecialItem>
    {
        [DataMember]
        public override Dictionary<SpecialItem.ItemTypeSpriteEnum, SpecialItem> Items { get; internal set; } =
            new();

        [JsonConstructor] private SpecialItems()
        {
        }

        public SpecialItems(ImportedGameState importedGameState)
        {
            // Carpet = 170, Grapple = 12, Spyglass = 89, HMSCape = 260, PocketWatch = 232, BlackBadge = 281,
            // WoodenBox = 270, Sextant = 256
            void addLegacyItem(SpecialItem.ItemTypeEnum specialItem,
                SpecialItem.ItemTypeSpriteEnum specialItemSprite) =>
                Items[specialItemSprite] = new SpecialItem(specialItemSprite,
                    importedGameState.GetSpecialItemQuantity(specialItem));

            addLegacyItem(SpecialItem.ItemTypeEnum.Carpet, SpecialItem.ItemTypeSpriteEnum.Carpet);
            addLegacyItem(SpecialItem.ItemTypeEnum.Grapple, SpecialItem.ItemTypeSpriteEnum.Grapple);
            addLegacyItem(SpecialItem.ItemTypeEnum.Spyglass, SpecialItem.ItemTypeSpriteEnum.Spyglass);
            addLegacyItem(SpecialItem.ItemTypeEnum.HMSCape, SpecialItem.ItemTypeSpriteEnum.HMSCape);
            addLegacyItem(SpecialItem.ItemTypeEnum.Sextant, SpecialItem.ItemTypeSpriteEnum.Sextant);
            addLegacyItem(SpecialItem.ItemTypeEnum.BlackBadge, SpecialItem.ItemTypeSpriteEnum.BlackBadge);
            addLegacyItem(SpecialItem.ItemTypeEnum.WoodenBox, SpecialItem.ItemTypeSpriteEnum.WoodenBox);
            Items[SpecialItem.ItemTypeSpriteEnum.PocketWatch] =
                new SpecialItem(SpecialItem.ItemTypeSpriteEnum.PocketWatch, 1);
        }
    }
}