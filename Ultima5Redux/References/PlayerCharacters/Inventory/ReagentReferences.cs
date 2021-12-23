using System;
using System.Collections.Generic;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    public class ReagentReferences
    {
        private readonly
            Dictionary<SmallMapReferences.SingleMapReference.Location,
                Dictionary<Reagent.ReagentTypeEnum, ReagentPriceAndQuantity>> _reagentPriceAndQuantities = new();

        public ReagentReferences()
        {
            List<byte> prices = GameReferences.DataOvlRef
                .GetDataChunk(DataOvlReference.DataChunkName.REAGENT_BASE_PRICES).GetAsByteList();
            List<byte> quantities = GameReferences.DataOvlRef
                .GetDataChunk(DataOvlReference.DataChunkName.REAGENT_QUANTITES).GetAsByteList();

            int nReagents = Enum.GetNames(typeof(Reagent.ReagentTypeEnum)).Length;

            // Get the locations that reagents are sold at
            List<SmallMapReferences.SingleMapReference.Location> locations = GetLocations();
            // cycle through each location and add reagents to location<->reagent map
            for (int i = 0; i < locations.Count; i++)
            {
                foreach (Reagent.ReagentTypeEnum reagentType in Enum.GetValues(typeof(Reagent.ReagentTypeEnum)))
                {
                    int nOffset = (int)reagentType - (int)Reagent.ReagentTypeEnum.SulfurAsh;

                    int nIndex = i * nReagents + nOffset;
                    SmallMapReferences.SingleMapReference.Location location = locations[i];
                    if (quantities[nIndex] > 0)
                    {
                        if (!_reagentPriceAndQuantities.ContainsKey(location))
                        {
                            _reagentPriceAndQuantities.Add(location,
                                new Dictionary<Reagent.ReagentTypeEnum, ReagentPriceAndQuantity>());
                        }

                        _reagentPriceAndQuantities[location].Add(reagentType,
                            new ReagentPriceAndQuantity(prices[nIndex], quantities[nIndex]));
                    }
                }
            }
        }

        /// <summary>
        ///     Get all locations that reagents are sold
        /// </summary>
        /// <returns></returns>
        private static List<SmallMapReferences.SingleMapReference.Location> GetLocations()
        {
            List<SmallMapReferences.SingleMapReference.Location> locations = new();

            List<byte> reagentSkByteList = GameReferences.DataOvlRef
                .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_TOWNES_REAGENTS).GetAsByteList();
            foreach (byte b in reagentSkByteList)
            {
                SmallMapReferences.SingleMapReference.Location location =
                    (SmallMapReferences.SingleMapReference.Location)b;
                locations.Add(location);
            }

            return locations;
        }

        public ReagentPriceAndQuantity GetPriceAndQuantity(SmallMapReferences.SingleMapReference.Location location,
            Reagent.ReagentTypeEnum reagentType)
        {
            if (!_reagentPriceAndQuantities.ContainsKey(location))
                throw new Ultima5ReduxException("Tried to buy reagent at location that doesn't sell them: " + location);
            if (!_reagentPriceAndQuantities[location].ContainsKey(reagentType))
                throw new Ultima5ReduxException("Tried to buy " + reagentType + " in " + location +
                                                " but they don't sell it");

            return _reagentPriceAndQuantities[location][reagentType];
        }

        public bool IsReagentSoldAtLocation(SmallMapReferences.SingleMapReference.Location location,
            Reagent.ReagentTypeEnum reagentType)
        {
            if (!_reagentPriceAndQuantities.ContainsKey(location))
                return false;
            if (!_reagentPriceAndQuantities[location].ContainsKey(reagentType))
                return false;

            return true;
        }
    }

    public class ReagentPriceAndQuantity
    {
        public int Price { get; }
        public int Quantity { get; }

        public ReagentPriceAndQuantity(int price, int quantity)
        {
            Price = price;
            Quantity = quantity;
        }
    }
}