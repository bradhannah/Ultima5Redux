using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public abstract class InventoryItems<TEnumType, T>
    {
        [DataMember] public abstract Dictionary<TEnumType, T> Items { get; internal set; }

        [IgnoreDataMember] public virtual IEnumerable<InventoryItem> GenericItemList =>
            Items.Values.Cast<InventoryItem>().ToList();

        [IgnoreDataMember] protected readonly List<byte> GameStateByteArray;

        [JsonConstructor] protected InventoryItems()
        {
        }

        protected InventoryItems(List<byte> gameStateByteArray)
        {
            GameStateByteArray = gameStateByteArray;
        }
    }
}