using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract]
    public abstract class CombatItems<TEnumType, T> : InventoryItems<TEnumType, T>
    {
        [IgnoreDataMember] public IEnumerable<CombatItem> AllCombatItems => GenericItemList.Cast<CombatItem>();

        [JsonConstructor]
        protected CombatItems()
        {
        }
    }
}