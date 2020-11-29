using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public abstract class Armour : CombatItem
    {
        protected Armour(DataOvlReference.Equipment specificEquipment, DataOvlReference dataOvlRef,
            int nQuantity, int nOffset, int nSpriteNum)
            : base(specificEquipment, dataOvlRef, nQuantity, nOffset, nSpriteNum)
        {
        }
    }
}