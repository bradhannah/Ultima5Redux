using System.Collections.Generic;

namespace Ultima5Redux.Maps
{
    public abstract class RegularMap : Map
    {
        protected RegularMap(TileOverrideReferences tileOverrideReferences, SmallMapReferences.SingleMapReference singleSmallMapReference,
            TileReferences spriteTileReferences) : base(tileOverrideReferences, spriteTileReferences)
        {
            CurrentSingleMapReference = singleSmallMapReference;

            // for now combat maps don't have overrides
            //if (singleSmallMapReference != null) 
            XYOverrides = tileOverrideReferences.GetTileXYOverrides(singleSmallMapReference);
        }

        public SmallMapReferences.SingleMapReference CurrentSingleMapReference { get; }

        protected sealed override Dictionary<Point2D, TileOverrideReference> XYOverrides { get; set; }
    }
}