using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Weapons : CombatItems<WeaponReference.WeaponTypeEnum, Weapon>
    {
        //private List<string> _equipmentNames;

        public Weapons(CombatItemReferences combatItemReferences, List<byte> gameStateByteArray)
        : base(combatItemReferences, gameStateByteArray)
        {
            foreach (WeaponReference weaponReference in combatItemReferences.WeaponReferences)
            {
                AddWeapon(weaponReference);
            }
        }

        private void AddWeapon(WeaponReference weaponReference)
            //WeaponReference.WeaponTypeEnum weapon, WeaponReference.WeaponTypeSpriteEnum weaponSprite,
            //DataOvlReference.Equipment equipment)
        {
            Weapon newWeapon;

            if (weaponReference.SpecificEquipment == DataOvlReference.Equipment.BareHands)
            {
                newWeapon = new Weapon(weaponReference, 262);
                //weaponReference.SpecificEquipment, weaponSprite, equipment, DataOvlRef, 262);
            }
            else
            {
                newWeapon = new Weapon(weaponReference, GameStateByteArray[(int)weaponReference.SpecificEquipment]);
            }
                //newWeapon = new Weapon(weaponReference.SpecificEquipment, weaponSprite, equipment, DataOvlRef, GameStateByteArray[(int)equipment]);
            
            Items.Add(weaponReference.WeaponType, newWeapon);
            
            ItemsFromEquipment.Add(weaponReference.SpecificEquipment, newWeapon);
        }
        
        // public Weapons(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef,
        //     gameStateByteArray)
        // {
        //     // _equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES)
        //     //     .GetAsStringListFromIndexes();
        //
        //     // we need to actually add shields because they can be equipped as weapons
        //     // but you should not expose shields twice in the UI
        //     AddWeapon(WeaponReference.WeaponTypeEnum.BareHands, WeaponReference.WeaponTypeSpriteEnum.BareHands,
        //         DataOvlReference.Equipment.BareHands);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.SmallShield, WeaponReference.WeaponTypeSpriteEnum.SmallShield,
        //         DataOvlReference.Equipment.SmallShield);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.LargeShield, WeaponReference.WeaponTypeSpriteEnum.LargeShield,
        //         DataOvlReference.Equipment.LargeShield);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.SpikedShield, WeaponReference.WeaponTypeSpriteEnum.SpikedShield,
        //         DataOvlReference.Equipment.SpikedShield);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.MagicShield, WeaponReference.WeaponTypeSpriteEnum.MagicShield,
        //         DataOvlReference.Equipment.MagicShield);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.JewelShield, WeaponReference.WeaponTypeSpriteEnum.JewelShield,
        //         DataOvlReference.Equipment.JewelShield);
        //
        //     AddWeapon(WeaponReference.WeaponTypeEnum.Dagger, WeaponReference.WeaponTypeSpriteEnum.Dagger,
        //         DataOvlReference.Equipment.Dagger);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.Sling, WeaponReference.WeaponTypeSpriteEnum.Sling, DataOvlReference.Equipment.Sling);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.Club, WeaponReference.WeaponTypeSpriteEnum.Club, DataOvlReference.Equipment.Club);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.FlamingOil, WeaponReference.WeaponTypeSpriteEnum.FlamingOil,
        //         DataOvlReference.Equipment.FlamingOil);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.MainGauche, WeaponReference.WeaponTypeSpriteEnum.MainGauche,
        //         DataOvlReference.Equipment.MainGauche);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.Spear, WeaponReference.WeaponTypeSpriteEnum.Spear, DataOvlReference.Equipment.Spear);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.ThrowingAxe, WeaponReference.WeaponTypeSpriteEnum.ThrowingAxe,
        //         DataOvlReference.Equipment.ThrowingAxe);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.ShortSword, WeaponReference.WeaponTypeSpriteEnum.ShortSword,
        //         DataOvlReference.Equipment.ShortSword);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.Mace, WeaponReference.WeaponTypeSpriteEnum.Mace, DataOvlReference.Equipment.Mace);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.MorningStar, WeaponReference.WeaponTypeSpriteEnum.MorningStar,
        //         DataOvlReference.Equipment.MorningStar);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.Bow, WeaponReference.WeaponTypeSpriteEnum.Bow, DataOvlReference.Equipment.Bow);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.Arrows, WeaponReference.WeaponTypeSpriteEnum.Arrows,
        //         DataOvlReference.Equipment.Arrows);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.Crossbow, WeaponReference.WeaponTypeSpriteEnum.Crossbow,
        //         DataOvlReference.Equipment.Crossbow);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.Quarrels, WeaponReference.WeaponTypeSpriteEnum.Quarrels,
        //         DataOvlReference.Equipment.Quarrels);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.LongSword, WeaponReference.WeaponTypeSpriteEnum.LongSword,
        //         DataOvlReference.Equipment.LongSword);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.TwoHHammer, WeaponReference.WeaponTypeSpriteEnum.TwoHHammer,
        //         DataOvlReference.Equipment.TwoHHammer);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.TwoHAxe, WeaponReference.WeaponTypeSpriteEnum.TwoHAxe,
        //         DataOvlReference.Equipment.TwoHAxe);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.TwoHSword, WeaponReference.WeaponTypeSpriteEnum.TwoHSword,
        //         DataOvlReference.Equipment.TwoHSword);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.Halberd, WeaponReference.WeaponTypeSpriteEnum.Halberd,
        //         DataOvlReference.Equipment.Halberd);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.SwordofChaos, WeaponReference.WeaponTypeSpriteEnum.SwordofChaos,
        //         DataOvlReference.Equipment.SwordofChaos);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.MagicBow, WeaponReference.WeaponTypeSpriteEnum.MagicBow,
        //         DataOvlReference.Equipment.MagicBow);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.SilverSword, WeaponReference.WeaponTypeSpriteEnum.SilverSword,
        //         DataOvlReference.Equipment.SilverSword);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.MagicAxe, WeaponReference.WeaponTypeSpriteEnum.MagicAxe,
        //         DataOvlReference.Equipment.MagicAxe);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.GlassSword, WeaponReference.WeaponTypeSpriteEnum.GlassSword,
        //         DataOvlReference.Equipment.GlassSword);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.JeweledSword, WeaponReference.WeaponTypeSpriteEnum.JeweledSword,
        //         DataOvlReference.Equipment.JeweledSword);
        //     AddWeapon(WeaponReference.WeaponTypeEnum.MysticSword, WeaponReference.WeaponTypeSpriteEnum.MysticSword,
        //         DataOvlReference.Equipment.MysticSword);
        // }

        private Dictionary<DataOvlReference.Equipment, Weapon> ItemsFromEquipment { get; } =
            new Dictionary<DataOvlReference.Equipment, Weapon>();

        public override Dictionary<WeaponReference.WeaponTypeEnum, Weapon> Items { get; } =
            new Dictionary<WeaponReference.WeaponTypeEnum, Weapon>();

        public Weapon GetWeaponFromEquipment(DataOvlReference.Equipment equipment)
        {
            if (equipment == DataOvlReference.Equipment.Nothing) return null;
            if (!ItemsFromEquipment.ContainsKey(equipment)) return null;
            return ItemsFromEquipment[equipment];
        }

        // private void AddWeapon(WeaponReference.WeaponTypeEnum weapon, WeaponReference.WeaponTypeSpriteEnum weaponSprite,
        //     DataOvlReference.Equipment equipment)
        // {
        //     Weapon newWeapon;
        //
        //     if (equipment == DataOvlReference.Equipment.BareHands)
        //     {
        //         newWeapon = new Weapon(weapon, weaponSprite, equipment, DataOvlRef, 262);
        //     }
        //     else
        //     {
        //         newWeapon = new Weapon(weapon, weaponSprite, equipment, DataOvlRef, GameStateByteArray[(int)equipment]);
        //     }
        //
        //     Items.Add(weapon, newWeapon);
        //     ItemsFromEquipment.Add(equipment, newWeapon);
        // }
    }
}