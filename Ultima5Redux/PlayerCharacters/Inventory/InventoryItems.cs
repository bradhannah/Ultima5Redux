using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public abstract class InventoryItems<TEnumType, T>
    {
        protected readonly DataOvlReference DataOvlRef;
        protected readonly List<byte> GameStateByteArray;

        protected InventoryItems(DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
        {
            DataOvlRef = dataOvlRef;
            GameStateByteArray = gameStateByteArray;
        }

        public abstract Dictionary<TEnumType, T> Items { get; }

        public virtual IEnumerable<InventoryItem> GenericItemList => Items.Values.Cast<InventoryItem>().ToList();
    }
}