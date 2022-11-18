using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public sealed class Weapons : CombatItems<WeaponReference.SpecificWeaponType, Weapon>
    {
        [DataMember]
        public override Dictionary<WeaponReference.SpecificWeaponType, Weapon> Items { get; internal set; } =
            new();

        [IgnoreDataMember] private Dictionary<DataOvlReference.Equipment, Weapon> ItemsFromEquipment { get; } = new();

        [JsonConstructor] private Weapons()
        {
        }

        public Weapons(ImportedGameState importedGameState)
        {
            void addWeaponLegacy(WeaponReference weaponReference)
            {
                AddWeapon(weaponReference, importedGameState.GetEquipmentQuantity(weaponReference.SpecificEquipment));
            }

            foreach (WeaponReference weaponReference in GameReferences.Instance.CombatItemRefs.WeaponReferences)
            {
                addWeaponLegacy(weaponReference);
            }
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            if (ItemsFromEquipment.Count > 0) return;
            foreach (Weapon weapon in Items.Values)
            {
                ItemsFromEquipment.Add(weapon.SpecificEquipment, weapon);
            }
        }

        private void AddWeapon(WeaponReference weaponReference, int nQuantity)
        {
            Weapon newWeapon = weaponReference.SpecificEquipment == DataOvlReference.Equipment.BareHands
                ? new Weapon(weaponReference, 262)
                : new Weapon(weaponReference, nQuantity);

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