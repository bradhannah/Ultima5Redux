using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [DataContract]
    public abstract class InventoryItem
    {
        public const int MAX_INVENTORY_ITEM_QUANTITY = 99;

        [DataMember] public virtual int Quantity { get; set; }

        [DataMember] public InventoryReferences.InventoryReferenceType InvRefType { get; set; }

        [DataMember] public int SpriteNum { get; private set; }
        [IgnoreDataMember] public abstract string FindDescription { get; }

        [IgnoreDataMember] public abstract bool HideQuantity { get; }

        [IgnoreDataMember] public abstract string InventoryReferenceString { get; }

        [IgnoreDataMember] public virtual int BasePrice => 0;

        [IgnoreDataMember] public virtual bool IsSellable => BasePrice > 0;

        [IgnoreDataMember] public virtual string LongName => InvRef.FriendlyItemName;

        [IgnoreDataMember] public virtual string ShortName => InvRef.ItemName;

        [IgnoreDataMember]
        public string QuantityString
        {
            get
            {
                if (HideQuantity) return string.Empty;
                return Quantity == 0 ? "--" : Quantity.ToString();
            }
        }

        [IgnoreDataMember]
        public InventoryReference InvRef
        {
            get => GameReferences.InvRef.GetInventoryReference(InvRefType, InventoryReferenceString);
            protected internal set =>
                InvRefType = value.InvRefType;
        }

        [JsonConstructor] protected InventoryItem()
        {
        }

        protected InventoryItem(int quantity, int spriteNum, InventoryReferences.InventoryReferenceType invRefType)
        {
            Quantity = quantity;
            SpriteNum = spriteNum;
            InvRefType = invRefType;
        }

        public virtual int GetAdjustedBuyPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location) => 0;

        public virtual int GetAdjustedSellPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location)
        {
            return 0;
        }

        public virtual int GetQuantityForSale(SmallMapReferences.SingleMapReference.Location location)
        {
            return 1;
        }
    }
}