using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.Maps;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public abstract class InventoryItem
    {
        protected InventoryItem(int quantity, string longName, string shortName, int spriteNum) : this(quantity,
            longName, shortName, "", spriteNum)
        {
        }

        protected InventoryItem(int quantity, string longName, string shortName, string findDescription, int spriteNum)
        {
            Quantity = quantity;
            LongName = longName;
            ShortName = shortName;
            SpriteNum = spriteNum;
            FindDescription = findDescription;
        }

        public abstract bool HideQuantity { get; }
        public virtual int Quantity { get; set; }

        public virtual string LongName { get; }

        public virtual string ShortName { get; }

        public virtual string FindDescription { get; }

        public InventoryReference InvRef { get; protected set; }

        public virtual bool IsSellable => BasePrice > 0;

        public virtual int BasePrice { get; protected set; } = 0;


        public int SpriteNum { get; }

        public string QuantityString
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