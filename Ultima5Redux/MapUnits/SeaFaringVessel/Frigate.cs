using System.Collections.Generic;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.SeaFaringVessel
{
    public class Frigate : SeaFaringVessel
    {
        private static Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; } = new Dictionary<SmallMapReferences.SingleMapReference.Location, int>()
        {
            {SmallMapReferences.SingleMapReference.Location.East_Britanny, 1300},
            {SmallMapReferences.SingleMapReference.Location.Minoc, 1500},
            {SmallMapReferences.SingleMapReference.Location.Buccaneers_Den, 1400},
            {SmallMapReferences.SingleMapReference.Location.Jhelom, 1200}
        };

        public static int GetPrice(SmallMapReferences.SingleMapReference.Location location,
            PlayerCharacterRecords records)
        {
            return GetAdjustedPrice(records, Prices[location]);
        }

        
        public Frigate(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, 
            bool bLoadedFromDisk) : base(mapUnitState, null,
            mapUnitMovement, bLoadedFromDisk)
        {
        }
    }
}