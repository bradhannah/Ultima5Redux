﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class SpecialItems : InventoryItems<SpecialItem.SpecificItemTypeSprite, SpecialItem>
    {
        [DataMember]
        public override Dictionary<SpecialItem.SpecificItemTypeSprite, SpecialItem> Items { get; internal set; } =
            new();

        [JsonConstructor] private SpecialItems()
        {
        }

        public SpecialItems(ImportedGameState importedGameState)
        {
            // Carpet = 170, Grapple = 12, Spyglass = 89, HMSCape = 260, PocketWatch = 232, BlackBadge = 281,
            // WoodenBox = 270, Sextant = 256
            void addLegacyItem(SpecialItem.SpecificItemType specialItem,
                SpecialItem.SpecificItemTypeSprite specialItemSprite) =>
                Items[specialItemSprite] = new SpecialItem(specialItem,
                    importedGameState.GetSpecialItemQuantity(specialItem));

            addLegacyItem(SpecialItem.SpecificItemType.Carpet, SpecialItem.SpecificItemTypeSprite.Carpet);
            addLegacyItem(SpecialItem.SpecificItemType.Grapple, SpecialItem.SpecificItemTypeSprite.Grapple);
            addLegacyItem(SpecialItem.SpecificItemType.Spyglass, SpecialItem.SpecificItemTypeSprite.Spyglass);
            addLegacyItem(SpecialItem.SpecificItemType.HMSCape, SpecialItem.SpecificItemTypeSprite.HMSCape);
            addLegacyItem(SpecialItem.SpecificItemType.Sextant, SpecialItem.SpecificItemTypeSprite.Sextant);
            addLegacyItem(SpecialItem.SpecificItemType.BlackBadge, SpecialItem.SpecificItemTypeSprite.BlackBadge);
            addLegacyItem(SpecialItem.SpecificItemType.WoodenBox, SpecialItem.SpecificItemTypeSprite.WoodenBox);
            Items[SpecialItem.SpecificItemTypeSprite.PocketWatch] =
                new SpecialItem(SpecialItem.SpecificItemType.PocketWatch, 1);
        }
    }
}