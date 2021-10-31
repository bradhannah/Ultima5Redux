using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class ArmourReference : CombatItemReference
    {
        public enum AmuletEnum { AmuletTurning = 0x247, SpikeCollar = 0x248, Ankh = 0x249 }
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
        }
    }
}