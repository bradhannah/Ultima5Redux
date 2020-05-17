using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters
{
    public abstract class Armour : CombatItem
    {
        public Armour(DataOvlReference.Equipment specificEquipment, DataOvlReference dataOvlRef, List<byte> gameStateByteRef, int nOffset, int nSpriteNum) 
            : base (specificEquipment, dataOvlRef, gameStateByteRef, nOffset, nSpriteNum)
        {

        }

        //public Armour(int quantity, string longName, string shortName, int nSpriteNum, int attackStat, int defendStat, DataOvlReference.EQUIPMENT specificEquipment) : 
        //    base(quantity, longName, shortName, nSpriteNum, attackStat, defendStat, specificEquipment)
        //{
        //}
    }
}