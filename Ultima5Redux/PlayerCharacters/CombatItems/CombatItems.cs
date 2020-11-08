using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public abstract class CombatItems<TEnumType, T> : InventoryItems<TEnumType, T>
    {
        public List<CombatItem> AllCombatItems => GenericItemList.Cast<CombatItem>().ToList();

        protected CombatItems(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
        }
    }
}