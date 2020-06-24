using System;
using System.Collections.Generic;
using Ultima5Redux.Maps;

namespace Ultima5Redux.PlayerCharacters
{
    public class Reagent : InventoryItem
    {
        private class ReagentPriceAndQuantity
        {
            public int Price { get; }
            public int Quantity { get; }

            public ReagentPriceAndQuantity(int price, int quantity)
            {
                Price = price;
                Quantity = quantity;
            }
        }
        
        private const int REAGENT_SPRITE = 259;
        public Reagent(ReagentTypeEnum reagentType, int quantity, string longName, string shortName) : base(quantity, longName, shortName, REAGENT_SPRITE)
        {
            ReagentType = reagentType;

            switch (reagentType)
            {
                case ReagentTypeEnum.SulfurAsh:
                    _reagentPriceAndQuantities = new Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>()
                    {
                        {SmallMapReferences.SingleMapReference.Location.Yew, new ReagentPriceAndQuantity(24, 12)},
                        {SmallMapReferences.SingleMapReference.Location.Skara_Brae, new ReagentPriceAndQuantity(28, 14)},
                    };
                    break;
                case ReagentTypeEnum.Ginseng:
                    _reagentPriceAndQuantities = new Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>()
                    {
                        {SmallMapReferences.SingleMapReference.Location.Yew, new ReagentPriceAndQuantity(32, 8)},
                        {SmallMapReferences.SingleMapReference.Location.Skara_Brae, new ReagentPriceAndQuantity(32, 8)},
                        {SmallMapReferences.SingleMapReference.Location.Moonglow, new ReagentPriceAndQuantity(40, 10)},
                    };
                    break;
                case ReagentTypeEnum.Garlic:
                    _reagentPriceAndQuantities = new Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>()
                    {
                        {SmallMapReferences.SingleMapReference.Location.Yew, new ReagentPriceAndQuantity(32, 8)},
                        {SmallMapReferences.SingleMapReference.Location.Moonglow, new ReagentPriceAndQuantity(36, 6)},
                    };
                    break;
                case ReagentTypeEnum.SpiderSilk:
                    _reagentPriceAndQuantities = new Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>()
                    {
                        {SmallMapReferences.SingleMapReference.Location.Yew, new ReagentPriceAndQuantity(16, 2)},
                        {SmallMapReferences.SingleMapReference.Location.Cove, new ReagentPriceAndQuantity(12, 2)},
                        {SmallMapReferences.SingleMapReference.Location.Moonglow, new ReagentPriceAndQuantity(24, 4)},
                    };
                    break;
                case ReagentTypeEnum.BloodMoss:
                    _reagentPriceAndQuantities = new Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>()
                    {
                        {SmallMapReferences.SingleMapReference.Location.Yew, new ReagentPriceAndQuantity(40, 4)},
                        {SmallMapReferences.SingleMapReference.Location.Skara_Brae, new ReagentPriceAndQuantity(60, 6)},
                        {SmallMapReferences.SingleMapReference.Location.Cove, new ReagentPriceAndQuantity(16, 2)},
                        {SmallMapReferences.SingleMapReference.Location.Lycaeum, new ReagentPriceAndQuantity(100, 4)},
                    };
                    break;
                case ReagentTypeEnum.BlackPearl:
                    _reagentPriceAndQuantities = new Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>()
                    {
                        {SmallMapReferences.SingleMapReference.Location.Skara_Brae, new ReagentPriceAndQuantity(36, 6)},
                        {SmallMapReferences.SingleMapReference.Location.Cove, new ReagentPriceAndQuantity(16, 2)},
                    };
                    break;
                case ReagentTypeEnum.NightShade:
                    _reagentPriceAndQuantities = new Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>()
                    {
                        {SmallMapReferences.SingleMapReference.Location.Cove, new ReagentPriceAndQuantity(20, 1)},
                        {SmallMapReferences.SingleMapReference.Location.Moonglow, new ReagentPriceAndQuantity(24, 1)},
                        {SmallMapReferences.SingleMapReference.Location.Lycaeum, new ReagentPriceAndQuantity(60, 1)},
                    };
                    break;
                case ReagentTypeEnum.MandrakeRoot:
                    _reagentPriceAndQuantities = new Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>()
                    {
                        {SmallMapReferences.SingleMapReference.Location.Cove, new ReagentPriceAndQuantity(30, 1)},
                        {SmallMapReferences.SingleMapReference.Location.Moonglow, new ReagentPriceAndQuantity(25, 1)},
                        {SmallMapReferences.SingleMapReference.Location.Lycaeum, new ReagentPriceAndQuantity(80, 1)},
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reagentType), reagentType, null);
            }
        }

        public override bool HideQuantity => false;
        public override bool IsSellable => false;
        public override int BasePrice => 0;
        public ReagentTypeEnum ReagentType { get; }

        //0x2AA 1 0-99 Sulfur Ash
        //0x2AB 1 0-99 Ginseng
        //0x2AC 1 0-99 Garlic
        //0x2AD 1 0-99 Spider Silk
        //0x2AE 1 0-99 Blood Moss
        //0x2AF 1 0-99 Black Pearl
        //0x2B0 1 0-99 Nightshade
        //0x2B1 1 0-99 Mandrake Root
        public enum ReagentTypeEnum { SulfurAsh = 0x2AA , Ginseng = 0x2AB, Garlic = 0x2AC, SpiderSilk = 0x2AD, BloodMoss = 0x2AE, BlackPearl = 0x2AF, 
            NightShade = 0x2B0, MandrakeRoot = 0x2B1 };

        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>
            _reagentPriceAndQuantities;
        
        
        public override int GetAdjustedBuyPrice(PlayerCharacterRecords records, SmallMapReferences.SingleMapReference.Location location)
        {
            if (!_reagentPriceAndQuantities.ContainsKey(location))
                throw new Ultima5ReduxException("Requested reagent "+ this.LongName + " from " + location + " which is not sold here");

            return _reagentPriceAndQuantities[location].Price;
        }

        public override int GetQuantityForSale(SmallMapReferences.SingleMapReference.Location location)
        {
            if (!_reagentPriceAndQuantities.ContainsKey(location))
                throw new Ultima5ReduxException("Requested reagent "+ this.LongName + " from " + location + " which is not sold here");

            return _reagentPriceAndQuantities[location].Quantity;
        }
    }
}
