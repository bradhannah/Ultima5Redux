﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.Maps
{
    public abstract class RegularMap : Map
    {
        [IgnoreDataMember]
        protected sealed override Dictionary<Point2D, TileOverrideReference> XYOverrides { get; set; }

        [IgnoreDataMember] public abstract SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; }

        [DataMember] public int MapFloor { get; private set; }

        [DataMember] public SmallMapReferences.SingleMapReference.Location MapLocation { get; private set; } 

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