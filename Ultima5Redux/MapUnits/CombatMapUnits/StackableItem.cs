using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class StackableItem
    {
        public InventoryItem InvItem { get; }

        public StackableItem(InventoryItem item)
        {
            InvItem = item;
        }
    }
}