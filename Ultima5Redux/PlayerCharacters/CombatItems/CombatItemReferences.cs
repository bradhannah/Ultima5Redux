using System;
using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class CombatItemReferences
    {
        private readonly DataOvlReference _dataOvlReference;
        private readonly List<CombatItemReference> _combatItemReferences = new List<CombatItemReference>();

        public readonly List<WeaponReference> WeaponReferences = new List<WeaponReference>();
        public readonly List<ArmourReference> ChestArmours = new List<ArmourReference>();
        public readonly List<ArmourReference> Helms = new List<ArmourReference>();
        public readonly List<ArmourReference> Rings = new List<ArmourReference>();
        public readonly List<ArmourReference> Amulets = new List<ArmourReference>();

        public readonly List<ArmourReference> AllArmour = new List<ArmourReference>();
        
        private enum CombatItemType { Armour, Weapon, Other }

        private static CombatItemType GetCombatItemTypeByEquipment(DataOvlReference.Equipment equipment)
        {
            bool equipmentMatches(Array theArray)
            {
                foreach (object theEnum in theArray) 
                {
                    if ((int)equipment == (int)theEnum) return true;
                }

                return false;
            }
            
            if (equipmentMatches(Enum.GetValues(typeof(WeaponReference.WeaponTypeEnum)))) return CombatItemType.Weapon;
            if (equipmentMatches(Enum.GetValues(typeof(ArmourReference.HelmEnum)))) return CombatItemType.Armour;
            if (equipmentMatches(Enum.GetValues(typeof(ArmourReference.ChestArmourEnum)))) return CombatItemType.Armour;
            if (equipmentMatches(Enum.GetValues(typeof(ArmourReference.RingEnum)))) return CombatItemType.Armour;
            if (equipmentMatches(Enum.GetValues(typeof(ArmourReference.AmuletEnum)))) return CombatItemType.Armour;

            throw new Ultima5ReduxException("Tried to create CombatItemReference from " + equipment);
        }

        
        public CombatItemReferences(DataOvlReference dataOvlReference, InventoryReferences inventoryReferences)
        {
            _dataOvlReference = dataOvlReference;
            
            List<InventoryReference> combatItems = 
                inventoryReferences.GetInventoryReferenceList(InventoryReferences.InventoryReferenceType.Armament);
            // foreach of the weapon references
            ///// NOTE! I need to actually sort by the armours into the right arrays!!!!!!!!!!!!!!!!!
            foreach (InventoryReference inventoryReference in combatItems)
            {
                DataOvlReference.Equipment equipment = inventoryReference.GetAsEquipment();
                switch (GetCombatItemTypeByEquipment(equipment))
                {
                    case CombatItemType.Armour:
                        AllArmour.Add(new ArmourReference(_dataOvlReference, inventoryReference));
                        break;
                    case CombatItemType.Weapon:
                        WeaponReferences.Add(new WeaponReference(_dataOvlReference, inventoryReference));
                        break;
                    case CombatItemType.Other:
                        throw new Ultima5ReduxException("Tried to create CombatItemReference from " + equipment);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                WeaponReference weaponReference = new WeaponReference(dataOvlReference, inventoryReference);
            }
        }
    }
}