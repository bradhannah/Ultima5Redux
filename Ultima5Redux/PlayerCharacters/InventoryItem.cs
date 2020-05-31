using System.Diagnostics.CodeAnalysis;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.PlayerCharacters
{
    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public abstract class InventoryItem
    {
        public abstract bool HideQuantity { get; }
        public virtual int Quantity { get; set; }

        public virtual string LongName { get; }

        public virtual string ShortName { get; }

        public virtual string FindDescription { get; }  
        
        public InventoryReference InvRef { get; protected set; }

        public virtual bool IsSellable => BasePrice > 0;

        public virtual int BasePrice { get; protected set; } = 0;

        public virtual int GetAdjustedBuyPrice(PlayerCharacterRecords records)
        {
            if (!IsSellable) return 0;

            // we add 3% of the value per dex point below 33, and subtract 3% for each point above 33
            const int nBaseDex = 33;
            int nAdjustedPrice = (int) (BasePrice + (BasePrice * 0.03f * (nBaseDex - (int)records.AvatarRecord.Stats.Dexterity)));
            return nAdjustedPrice <= 0 ? 1 : nAdjustedPrice;
        }

        public virtual int GetAdjustedSellPrice(PlayerCharacterRecords records)
        {
            if (!IsSellable) return 0;
            
            // we subtract 3% of the value for every dexterity point below 33, and add 3% for each point above it
            const int nBaseDex = 33;
            int nAdjustedPrice = (int)(BasePrice - (BasePrice * 0.03f * (nBaseDex - (int)records.AvatarRecord.Stats.Dexterity)));
            return nAdjustedPrice <= 0 ? 1 : nAdjustedPrice;
        }

        public int SpriteNum { get; }

        public string QuantityString
        {
            get
            {
                if (HideQuantity) return string.Empty;
                return Quantity == 0 ? "--" : Quantity.ToString();
            }
        }

        protected InventoryItem(int quantity, string longName, string shortName, int spriteNum) : this (quantity, longName, shortName, "", spriteNum)
        {
            
        }

        protected InventoryItem(int quantity, string longName, string shortName, string findDescription, int spriteNum)
        {
            this.Quantity = quantity;
            this.LongName = longName;
            this.ShortName = shortName;
            this.SpriteNum = spriteNum;
            this.FindDescription = findDescription;
        }
    }
}
