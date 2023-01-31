using System;
using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    public class ReagentReferences
    {
        private readonly
            Dictionary<SmallMapReferences.SingleMapReference.Location,
                Dictionary<Reagent.SpecificReagentType, ReagentPriceAndQuantity>> _reagentPriceAndQuantities = new();

        public ReagentReferences()
        {
            List<byte> prices = GameReferences.Instance.DataOvlRef
                .GetDataChunk(DataOvlReference.DataChunkName.REAGENT_BASE_PRICES).GetAsByteList();
            List<byte> quantities = GameReferences.Instance.DataOvlRef
                .GetDataChunk(DataOvlReference.DataChunkName.REAGENT_QUANTITES).GetAsByteList();

            int nReagents = Enum.GetNames(typeof(Reagent.SpecificReagentType)).Length;

            // Get the locations that reagents are sold at
            List<SmallMapReferences.SingleMapReference.Location> locations = GetLocations();
            // cycle through each location and add reagents to location<->reagent map
            for (int i = 0; i < locations.Count; i++)
            {
                foreach (Reagent.SpecificReagentType reagentType in Enum.GetValues(typeof(Reagent.SpecificReagentType)))
                {
                    int nOffset = (int)reagentType - (int)Reagent.SpecificReagentType.SulfurAsh;

                    int nIndex = i * nReagents + nOffset;
                    SmallMapReferences.SingleMapReference.Location location = locations[i];
                    if (quantities[nIndex] <= 0) continue;

                    if (!_reagentPriceAndQuantities.ContainsKey(location))
                    {
                        _reagentPriceAndQuantities.Add(location,
                            new Dictionary<Reagent.SpecificReagentType, ReagentPriceAndQuantity>());
                    }

                    _reagentPriceAndQuantities[location].Add(reagentType,
                        new ReagentPriceAndQuantity(prices[nIndex], quantities[nIndex]));
                }
            }
        }

        /// <summary>
        ///     Get all locations that reagents are sold
        /// </summary>
        /// <returns></returns>
        private static List<SmallMapReferences.SingleMapReference.Location> GetLocations()
        {
            List<byte> reagentSkByteList = GameReferences.Instance.DataOvlRef
                .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_TOWNES_REAGENTS).GetAsByteList();

            return reagentSkByteList.Select(b => (SmallMapReferences.SingleMapReference.Location)b).ToList();
        }

        public ReagentPriceAndQuantity GetPriceAndQuantity(SmallMapReferences.SingleMapReference.Location location,
            Reagent.SpecificReagentType specificReagentType)
        {
            if (!_reagentPriceAndQuantities.ContainsKey(location))
                throw new Ultima5ReduxException("Tried to buy reagent at location that doesn't sell them: " + location);
            if (!_reagentPriceAndQuantities[location].ContainsKey(specificReagentType))
                throw new Ultima5ReduxException("Tried to buy " + specificReagentType + " in " + location +
                                                " but they don't sell it");

            return _reagentPriceAndQuantities[location][specificReagentType];
        }

        public bool IsReagentSoldAtLocation(SmallMapReferences.SingleMapReference.Location location,
            Reagent.SpecificReagentType specificReagentType) =>
            _reagentPriceAndQuantities.ContainsKey(location) &&
            _reagentPriceAndQuantities[location].ContainsKey(specificReagentType);
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