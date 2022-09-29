using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    public sealed class CombatItemReferences
    {
        private enum CombatItemType { Armour, Weapon, Other }

        private readonly List<ArmourReference> _allArmour = new();
        private readonly List<ArmourReference> _amulets = new();
        private readonly List<ArmourReference> _chestArmours = new();

        private readonly Dictionary<DataOvlReference.Equipment, CombatItemReference> _equipmentToCombatItemReference =
            new();

        private readonly List<ArmourReference> _helms = new();
        private readonly List<ArmourReference> _rings = new();
        private readonly List<WeaponReference> _weaponReferences = new();

        public ReadOnlyCollection<ArmourReference> AllArmour => _allArmour.AsReadOnly();
        public ReadOnlyCollection<ArmourReference> Amulets => _amulets.AsReadOnly();
        public ReadOnlyCollection<ArmourReference> ChestArmours => _chestArmours.AsReadOnly();
        public ReadOnlyCollection<ArmourReference> Helms => _helms.AsReadOnly();
        public ReadOnlyCollection<ArmourReference> Rings => _rings.AsReadOnly();

        public ReadOnlyCollection<WeaponReference> WeaponReferences => _weaponReferences.AsReadOnly();

        public CombatItemReferences(InventoryReferences inventoryReferences)
        {
            List<InventoryReference> combatItems =
                inventoryReferences.GetInventoryReferenceList(InventoryReferences.InventoryReferenceType.Armament);
            // foreach of the weapon references
            foreach (InventoryReference inventoryReference in combatItems)
            {
                DataOvlReference.Equipment equipment = inventoryReference.GetAsEquipment();
                switch (GetCombatItemTypeByEquipment(equipment))
                {
                    case CombatItemType.Armour:
                        ArmourReference armourReference = new(GameReferences.DataOvlRef, inventoryReference);
                        _equipmentToCombatItemReference.Add(equipment, armourReference);

                        _allArmour.Add(armourReference);
                        switch (armourReference.TheArmourType)
                        {
                            case ArmourReference.ArmourType.Amulet:
                                _amulets.Add(armourReference);
                                break;
                            case ArmourReference.ArmourType.ChestArmour:
                                _chestArmours.Add(armourReference);
                                break;
                            case ArmourReference.ArmourType.Helm:
                                _helms.Add(armourReference);
                                break;
                            case ArmourReference.ArmourType.Ring:
                                _rings.Add(armourReference);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(inventoryReferences),
                                    @"Bad combat item (armour) reference type");
                        }

                        break;
                    case CombatItemType.Weapon:
                        WeaponReference weaponReference = new(GameReferences.DataOvlRef, inventoryReference);
                        _weaponReferences.Add(weaponReference);
                        _equipmentToCombatItemReference.Add(equipment, weaponReference);
                        break;
                    case CombatItemType.Other:
                        throw new Ultima5ReduxException("Tried to create CombatItemReference from " + equipment);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(inventoryReferences),
                            @"Bad combat item reference type");
                }
            }
        }

        internal static bool EquipmentMatches(Array theArray, ref DataOvlReference.Equipment equipment)
        {
            if (theArray == null)
                throw new Ultima5ReduxException("Tried to match equipment, but passed in null array");

            foreach (object theEnum in theArray)
            {
                if (string.Equals(equipment.ToString(), theEnum.ToString(),
                        StringComparison.CurrentCultureIgnoreCase)) return true;
            }

            return false;
        }

        private static CombatItemType GetCombatItemTypeByEquipment(DataOvlReference.Equipment equipment)
        {
            if (EquipmentMatches(Enum.GetValues(typeof(WeaponReference.SpecificWeaponType)), ref equipment))
                return CombatItemType.Weapon;
            if (EquipmentMatches(Enum.GetValues(typeof(ArmourReference.SpecificHelm)), ref equipment))
                return CombatItemType.Armour;
            if (EquipmentMatches(Enum.GetValues(typeof(ArmourReference.SpecificChestArmour)), ref equipment))
                return CombatItemType.Armour;
            if (EquipmentMatches(Enum.GetValues(typeof(ArmourReference.SpecificRing)), ref equipment))
                return CombatItemType.Armour;
            if (EquipmentMatches(Enum.GetValues(typeof(ArmourReference.SpecificAmulet)), ref equipment))
                return CombatItemType.Armour;

            throw new Ultima5ReduxException("Tried to create CombatItemReference from " + equipment);
        }

        public ArmourReference GetArmourReferenceFromEquipment(DataOvlReference.Equipment equipment)
        {
            return AllArmour.FirstOrDefault(e => e.SpecificEquipment == equipment);
        }

        public WeaponReference GetWeaponReferenceFromEquipment(DataOvlReference.Equipment equipment)
        {
            return WeaponReferences.FirstOrDefault(e => e.SpecificEquipment == equipment);
        }

        public CombatItemReference GetCombatItemReferenceFromEquipment(DataOvlReference.Equipment equipment) =>
            _equipmentToCombatItemReference[equipment];
    }
}