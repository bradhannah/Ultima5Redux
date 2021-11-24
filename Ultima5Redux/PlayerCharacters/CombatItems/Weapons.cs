using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public sealed class Weapons : CombatItems<WeaponReference.WeaponTypeEnum, Weapon>
    {
        [DataMember] public override Dictionary<WeaponReference.WeaponTypeEnum, Weapon> Items { get; internal set; } =
            new Dictionary<WeaponReference.WeaponTypeEnum, Weapon>();

        [IgnoreDataMember] private Dictionary<DataOvlReference.Equipment, Weapon> ItemsFromEquipment { get; } =
            new Dictionary<DataOvlReference.Equipment, Weapon>();

        [JsonConstructor] private Weapons()
        {
        }

        public Weapons(List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
            foreach (WeaponReference weaponReference in GameReferences.CombatItemRefs.WeaponReferences)
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

        public Weapon GetWeaponFromEquipment(DataOvlReference.Equipment equipment)
        {
            if (equipment == DataOvlReference.Equipment.Nothing) return null;
            if (!ItemsFromEquipment.ContainsKey(equipment)) return null;
            return ItemsFromEquipment[equipment];
        }
    }
}