using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    [DataContract]
    public class MapHolder
    {
        /// <summary>
        ///     Both underworld and overworld maps
        /// </summary>
        [DataMember(Name = "LargeMaps")] private readonly Dictionary<Map.Maps, LargeMap> _largeMaps = new(2)
        {
            { Map.Maps.Overworld, new LargeMap(LargeMapLocationReferences.LargeMapType.Overworld) },
            { Map.Maps.Underworld, new LargeMap(LargeMapLocationReferences.LargeMapType.Underworld) }
        };

        /// <summary>
        ///     All the small maps
        /// </summary>
        [DataMember(Name = "SmallMaps")] private readonly SmallMaps _smallMaps = new();

        [DataMember] public DungeonMap TheDungeonMap { get; internal set; }

        [IgnoreDataMember] public CombatMap TheCombatMap { get; internal set; }
        public LargeMap OverworldMap => _largeMaps[Map.Maps.Overworld];
        public SmallMaps SmallMaps => _smallMaps;
        public LargeMap UnderworldMap => _largeMaps[Map.Maps.Underworld];

        [OnDeserialized]
        private void PostDeserialize(StreamingContext context)
        {
            foreach (KeyValuePair<SmallMapReferences.SingleMapReference.Location, Dictionary<int, SmallMap>> kvp in
                     _smallMaps.MapLocationDictionary)
            {
                foreach (KeyValuePair<int, SmallMap> map in kvp.Value)
                {
                    // some maps such as dungeons don't have small map objects
                    map.Value?.SetSmallMaps(_smallMaps);
                }
            }
        }

        public LargeMap GetLargeMapByLargeMapType(LargeMapLocationReferences.LargeMapType largeMapType) =>
            largeMapType == LargeMapLocationReferences.LargeMapType.Overworld ? OverworldMap : UnderworldMap;

        public SmallMap GetSmallMap(SmallMapReferences.SingleMapReference singleMapReference) =>
            _smallMaps.GetSmallMap(singleMapReference.MapLocation, singleMapReference.Floor);
    }
}