using System.Collections.Generic;
using Ultima5Redux.References;

namespace Ultima5Redux.Maps
{
    public abstract class RegularMap : Map
    {
        protected RegularMap(SmallMapReferences.SingleMapReference singleSmallMapReference) : base()
        {
            CurrentSingleMapReference = singleSmallMapReference;

            // for now combat maps don't have overrides
            //if (singleSmallMapReference != null) 
            XYOverrides = GameReferences.TileOverrideRefs.GetTileXYOverrides(singleSmallMapReference);
        }

        public SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; }

        protected sealed override Dictionary<Point2D, TileOverrideReference> XYOverrides { get; set; }
    }
}