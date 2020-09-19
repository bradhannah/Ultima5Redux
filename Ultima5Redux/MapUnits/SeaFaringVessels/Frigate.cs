using System.Collections.Generic;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    public class Frigate : SeaFaringVessel
    {

        /// <summary>
        /// How many skiffs does the frigate have aboard?
        /// </summary>
        public int SkiffsAboard
        {
            get => this.TheMapUnitState.Depends3;
            set => this.TheMapUnitState.Depends3 = (byte)value;
        }
        
        // public static Frigate CreateFrigateAtDock(SmallMapReferences.SingleMapReference.Location location, 
        //     MapUnitState existingState)
        // {
        //     Frigate frigate = new Frigate(existingState, );
        // }

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
            TileReferences tileReferences, SmallMapReferences.SingleMapReference.Location location) : 
            base(mapUnitState, null,
            mapUnitMovement, tileReferences, location)
        {
        }
    }
}