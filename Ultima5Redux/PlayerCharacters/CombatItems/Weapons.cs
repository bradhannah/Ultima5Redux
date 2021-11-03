using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Weapons : CombatItems<WeaponReference.WeaponTypeEnum, Weapon>
    {
        public Weapons(CombatItemReferences combatItemReferences, List<byte> gameStateByteArray)
        : base(combatItemReferences, gameStateByteArray)
        {
            foreach (WeaponReference weaponReference in combatItemReferences.WeaponReferences)
            {
                AddWeapon(weaponReference);
            }
        }

        private void AddWeapon(WeaponReference weaponReference)
        {
            Weapon newWeapon;

            if (weaponReference.SpecificEquipment == DataOvlReference.Equipment.BareHands)
            {
                newWeapon = new Weapon(weaponReference, 262);
            }
            else
            {
                newWeapon = new Weapon(weaponReference, GameStateByteArray[(int)weaponReference.SpecificEquipment]);
            }
            
            Items.Add(weaponReference.WeaponType, newWeapon);
            
            ItemsFromEquipment.Add(weaponReference.SpecificEquipment, newWeapon);
        }
        
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


    }
}