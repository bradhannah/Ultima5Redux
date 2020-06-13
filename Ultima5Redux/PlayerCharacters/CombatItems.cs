using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters
{
    public abstract class CombatItems<TEnumType, T> : InventoryItems<TEnumType, T>
    {
        public List<CombatItem> AllCombatItems
        {
            get
            {
                List<CombatItem> itemList = new List<CombatItem>(); 
                foreach (InventoryItem item in GenericItemList)
                {
                    itemList.Add((CombatItem)item);
                }

                return itemList;
                //return (List<CombatItem>) GenericItemList.Cast<CombatItem>();

            }
        } 
        
        protected CombatItems(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
        }
    }
}