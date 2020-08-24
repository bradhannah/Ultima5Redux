using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Weapons : CombatItems<Weapon.WeaponTypeEnum, Weapon>
    {
        public override Dictionary<Weapon.WeaponTypeEnum, Weapon> Items { get; } = new Dictionary<Weapon.WeaponTypeEnum, Weapon>();
        private List<string> _equipmentNames;

        private Dictionary<DataOvlReference.Equipment, Weapon> ItemsFromEquipment { get; } = new Dictionary<DataOvlReference.Equipment, Weapon>();
        public Weapon GetWeaponFromEquipment(DataOvlReference.Equipment equipment)
        {
            if (equipment == DataOvlReference.Equipment.Nothing)
            {
                return null;
            }
            if (!ItemsFromEquipment.ContainsKey(equipment))
            {
                return null;
            }
            return ItemsFromEquipment[equipment];
        }

        public Weapons(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            _equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES).GetAsStringListFromIndexes();

            // we need to actually add shields because they can be equipped as weapons
            // but you should not expose shields twice in the UI
            AddWeapon(Weapon.WeaponTypeEnum.SmallShield, Weapon.WeaponTypeSpriteEnum.SmallShield, DataOvlReference.Equipment.SmallShield);
            AddWeapon(Weapon.WeaponTypeEnum.LargeShield, Weapon.WeaponTypeSpriteEnum.LargeShield, DataOvlReference.Equipment.LargeShield);
            AddWeapon(Weapon.WeaponTypeEnum.SpikedShield, Weapon.WeaponTypeSpriteEnum.SpikedShield, DataOvlReference.Equipment.SpikedShield);
            AddWeapon(Weapon.WeaponTypeEnum.MagicShield, Weapon.WeaponTypeSpriteEnum.MagicShield, DataOvlReference.Equipment.MagicShield);
            AddWeapon(Weapon.WeaponTypeEnum.JewelShield, Weapon.WeaponTypeSpriteEnum.JewelShield, DataOvlReference.Equipment.JewelShield);

            AddWeapon(Weapon.WeaponTypeEnum.Dagger, Weapon.WeaponTypeSpriteEnum.Dagger, DataOvlReference.Equipment.Dagger);
            AddWeapon(Weapon.WeaponTypeEnum.Sling, Weapon.WeaponTypeSpriteEnum.Sling, DataOvlReference.Equipment.Sling);
            AddWeapon(Weapon.WeaponTypeEnum.Club, Weapon.WeaponTypeSpriteEnum.Club, DataOvlReference.Equipment.Club);
            AddWeapon(Weapon.WeaponTypeEnum.FlamingOil, Weapon.WeaponTypeSpriteEnum.FlamingOil, DataOvlReference.Equipment.FlamingOil);
            AddWeapon(Weapon.WeaponTypeEnum.MainGauche, Weapon.WeaponTypeSpriteEnum.MainGauche, DataOvlReference.Equipment.MainGauche);
            AddWeapon(Weapon.WeaponTypeEnum.Spear, Weapon.WeaponTypeSpriteEnum.Spear, DataOvlReference.Equipment.Spear);
            AddWeapon(Weapon.WeaponTypeEnum.ThrowingAxe, Weapon.WeaponTypeSpriteEnum.ThrowingAxe, DataOvlReference.Equipment.ThrowingAxe);
            AddWeapon(Weapon.WeaponTypeEnum.ShortSword, Weapon.WeaponTypeSpriteEnum.ShortSword, DataOvlReference.Equipment.ShortSword);
            AddWeapon(Weapon.WeaponTypeEnum.Mace, Weapon.WeaponTypeSpriteEnum.Mace, DataOvlReference.Equipment.Mace);
            AddWeapon(Weapon.WeaponTypeEnum.MorningStar, Weapon.WeaponTypeSpriteEnum.MorningStar, DataOvlReference.Equipment.MorningStar);
            AddWeapon(Weapon.WeaponTypeEnum.Bow, Weapon.WeaponTypeSpriteEnum.Bow, DataOvlReference.Equipment.Bow);
            AddWeapon(Weapon.WeaponTypeEnum.Arrows, Weapon.WeaponTypeSpriteEnum.Arrows, DataOvlReference.Equipment.Arrows);
            AddWeapon(Weapon.WeaponTypeEnum.Crossbow, Weapon.WeaponTypeSpriteEnum.Crossbow, DataOvlReference.Equipment.Crossbow);
            AddWeapon(Weapon.WeaponTypeEnum.Quarrels, Weapon.WeaponTypeSpriteEnum.Quarrels, DataOvlReference.Equipment.Quarrels);
            AddWeapon(Weapon.WeaponTypeEnum.LongSword, Weapon.WeaponTypeSpriteEnum.LongSword, DataOvlReference.Equipment.LongSword);
            AddWeapon(Weapon.WeaponTypeEnum.TwoHHammer, Weapon.WeaponTypeSpriteEnum.TwoHHammer, DataOvlReference.Equipment.TwoHHammer);
            AddWeapon(Weapon.WeaponTypeEnum.TwoHAxe, Weapon.WeaponTypeSpriteEnum.TwoHAxe, DataOvlReference.Equipment.TwoHAxe);
            AddWeapon(Weapon.WeaponTypeEnum.TwoHSword, Weapon.WeaponTypeSpriteEnum.TwoHSword, DataOvlReference.Equipment.TwoHSword);
            AddWeapon(Weapon.WeaponTypeEnum.Halberd, Weapon.WeaponTypeSpriteEnum.Halberd, DataOvlReference.Equipment.Halberd);
            AddWeapon(Weapon.WeaponTypeEnum.SwordofChaos, Weapon.WeaponTypeSpriteEnum.SwordofChaos, DataOvlReference.Equipment.SwordofChaos);
            AddWeapon(Weapon.WeaponTypeEnum.MagicBow, Weapon.WeaponTypeSpriteEnum.MagicBow, DataOvlReference.Equipment.MagicBow);
            AddWeapon(Weapon.WeaponTypeEnum.SilverSword, Weapon.WeaponTypeSpriteEnum.SilverSword, DataOvlReference.Equipment.SilverSword);
            AddWeapon(Weapon.WeaponTypeEnum.MagicAxe, Weapon.WeaponTypeSpriteEnum.MagicAxe, DataOvlReference.Equipment.MagicAxe);
            AddWeapon(Weapon.WeaponTypeEnum.GlassSword, Weapon.WeaponTypeSpriteEnum.GlassSword, DataOvlReference.Equipment.GlassSword);
            AddWeapon(Weapon.WeaponTypeEnum.JeweledSword, Weapon.WeaponTypeSpriteEnum.JeweledSword, DataOvlReference.Equipment.JeweledSword);
            AddWeapon(Weapon.WeaponTypeEnum.MysticSword, Weapon.WeaponTypeSpriteEnum.MysticSword, DataOvlReference.Equipment.MysticSword);
        }

        private void AddWeapon(Weapon.WeaponTypeEnum weapon, Weapon.WeaponTypeSpriteEnum weaponSprite, DataOvlReference.Equipment equipment)
        {
            Weapon newWeapon = new Weapon(weapon, weaponSprite, equipment, DataOvlRef, GameStateByteArray);
            Items.Add(weapon, newWeapon);
            ItemsFromEquipment.Add(equipment, newWeapon);
        }
    }
}