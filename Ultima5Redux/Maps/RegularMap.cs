using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public abstract class RegularMap : Map
    {
        [DataMember] public int MapFloor { get; private set; }

        [DataMember] public SmallMapReferences.SingleMapReference.Location MapLocation { get; private set; }

        [IgnoreDataMember] public abstract SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; }

        [IgnoreDataMember]
        protected sealed override Dictionary<Point2D, TileOverrideReference> XYOverrides =>
            _xyOverrides ??= GameReferences.TileOverrideRefs.GetTileXYOverrides(CurrentSingleMapReference);

        private Dictionary<Point2D, TileOverrideReference> _xyOverrides;

        [JsonConstructor] protected RegularMap()
        {
        }

        protected RegularMap(SmallMapReferences.SingleMapReference.Location location, int nFloor)
        {
            MapLocation = location;
            MapFloor = nFloor;
        }
    }
}