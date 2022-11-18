using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    [DataContract] public sealed class StackableItem
    {
        [IgnoreDataMember] public InventoryItem InvItem { get; private set; }

        [DataMember] private readonly string _inventoryItemName;
        [DataMember] private readonly InventoryReferences.InventoryReferenceType _inventoryReferenceType;
        [DataMember] private readonly int _nQuantity;

        [OnDeserialized] private void PostDeserialize(StreamingContext streamingContext)
        {
            InventoryReference inventoryReference =
                GameReferences.Instance.InvRef.GetInventoryReference(_inventoryReferenceType, _inventoryItemName);
            InvItem = InventoryItemFactory.Create(inventoryReference);
            InvItem.Quantity = _nQuantity;
        }


        [JsonConstructor] public StackableItem()
        {
        }

        public StackableItem(InventoryItem item)
        {
            InvItem = item;
            _inventoryItemName = item.InvRef.ItemName;
            _inventoryReferenceType = item.InvRefType;
            _nQuantity = item.Quantity;
        }
    }
}