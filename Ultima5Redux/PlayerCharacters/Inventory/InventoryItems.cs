using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public abstract class InventoryItems<TEnumType, T>
    {
        [IgnoreDataMember] protected readonly List<byte> GameStateByteArray;

        protected InventoryItems(List<byte> gameStateByteArray)
        {
            GameStateByteArray = gameStateByteArray;
        }

        [DataMember] public abstract Dictionary<TEnumType, T> Items { get; }

        [IgnoreDataMember] public virtual IEnumerable<InventoryItem> GenericItemList => Items.Values.Cast<InventoryItem>().ToList();
    }
}