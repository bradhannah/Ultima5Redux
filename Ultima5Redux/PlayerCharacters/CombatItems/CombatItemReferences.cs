using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class CombatItemReferences
    {
        public readonly List<WeaponReference> WeaponReferences = new List<WeaponReference>();
        public readonly List<ArmourReference> ChestArmours = new List<ArmourReference>();
        public readonly List<ArmourReference> Helms = new List<ArmourReference>();
        public readonly List<ArmourReference> Rings = new List<ArmourReference>();
        public readonly List<ArmourReference> Amulets = new List<ArmourReference>();

        public readonly List<ArmourReference> AllArmour = new List<ArmourReference>();
        
        private enum CombatItemType { Armour, Weapon, Other }

        private static CombatItemType GetCombatItemTypeByEquipment(DataOvlReference.Equipment equipment)
        {
            if (EquipmentMatches(Enum.GetValues(typeof(WeaponReference.WeaponTypeEnum)), ref equipment)) return CombatItemType.Weapon;
            if (EquipmentMatches(Enum.GetValues(typeof(ArmourReference.HelmEnum)), ref equipment)) return CombatItemType.Armour;
            if (EquipmentMatches(Enum.GetValues(typeof(ArmourReference.ChestArmourEnum)), ref equipment)) return CombatItemType.Armour;
            if (EquipmentMatches(Enum.GetValues(typeof(ArmourReference.RingEnum)), ref equipment)) return CombatItemType.Armour;
            if (EquipmentMatches(Enum.GetValues(typeof(ArmourReference.AmuletEnum)), ref equipment)) return CombatItemType.Armour;

            throw new Ultima5ReduxException("Tried to create CombatItemReference from " + equipment);
        }


        internal static bool EquipmentMatches(Array theArray, ref DataOvlReference.Equipment equipment)
        {
            foreach (object theEnum in theArray)
            {
                if (string.Equals(equipment.ToString(), theEnum.ToString(), StringComparison.CurrentCultureIgnoreCase)) return true;
            }

            return false;
        }


        public CombatItemReferences(InventoryReferences inventoryReferences)
        {
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
                        ArmourReference armourReference = new ArmourReference(GameReferences.DataOvlRef, inventoryReference);
                        
                        AllArmour.Add(armourReference);
                        switch (armourReference.TheArmourType)
                        {
                            case ArmourReference.ArmourType.Amulet:
                                Amulets.Add(armourReference);
                                break;
                            case ArmourReference.ArmourType.ChestArmour:
                                ChestArmours.Add(armourReference);
                                break;
                            case ArmourReference.ArmourType.Helm:
                                Helms.Add(armourReference);
                                break;
                            case ArmourReference.ArmourType.Ring:
                                Rings.Add(armourReference);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;
                    case CombatItemType.Weapon:
                        WeaponReferences.Add(new WeaponReference(GameReferences.DataOvlRef, inventoryReference));
                        break;
                    case CombatItemType.Other:
                        throw new Ultima5ReduxException("Tried to create CombatItemReference from " + equipment);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                //WeaponReference weaponReference = new WeaponReference(dataOvlReference, inventoryReference);
            }
        }
    }
}