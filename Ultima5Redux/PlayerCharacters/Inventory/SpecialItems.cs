using System.Collections.Generic;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public sealed class SpecialItems : InventoryItems<SpecialItem.ItemTypeSpriteEnum, SpecialItem>
    {
        public override Dictionary<SpecialItem.ItemTypeSpriteEnum, SpecialItem> Items { get; } =
            new Dictionary<SpecialItem.ItemTypeSpriteEnum, SpecialItem>();

        public SpecialItems(List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
            //   Carpet = 170, Grapple = 12, Spyglass = 89, HMSCape = 260, PocketWatch = 232, BlackBadge = 281,
            //WoodenBox = 270, Sextant = 256
            Items[SpecialItem.ItemTypeSpriteEnum.Carpet] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Carpet,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Carpet]);
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNamesStrings.MAGIC_CRPT),
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNamesStrings.MAGIC_CRPT));
            Items[SpecialItem.ItemTypeSpriteEnum.Grapple] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Grapple,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Grapple]);
            // "Grappling Hook",
            // "Grapple");
            Items[SpecialItem.ItemTypeSpriteEnum.Spyglass] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Spyglass,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Spyglass]);
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.SPYGLASS),
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.SPYGLASS));
            Items[SpecialItem.ItemTypeSpriteEnum.HMSCape] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.HMSCape,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.HMSCape]);
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.HMS_CAPE_PLAN),
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.HMS_CAPE_PLAN));
            Items[SpecialItem.ItemTypeSpriteEnum.Sextant] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Sextant,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Sextant]);
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.SEXTANT),
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.SEXTANT));
            Items[SpecialItem.ItemTypeSpriteEnum.PocketWatch] =
                new SpecialItem(SpecialItem.ItemTypeSpriteEnum.PocketWatch, 1);
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.POCKET_WATCH),
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.POCKET_WATCH));
            Items[SpecialItem.ItemTypeSpriteEnum.BlackBadge] = new SpecialItem(
                SpecialItem.ItemTypeSpriteEnum.BlackBadge,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.BlackBadge]);
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.BLACK_BADGE),
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.BLACK_BADGE));
            Items[SpecialItem.ItemTypeSpriteEnum.WoodenBox] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.WoodenBox,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.WoodenBox]);
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.WOODEN_BOX),
            // dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.WOODEN_BOX));
        }
    }
}