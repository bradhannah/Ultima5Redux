using System;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class WeaponReference : CombatItemReference
    {
        public enum WeaponTypeEnum
        {
            BareHands = -2, SmallShield = 0x21E, LargeShield = 0x21F, SpikedShield = 0x220, MagicShield = 0x221,
            JewelShield = 0x222, Dagger = 0x22a, Sling, Club, FlamingOil, MainGauche, Spear, ThrowingAxe, ShortSword,
            Mace, MorningStar, Bow, Arrows, Crossbow, Quarrels, LongSword, TwoHHammer, TwoHAxe, TwoHSword, Halberd,
            SwordofChaos, MagicBow, SilverSword, MagicAxe, GlassSword, JeweledSword, MysticSword
        }

        public override bool CanSell => BasePrice > 0 || IsAmmo;

        public WeaponTypeEnum WeaponType { get; }

        // public enum WeaponTypeSpriteEnum
        // {
        //     BareHands = 261, SmallShield = 262, LargeShield = 262, SpikedShield = 262, MagicShield = 262,
        //     JewelShield = 262, Dagger = 261, Sling = 261, Club = 261, FlamingOil = 261, MainGauche = 261, Spear = 261,
        //     ThrowingAxe = 261, ShortSword = 261, Mace = 261, MorningStar = 261, Bow = 261, Arrows = 261, Crossbow = 261,
        //     Quarrels = 261, LongSword = 261, TwoHHammer = 261, TwoHAxe = 261, TwoHSword = 261, Halberd = 261,
        //     SwordofChaos = 261, MagicBow = 261, SilverSword = 261, MagicAxe = 261, GlassSword = 261, JeweledSword = 261,
        //     MysticSword = 261
        // }

        public WeaponReference(DataOvlReference dataOvlReference, InventoryReference inventoryReference) :
            base(dataOvlReference, inventoryReference)
        {
            WeaponType =
                (WeaponTypeEnum)Enum.Parse(typeof(WeaponTypeEnum), inventoryReference.GetAsEquipment().ToString());
        }
    }
}