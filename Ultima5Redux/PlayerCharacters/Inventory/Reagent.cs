using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    /// <summary>
    ///     Instance represents a single reagent type
    /// </summary>
    public class Reagent : InventoryItem
    {
        //0x2AA 1 0-99 Sulfur Ash
        //0x2AB 1 0-99 Ginseng
        //0x2AC 1 0-99 Garlic
        //0x2AD 1 0-99 Spider Silk
        //0x2AE 1 0-99 Blood Moss
        //0x2AF 1 0-99 Black Pearl
        //0x2B0 1 0-99 Nightshade
        //0x2B1 1 0-99 Mandrake Root
        public enum ReagentTypeEnum
        {
            SulfurAsh = 0x2AA, Ginseng = 0x2AB, Garlic = 0x2AC, SpiderSilk = 0x2AD, BloodMoss = 0x2AE,
            BlackPearl = 0x2AF, NightShade = 0x2B0, MandrakeRoot = 0x2B1
        }

        private const int REAGENT_SPRITE = 259;

        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>
            _reagentPriceAndQuantities;

        private readonly GameState _state;

        /// <summary>
        ///     Create a reagent
        /// </summary>
        /// <param name="reagentType">The type of reagent</param>
        /// <param name="quantity">how many the party has</param>
        /// <param name="longName">long verbose name</param>
        /// <param name="shortName">shortened version of the name</param>
        /// <param name="dataOvlRef"></param>
        /// <param name="state"></param>
        public Reagent(ReagentTypeEnum reagentType, int quantity, string longName, string shortName,
            DataOvlReference dataOvlRef, GameState state) : base(quantity, longName, shortName, REAGENT_SPRITE)
        {
            // capture the game state so we know the users Karma for cost calculations
            _state = state;
            //List<SmallMapReferences.SingleMapReference.Location> reagentShoppeKeeperLocations1 = reagentShoppeKeeperLocations;
            ReagentType = reagentType;
            _reagentPriceAndQuantities =
                new Dictionary<SmallMapReferences.SingleMapReference.Location, ReagentPriceAndQuantity>();

            List<byte> prices = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.REAGENT_BASE_PRICES)
                .GetAsByteList();
            List<byte> quantities = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.REAGENT_QUANTITES)
                .GetAsByteList();
            int nOffset = (int)ReagentType - (int)ReagentTypeEnum.SulfurAsh;
            int nReagents = Enum.GetNames(typeof(ReagentTypeEnum)).Length;

            // Get the locations that reagents are sold at
            List<SmallMapReferences.SingleMapReference.Location> locations = GetLocations(dataOvlRef);
            // cycle through each location and add reagents to location<->reagent map
            for (int i = 0; i < locations.Count; i++)
            {
                int nIndex = i * nReagents + nOffset;
                SmallMapReferences.SingleMapReference.Location location = locations[i];
                if (quantities[nIndex] > 0)
                    _reagentPriceAndQuantities.Add(location,
                        new ReagentPriceAndQuantity(prices[nIndex], quantities[nIndex]));
            }
        }


        public override bool HideQuantity => false;
        public override bool IsSellable => false;
        public override int BasePrice => 0;
        public ReagentTypeEnum ReagentType { get; }

        public override int Quantity
        {
            get => base.Quantity;
            set => base.Quantity = value > 99 ? 99 : value;
        }

        /// <summary>
        ///     Standard index/order of reagents in data files
        /// </summary>
        public int ReagentIndex => (int)ReagentType - (int)ReagentTypeEnum.SulfurAsh;

        public override string InventoryReferenceString => ReagentType.ToString();


        /// <summary>
        ///     Get all locations that reagents are sold
        /// </summary>
        /// <param name="dataOvlReference"></param>
        /// <returns></returns>
        private List<SmallMapReferences.SingleMapReference.Location> GetLocations(DataOvlReference dataOvlReference)
        {
            List<SmallMapReferences.SingleMapReference.Location> locations =
                new List<SmallMapReferences.SingleMapReference.Location>();

            foreach (SmallMapReferences.SingleMapReference.Location location in
                dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_TOWNES_REAGENTS)
                    .GetAsByteList())
            {
                locations.Add(location);
            }

            return locations;
        }

        /// <summary>
        ///     Get the correct price adjust for the specific location and
        /// </summary>
        /// <param name="records"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public override int GetAdjustedBuyPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location)
        {
            if (!_reagentPriceAndQuantities.ContainsKey(location))
                throw new Ultima5ReduxException("Requested reagent " + LongName + " from " + location +
                                                " which is not sold here");

            // A big thank you to Markus Brenner (@minstrel_dragon) for digging in and figuring out the Karma calculation
            // price = Base Price * (1 + (100 - Karma) / 100)
            int nAdjustedPrice = _reagentPriceAndQuantities[location].Price * (1 + (100 - _state.Karma) / 100);
            return nAdjustedPrice;
        }

        /// <summary>
        ///     Get bundle quantity based on location
        ///     Different merchants sell in different quantities
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public override int GetQuantityForSale(SmallMapReferences.SingleMapReference.Location location)
        {
            if (!_reagentPriceAndQuantities.ContainsKey(location))
                throw new Ultima5ReduxException("Requested reagent " + LongName + " from " + location +
                                                " which is not sold here");

            return _reagentPriceAndQuantities[location].Quantity;
        }

        /// <summary>
        ///     Does a particular location sell a particular reagent?
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public bool IsReagentForSale(SmallMapReferences.SingleMapReference.Location location)
        {
            return _reagentPriceAndQuantities.ContainsKey(location);
        }

        private class ReagentPriceAndQuantity
        {
            public ReagentPriceAndQuantity(int price, int quantity)
            {
                Price = price;
                Quantity = quantity;
            }

            public int Price { get; }
            public int Quantity { get; }
        }
    }
}