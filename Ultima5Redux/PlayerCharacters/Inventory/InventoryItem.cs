using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Ultima5Redux.Maps;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [DataContract]
    public abstract class InventoryItem
    {
        protected InventoryItem(int quantity, int spriteNum) : this(quantity, "", spriteNum)
        {
        }

        protected InventoryItem(int quantity, string findDescription, int spriteNum)
        {
            Quantity = quantity;
            SpriteNum = spriteNum;
            FindDescription = findDescription;
        }

        [DataMember] public virtual string FindDescription { get; }
        [DataMember] public virtual int Quantity { get; set; }
        [DataMember] public int SpriteNum { get; }

        [IgnoreDataMember] public InventoryReference InvRef { get; protected internal set; }

        [IgnoreDataMember] public abstract bool HideQuantity { get; }

        [IgnoreDataMember] public virtual bool IsSellable => BasePrice > 0;

        [IgnoreDataMember] public virtual int BasePrice => 0;

        [IgnoreDataMember] public abstract string InventoryReferenceString { get; }

        [IgnoreDataMember] public virtual string LongName => InvRef.FriendlyItemName;
        [IgnoreDataMember] public virtual string ShortName => InvRef.ItemName;

        [IgnoreDataMember] public string QuantityString
        {
            get
            {
                if (HideQuantity) return string.Empty;
                return Quantity == 0 ? "--" : Quantity.ToString();
            }
        }

        public virtual int GetAdjustedBuyPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location)
        {
            return 0;
        }

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