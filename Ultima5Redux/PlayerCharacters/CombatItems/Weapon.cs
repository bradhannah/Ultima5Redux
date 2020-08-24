using System.Collections.Generic;
using Ultima5Redux.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Weapon : CombatItem
    {
        public enum WeaponTypeEnum {
            SmallShield = 0x21E, LargeShield = 0x21F, SpikedShield = 0x220, MagicShield = 0x221, JewelShield = 0x222,
            Dagger = 0x22a, Sling, Club, FlamingOil, MainGauche, Spear, ThrowingAxe, ShortSword, Mace,
            MorningStar, Bow, Arrows, Crossbow, Quarrels, LongSword, TwoHHammer, TwoHAxe, TwoHSword, Halberd, SwordofChaos,
            MagicBow, SilverSword, MagicAxe, GlassSword, JeweledSword, MysticSword
        }

        public enum WeaponTypeSpriteEnum {
            SmallShield = 262, LargeShield = 262, SpikedShield = 262, MagicShield = 262, JewelShield = 262,
            Dagger = 261, Sling = 261, Club = 261, FlamingOil = 261, MainGauche = 261,
            Spear = 261, ThrowingAxe = 261, ShortSword = 261, Mace = 261,
            MorningStar = 261, Bow = 261, Arrows = 261, Crossbow = 261, Quarrels = 261, 
            LongSword = 261, TwoHHammer = 261, TwoHAxe = 261, TwoHSword = 261, Halberd = 261, 
            SwordofChaos = 261, MagicBow = 261, SilverSword = 261, MagicAxe = 261, 
            GlassSword = 261, JeweledSword = 261, MysticSword = 261
        }
        
        public WeaponTypeEnum WeaponType { get; }

        public bool IsAmmo { get; }

        public bool IsTwoHanded { get; }

        public bool IsShield { get; }

        public override bool CanSell => BasePrice > 0 || IsAmmo;

        public override bool HideQuantity => false;
        
        public Weapon(WeaponTypeEnum weapon, WeaponTypeSpriteEnum sprite, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef, IReadOnlyList<byte> gameStateByteArray)
            : base (equipment, dataOvlRef, gameStateByteArray, (int)weapon, (int)sprite)
        {
            IsAmmo = equipment == DataOvlReference.Equipment.Quarrels || equipment == DataOvlReference.Equipment.Arrows;
            IsTwoHanded = equipment == DataOvlReference.Equipment.TwoHAxe || equipment == DataOvlReference.Equipment.TwoHSword ||
                          equipment == DataOvlReference.Equipment.TwoHHammer || equipment == DataOvlReference.Equipment.Bow || equipment == DataOvlReference.Equipment.MagicBow ||
                          equipment == DataOvlReference.Equipment.Crossbow || equipment == DataOvlReference.Equipment.Halberd || equipment == DataOvlReference.Equipment.FlamingOil;
            IsShield = equipment == DataOvlReference.Equipment.SmallShield || equipment == DataOvlReference.Equipment.LargeShield ||
                       equipment == DataOvlReference.Equipment.SpikedShield || equipment == DataOvlReference.Equipment.MagicShield || 
                       equipment == DataOvlReference.Equipment.JewelShield;
        }

      
    }
}