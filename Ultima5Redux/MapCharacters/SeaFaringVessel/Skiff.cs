using System.Collections.Generic;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapCharacters.SeaFaringVessel
{
    public class Skiff : SeaFaringVessel
    {
        private static Dictionary<SmallMapReferences.SingleMapReference.Location, int> Prices { get; } = new Dictionary<SmallMapReferences.SingleMapReference.Location, int>()
        {
            {SmallMapReferences.SingleMapReference.Location.East_Britanny, 250},
            {SmallMapReferences.SingleMapReference.Location.Minoc, 350},
            {SmallMapReferences.SingleMapReference.Location.Buccaneers_Den, 200},
            {SmallMapReferences.SingleMapReference.Location.Jhelom, 400}
        };
        
        public static int GetPrice(SmallMapReferences.SingleMapReference.Location location,
            PlayerCharacterRecords records)
        {
            return GetAdjustedPrice(records, Prices[location]);
        }

        public Skiff(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, 
            bool bLoadedFromDisk) : base(mapUnitState, null,
            mapUnitMovement, bLoadedFromDisk)
        {
        }
    }
}