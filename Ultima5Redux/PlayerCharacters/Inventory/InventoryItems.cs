using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public abstract class InventoryItems <TEnumType, T>
    {
        public abstract Dictionary<TEnumType, T> Items { get; }

        public virtual IEnumerable<InventoryItem> GenericItemList => Items.Values.Cast<InventoryItem>().ToList();

        protected readonly DataOvlReference DataOvlRef;
        protected readonly List<byte> GameStateByteArray;

        protected InventoryItems(DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
        {
            this.DataOvlRef = dataOvlRef;
            this.GameStateByteArray = gameStateByteArray;
        }
    }
}
