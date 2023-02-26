using System;
using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public static class InventoryItemFactory
    {
        // 259,ItemPotion,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 260,ItemScroll,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 261,ItemWeapon,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 262,ItemShield,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 265,ItemHelm,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 266,ItemRing,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 267,ItemArmour,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 268,ItemAnkh,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 270,ItemSandalwoodBox,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 436,Shard,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-2,Guess,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 437,Crown,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-2,Guess,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 438,Sceptre,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-2,Guess,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
        // 439,Amulet,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-2,Guess,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private enum SpriteRefs
        {
            Potion = 259,
            Scroll = 260,
            Weapon = 251,
            Shield = 262,
            Helm = 265,
            Ring = 266,
            Armour = 267,
            Ankh = 268,
            SandalwoodBox = 270,
            Shard = 436,
            Crown = 437,
            Sceptre = 438,
            Amulet = 439
        }

        private static CombatItem CreateArmament(InventoryReference itemReference)
        {
            CombatItemReference combatItemReference =
                GameReferences.Instance.CombatItemRefs.GetCombatItemReferenceFromEquipment(
                    itemReference.GetAsEquipment());

            return combatItemReference switch
            {
                WeaponReference weaponReference => new Weapon(weaponReference, 1),
                ArmourReference armourReference => armourReference.TheArmourType switch
                {
                    ArmourReference.ArmourType.Amulet => new Amulet(combatItemReference, 1),
                    ArmourReference.ArmourType.ChestArmour => new ChestArmour(combatItemReference, 1),
                    ArmourReference.ArmourType.Helm => new Helm(combatItemReference, 1),
                    ArmourReference.ArmourType.Ring => new Ring(combatItemReference, 1),
                    _ => throw new Ultima5ReduxException(
                        $"Tried to get inventory armour type and only got one - but was expecting more for {itemReference.ItemSpriteExposed}")
                },
                _ => throw new Ultima5ReduxException(
                    $"Tried to get inventory ref and only got one - but was expecting more for {itemReference.ItemSpriteExposed}")
            };
        }

        private static InventoryItem CreateItem(InventoryReference itemReference)
        {
            if (itemReference.InvRefType != InventoryReferences.InventoryReferenceType.Item)
                throw new Ultima5ReduxException(
                    $"Tried to get inventory ref and only got one - but was expecting more for {itemReference.ItemSpriteExposed}");

            switch (itemReference.ItemSpriteExposed)
            {
                case (int)ProvisionReferences.SpecificProvisionSpritesType.Torches:
                case (int)ProvisionReferences.SpecificProvisionSpritesType.Gems:
                case (int)ProvisionReferences.SpecificProvisionSpritesType.Keys:
                case (int)ProvisionReferences.SpecificProvisionSpritesType.Food:
                case (int)ProvisionReferences.SpecificProvisionSpritesType.Gold:
                    // skull keys work because they match with Keys sprite, but have a specific itemReference name
                    return new Provision(
                        GetEnumByInventoryName<ProvisionReferences.SpecificProvisionType>(itemReference),
                        itemReference.ItemSpriteExposed);
                case (int)SpriteRefs.Potion:
                    return new Potion(GetEnumByInventoryName<Potion.PotionColor>(itemReference), 1);
                case (int)SpriteRefs.Scroll:
                    if (itemReference.ItemName == "HMSCape")
                        return new SpecialItem(SpecialItem.SpecificItemType.HMSCape,
                            SpecialItem.SpecificItemTypeSprite.HMSCape, 1);

                    var spellWords =
                        GetEnumByInventoryName<MagicReference.SpellWords>(itemReference);
                    return new Scroll(spellWords, 1, GameReferences.Instance.MagicRefs.GetMagicReference(spellWords));
                case (int)SpriteRefs.Shard:
                    return new ShadowlordShard(GetEnumByInventoryName<ShadowlordShard.ShardType>(itemReference), 1);
                case (int)SpriteRefs.Crown:
                case (int)SpriteRefs.Sceptre:
                case (int)SpriteRefs.Amulet:
                    return new LordBritishArtifact(
                        GetEnumByInventoryName<LordBritishArtifact.ArtifactType>(itemReference), 1);
                case (int)SpecialItem.SpecificItemTypeSprite.Carpet:
                case 283:
                    return new SpecialItem(SpecialItem.SpecificItemType.Carpet,
                        SpecialItem.SpecificItemTypeSprite.Carpet, 1);
                case (int)SpecialItem.SpecificItemTypeSprite.Grapple:
                    return new SpecialItem(SpecialItem.SpecificItemType.Grapple,
                        SpecialItem.SpecificItemTypeSprite.Grapple, 1);
                case (int)SpecialItem.SpecificItemTypeSprite.Sextant:
                    return new SpecialItem(SpecialItem.SpecificItemType.Sextant,
                        SpecialItem.SpecificItemTypeSprite.Sextant, 1);
                case (int)SpecialItem.SpecificItemTypeSprite.Spyglass:
                    return new SpecialItem(SpecialItem.SpecificItemType.Spyglass,
                        SpecialItem.SpecificItemTypeSprite.Spyglass, 1);
                case (int)SpecialItem.SpecificItemTypeSprite.BlackBadge:
                    return new SpecialItem(SpecialItem.SpecificItemType.BlackBadge,
                        SpecialItem.SpecificItemTypeSprite.BlackBadge, 1);
                case (int)SpecialItem.SpecificItemTypeSprite.PocketWatch:
                    return new SpecialItem(SpecialItem.SpecificItemType.PocketWatch,
                        SpecialItem.SpecificItemTypeSprite.PocketWatch, 1);
                case (int)SpecialItem.SpecificItemTypeSprite.WoodenBox:
                    return new SpecialItem(SpecialItem.SpecificItemType.WoodenBox,
                        SpecialItem.SpecificItemTypeSprite.WoodenBox, 1);
                case Moonstone.MOONSTONE_SPRITE:
                    return new Moonstone(GetEnumByInventoryName<MoonPhaseReferences.MoonPhases>(itemReference));
                default:
                    throw new Ultima5ReduxException(
                        $"Tried to get inventory ref {itemReference.ItemSpriteExposed} but wasn't found");
            }
        }

        private static T GetEnumByInventoryName<T>(InventoryReference inventoryReference) where T : Enum
        {
            var enumResult = (T)Enum.Parse(typeof(T), inventoryReference.ItemName);
            return enumResult;
        }

        public static InventoryItem Create(InventoryReference itemReference)
        {
            switch (itemReference.InvRefType)
            {
                case InventoryReferences.InventoryReferenceType.Reagent:
                    return new Reagent(GetEnumByInventoryName<Reagent.SpecificReagentType>(itemReference), 1);
                case InventoryReferences.InventoryReferenceType.Armament:
                    return CreateArmament(itemReference);
                case InventoryReferences.InventoryReferenceType.Spell:
                    var spellWords =
                        GetEnumByInventoryName<MagicReference.SpellWords>(itemReference);
                    return new Spell(spellWords, 1);
                case InventoryReferences.InventoryReferenceType.Item:
                    return CreateItem(itemReference);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}