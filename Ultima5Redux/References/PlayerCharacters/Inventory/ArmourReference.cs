using System;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    public class ArmourReference : CombatItemReference
    {
        public enum ArmourType { Amulet, ChestArmour, Helm, Ring }

        public enum SpecificAmulet { AmuletOfTurning = 0x247, SpikedCollar = 0x248, Ankh = 0x249 }

        public enum SpecificChestArmour
        {
            ClothArmour = 0x223, LeatherArmour = 0x224, Ringmail = 0x225, ScaleMail = 0x226, ChainMail = 0x227,
            PlateMail = 0x228, MysticArmour = 0x229
        }

        public enum SpecificHelm { LeatherHelm = 0x21A, ChainCoif = 0x21B, IronHelm = 0x21C, SpikedHelm = 0x21D }

        public enum SpecificRing { RingInvisibility = 0x244, RingProtection = 0x245, RingRegeneration = 0x246 }

        public ArmourType TheArmourType { get; }

        public ArmourReference(DataOvlReference dataOvlReference, InventoryReference inventoryReference) : base(
            dataOvlReference, inventoryReference) =>
            TheArmourType = GetArmourTypeByEquipment(inventoryReference.GetAsEquipment());

        internal static ArmourType GetArmourTypeByEquipment(DataOvlReference.Equipment equipment)
        {
            if (CombatItemReferences.EquipmentMatches(Enum.GetValues(typeof(SpecificHelm)), ref equipment))
                return ArmourType.Helm;
            if (CombatItemReferences.EquipmentMatches(Enum.GetValues(typeof(SpecificChestArmour)), ref equipment))
                return ArmourType.ChestArmour;
            if (CombatItemReferences.EquipmentMatches(Enum.GetValues(typeof(SpecificRing)), ref equipment))
                return ArmourType.Ring;
            if (CombatItemReferences.EquipmentMatches(Enum.GetValues(typeof(SpecificAmulet)), ref equipment))
                return ArmourType.Amulet;

            throw new Ultima5ReduxException("Tried to create CombatItemReference from " + equipment);
        }
    }
}