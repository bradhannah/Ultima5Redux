using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    /// <summary>
    ///     Contains details and information on the services provided and the costs thereof
    /// </summary>
    public class HealerServices
    {
        private readonly DataOvlReference _dataOvlReference;

        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, Dictionary<Healer.RemedyTypes, int>>
            _priceDictionary =
                new Dictionary<SmallMapReferences.SingleMapReference.Location, Dictionary<Healer.RemedyTypes, int>>();

        public HealerServices(DataOvlReference dataOvlReference)
        {
            _dataOvlReference = dataOvlReference;
            BuildServicesList();
        }

        public int GetServicePrice(SmallMapReferences.SingleMapReference.Location location, Healer.RemedyTypes remedy)
        {
            return _priceDictionary[location][remedy];
        }

        private void BuildServicesList()
        {
            IEnumerable<SmallMapReferences.SingleMapReference.Location> locations = ShoppeKeeper.GetLocations(
                _dataOvlReference,
                DataOvlReference.DataChunkName.SHOPPE_KEEPER_TOWNES_HEALING);
            List<byte> healPrices = _dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.HEALER_HEAL_PRICES)
                .GetAsByteList();
            List<byte> curePrices = _dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.HEALER_CURE_PRICES)
                .GetAsByteList();
            List<ushort> resurrectPrices = _dataOvlReference
                .GetDataChunk(DataOvlReference.DataChunkName.HEALER_RESURRECT_PRICES).GetChunkAsUint16List();

            int nIndex = 0;
            foreach (SmallMapReferences.SingleMapReference.Location location in locations)
            {
                Dictionary<Healer.RemedyTypes, int> pricesAtLocation = new Dictionary<Healer.RemedyTypes, int>
                {
                    { Healer.RemedyTypes.Heal, healPrices[nIndex] },
                    { Healer.RemedyTypes.Cure, curePrices[nIndex] },
                    { Healer.RemedyTypes.Resurrect, resurrectPrices[nIndex] }
                };

                _priceDictionary.Add(location, pricesAtLocation);

                nIndex++;
            }
        }
    }
}