using System;
using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.PlayerCharacters
{
    public class Provision : InventoryItem
    {
        private class ProvisionPriceAndQuantity
        {
            public int Price { get; }
            public int Quantity { get; }

            public ProvisionPriceAndQuantity(int price, int quantity)
            {
                Price = price;
                Quantity = quantity;
            }
        }
        
        public enum ProvisionTypeEnum { Torches = 0x208, Gems = 0x207, Keys = 0x206, SkullKeys = 0x20B }
        public enum ProvisionSpritesTypeEnum { Torches = 269, Gems = 264, Keys = 263, SkullKeys = 263 }

        private DataOvlReference _dataOvlReference;
        private GameState _state; 
        public ProvisionTypeEnum ProvisionType { get; private set; }
        
        public Provision(ProvisionTypeEnum provisionTypeEnum, string longName, string shortName, 
            string findDescription, int spriteNum, DataOvlReference dataOvlRef, GameState state) 
            : base(0, longName, shortName, findDescription, spriteNum)
        {
            ProvisionType = provisionTypeEnum;
            _dataOvlReference = dataOvlRef;
            _state = state;
        }

        public override int GetAdjustedBuyPrice(PlayerCharacterRecords records, SmallMapReferences.SingleMapReference.Location location)
        {
            int nBasePrice = GetBasePrice(location);
            if (nBasePrice == -1)
                 throw new Ultima5ReduxException("Requested provision "+ this.LongName + " from " + location + " which is not sold here");

            // A big thank you to Markus Brenner (@minstrel_dragon) for digging in and figuring out the Karma calculation
            // price = Base Price * (1 + (100 - Karma) / 100)
            int nAdjustedPrice = (nBasePrice * (1 + (100 - (int)_state.Karma) / 100)); 
            return nAdjustedPrice;
        }

        public override bool HideQuantity => false;

        public override int Quantity
        {
            get
            {
                switch (ProvisionType)
                {
                    case ProvisionTypeEnum.Torches:
                        return _state.GetDataChunk(GameState.DataChunkName.TORCHES_QUANTITY).GetChunkAsByte();
                    case ProvisionTypeEnum.Gems:
                        return _state.GetDataChunk(GameState.DataChunkName.GEMS_QUANTITY).GetChunkAsByte();
                    case ProvisionTypeEnum.Keys:
                        return _state.GetDataChunk(GameState.DataChunkName.KEYS_QUANTITY).GetChunkAsByte();
                    case ProvisionTypeEnum.SkullKeys:
                        return _state.GetDataChunk(GameState.DataChunkName.SKULL_KEYS_QUANTITY).GetChunkAsByte();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (ProvisionType)
                {
                    case ProvisionTypeEnum.Torches:
                        _state.GetDataChunk(GameState.DataChunkName.TORCHES_QUANTITY).SetChunkAsByte((byte)value);
                        break;
                    case ProvisionTypeEnum.Gems:
                        _state.GetDataChunk(GameState.DataChunkName.GEMS_QUANTITY).SetChunkAsByte((byte)value);
                        break;
                    case ProvisionTypeEnum.Keys:
                        _state.GetDataChunk(GameState.DataChunkName.KEYS_QUANTITY).SetChunkAsByte((byte)value);
                        break;
                    case ProvisionTypeEnum.SkullKeys:
                        _state.GetDataChunk(GameState.DataChunkName.SKULL_KEYS_QUANTITY).SetChunkAsByte((byte)value);
                        break;
                    default:
                        break;
                        //throw new ArgumentOutOfRangeException();
                }
                
            }
        }

        private int GetBasePrice(SmallMapReferences.SingleMapReference.Location location)
        {
            foreach (SmallMapReferences.SingleMapReference.Location potentialLocation in _dataOvlReference
                .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_TOWNES_PROVISIONS).GetAsByteList().Cast<SmallMapReferences.SingleMapReference.Location>())
            {
                if (potentialLocation == location)
                {
                    return 100;
                    // they sell it, now we find it
                }
            }

            return -1;
        }
    }
}