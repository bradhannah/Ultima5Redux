using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters
{
    public class Shield : Armour
    {
        public enum ShieldTypeEnum { SmallShield = 0x21E, LargeShield = 0x21F,  SpikedShield = 0x220, MagicShield = 0x221, JewelShield = 0x222 }
        
        private const int SHIELD_SPRITE = 262;
 
        public Shield(ShieldTypeEnum shieldType, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
            : base(equipment, dataOvlRef, gameStateByteArray, (int)shieldType, SHIELD_SPRITE)
        {
            ShieldType = shieldType;
        }

        public ShieldTypeEnum ShieldType { get; }


        public override bool HideQuantity => false;
    }
}