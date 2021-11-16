using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public abstract class CombatItems<TEnumType, T> : InventoryItems<TEnumType, T>
    {
        protected CombatItems(List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
        }

        [IgnoreDataMember] public List<CombatItem> AllCombatItems => GenericItemList.Cast<CombatItem>().ToList();
    }
}