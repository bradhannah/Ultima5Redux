using System.Collections.Generic;

namespace Ultima5Redux.Maps
{
    public abstract class RegularMap : Map
    {
        protected RegularMap(TileOverrides tileOverrides, SmallMapReferences.SingleMapReference singleSmallMapReference,
            TileReferences spriteTileReferences) : base(tileOverrides, spriteTileReferences)
        {
            CurrentSingleMapReference = singleSmallMapReference;

            // for now combat maps don't have overrides
            //if (singleSmallMapReference != null) 
            XYOverrides = tileOverrides.GetTileXYOverrides(singleSmallMapReference);
        }

        protected sealed override Dictionary<Point2D, TileOverride> XYOverrides { get; set; }

        public SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; }
    }
}