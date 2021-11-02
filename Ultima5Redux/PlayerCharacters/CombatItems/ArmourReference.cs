using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class ArmourReference : CombatItemReference
    {
        public enum AmuletEnum { AmuletOfTurning = 0x247, SpikedCollar = 0x248, Ankh = 0x249 }
        public enum ChestArmourEnum
        {
            ClothArmour = 0x223, LeatherArmour = 0x224, Ringmail = 0x225, ScaleMail = 0x226, ChainMail = 0x227,
            PlateMail = 0x228, MysticArmour = 0x229
        }
        public enum HelmEnum { LeatherHelm = 0x21A, ChainCoif = 0x21B, IronHelm = 0x21C, SpikedHelm = 0x21D }
        public enum RingEnum { RingInvisibility = 0x244, RingProtection = 0x245, RingRegeneration = 0x246 }

        public enum ArmourType { Amulet, ChestArmour, Helm, Ring}
        
        public ArmourType TheArmourType { get; }

        public ArmourReference(DataOvlReference dataOvlReference, InventoryReference inventoryReference) 
            : base(dataOvlReference, inventoryReference)
        {
            TheArmourType = GetArmourTypeByEquipment(inventoryReference.GetAsEquipment());
        }
        
        internal static ArmourType GetArmourTypeByEquipment(DataOvlReference.Equipment equipment)
        {
            if (CombatItemReferences.EquipmentMatches(Enum.GetValues(typeof(HelmEnum)), ref equipment)) return ArmourType.Helm;
            if (CombatItemReferences.EquipmentMatches(Enum.GetValues(typeof(ChestArmourEnum)), ref equipment)) return ArmourType.ChestArmour;
            if (CombatItemReferences.EquipmentMatches(Enum.GetValues(typeof(RingEnum)), ref equipment)) return ArmourType.Ring;
            if (CombatItemReferences.EquipmentMatches(Enum.GetValues(typeof(AmuletEnum)), ref equipment)) return ArmourType.Amulet;

            throw new Ultima5ReduxException("Tried to create CombatItemReference from " + equipment);
        }

    }
}