using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class SpecialItems : InventoryItems<SpecialItem.ItemTypeSpriteEnum, SpecialItem>
    {
        [DataMember]
        public override Dictionary<SpecialItem.ItemTypeSpriteEnum, SpecialItem> Items { get; internal set; } =
            new Dictionary<SpecialItem.ItemTypeSpriteEnum, SpecialItem>();

        [JsonConstructor] private SpecialItems()
        {
        }
        
        public SpecialItems(List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
            // Carpet = 170, Grapple = 12, Spyglass = 89, HMSCape = 260, PocketWatch = 232, BlackBadge = 281,
            // WoodenBox = 270, Sextant = 256
            
            Items[SpecialItem.ItemTypeSpriteEnum.Carpet] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Carpet,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Carpet]);
            Items[SpecialItem.ItemTypeSpriteEnum.Grapple] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Grapple,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Grapple]);
            Items[SpecialItem.ItemTypeSpriteEnum.Spyglass] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Spyglass,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Spyglass]);
            Items[SpecialItem.ItemTypeSpriteEnum.HMSCape] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.HMSCape,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.HMSCape]);
            Items[SpecialItem.ItemTypeSpriteEnum.Sextant] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Sextant,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Sextant]);
            Items[SpecialItem.ItemTypeSpriteEnum.PocketWatch] =
                new SpecialItem(SpecialItem.ItemTypeSpriteEnum.PocketWatch, 1);
            Items[SpecialItem.ItemTypeSpriteEnum.BlackBadge] = new SpecialItem(
                SpecialItem.ItemTypeSpriteEnum.BlackBadge,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.BlackBadge]);
            Items[SpecialItem.ItemTypeSpriteEnum.WoodenBox] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.WoodenBox,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.WoodenBox]);
        }
    }
}