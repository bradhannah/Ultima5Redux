using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Provision : InventoryItem
    {
        public enum ProvisionSpritesTypeEnum { Torches = 269, Gems = 264, Keys = 263, SkullKeys = 263 }

        public enum ProvisionTypeEnum { Torches = 0x208, Gems = 0x207, Keys = 0x206, SkullKeys = 0x20B }

        private readonly DataOvlReference _dataOvlReference;
        private readonly GameState _state;

        /// <summary>
        ///     Creates a provision
        /// </summary>
        /// <param name="provisionTypeEnum">what kind of provision</param>
        /// <param name="longName"></param>
        /// <param name="shortName"></param>
        /// <param name="findDescription"></param>
        /// <param name="spriteNum"></param>
        /// <param name="dataOvlRef"></param>
        /// <param name="state"></param>
        public Provision(ProvisionTypeEnum provisionTypeEnum, string longName, string shortName,
            string findDescription, int spriteNum, DataOvlReference dataOvlRef, GameState state)
            : base(0, longName, shortName, findDescription, spriteNum)
        {
            ProvisionType = provisionTypeEnum;
            _dataOvlReference = dataOvlRef;
            _state = state;
        }

        public ProvisionTypeEnum ProvisionType { get; }

        public override bool HideQuantity => false;

        public override string InventoryReferenceString => ProvisionType.ToString();

        /// <summary>
        ///     Gets the parties current quantity of the specific provision
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override int Quantity
        {
            get
            {
                switch (ProvisionType)
                {
                    case ProvisionTypeEnum.Torches:
                        return _state.Torches;
                    case ProvisionTypeEnum.Gems:
                        return _state.Gems;
                    case ProvisionTypeEnum.Keys:
                        return _state.Keys;
                    case ProvisionTypeEnum.SkullKeys:
                        return _state.SkullKeys;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (ProvisionType)
                {
                    case ProvisionTypeEnum.Torches:
                        _state.Torches = value;
                        break;
                    case ProvisionTypeEnum.Gems:
                        _state.Gems = value;
                        break;
                    case ProvisionTypeEnum.Keys:
                        _state.Keys = value;
                        break;
                    case ProvisionTypeEnum.SkullKeys:
                        _state.SkullKeys = value;
                        break;
                    //throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        ///     Gets the cost of the provision based on the avatar's intelligence rating
        /// </summary>
        /// <param name="records"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public override int GetAdjustedBuyPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location)
        {
            int nBasePrice = GetBasePrice(location);
            if (nBasePrice == -1)
                throw new Ultima5ReduxException("Requested provision " + LongName + " from " + location +
                                                " which is not sold here");

            // The 'base price' is what a character with a theoretical Intelligence of 0 would get. Every additional Intelligence point
            // deducts 1.5% from the item's cost. The scale is reversed for selling, where every point increases gold received by 1.5%,
            // up to a theoretical 66.6 Intelligence points. Therefore, only the 'base prices' need be listed here.
            // Note that buying prices are rounded down, while selling prices are rounded up.
            // http://infinitron.nullneuron.net/u5eco.html
            int nAdjustedPrice = nBasePrice -
                                 _state.CharacterRecords.AvatarRecord.Stats.Intelligence * (int) (nBasePrice * 0.015f);
            //* (1 + (100 - (int)_state.CharacterRecords.AvatarRecord.Stats.Intelligence) / 100)); 
            return nAdjustedPrice;
        }

        /// <summary>
        ///     Gets the bundle quantity for the current provision
        /// </summary>
        /// <returns></returns>
        public int GetBundleQuantity()
        {
            return ProvisionCostsAndQuantities.BundleQuantity[ProvisionType];
        }

        /// <summary>
        ///     Gets the base price which is later adjusted based on intelligence
        /// </summary>
        /// <param name="location"></param>
        /// <returns>Greater than zero if it is sold, otherwise returns -1</returns>
        private int GetBasePrice(SmallMapReferences.SingleMapReference.Location location)
        {
            int nIndex = 0;
            foreach (SmallMapReferences.SingleMapReference.Location potentialLocation in _dataOvlReference
                .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_TOWNES_PROVISIONS).GetAsByteList())
            {
                if (potentialLocation == location)
                    // they sell it, now we find it
                    return ProvisionCostsAndQuantities.Prices[nIndex,
                        ProvisionCostsAndQuantities.ProvisionOrder[ProvisionType]];
                nIndex++;
            }

            return -1;
        }

        private static class ProvisionCostsAndQuantities
        {
            /// <summary>
            ///     the prices of provisions because I can't find it in the code!
            /// </summary>
            public static readonly int[,] Prices =
            {
                {
                    320, 400, 22
                },
                {
                    370, 450, 50
                },
                {
                    380, 510, 24
                }
            };

            public static readonly Dictionary<ProvisionTypeEnum, int> BundleQuantity =
                new Dictionary<ProvisionTypeEnum, int>
                {
                    {ProvisionTypeEnum.Torches, 5}, {ProvisionTypeEnum.Keys, 3}, {ProvisionTypeEnum.Gems, 4}
                };

            // the order of the provisions in the _prices array
            public static readonly Dictionary<ProvisionTypeEnum, int> ProvisionOrder =
                new Dictionary<ProvisionTypeEnum, int>
                {
                    {ProvisionTypeEnum.Keys, 0}, {ProvisionTypeEnum.Gems, 1}, {ProvisionTypeEnum.Torches, 2}
                };
        }
    }
}