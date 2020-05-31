using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters
{
    public class Amulet : Armour
    {
        public AmuletEnum AmuletType;
        public enum AmuletEnum { AmuletTurning = 0x247, SpikeCollar = 0x248, Ankh = 0x249 }

        private const int AMULET_SPRITE = 268;
     
        public override bool HideQuantity => false;

        public Amulet(AmuletEnum amuletType, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
            : base(equipment, dataOvlRef, gameStateByteArray, (int)amuletType, AMULET_SPRITE)
        {
            AmuletType = amuletType;
        }
    }
}