using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class TileStack : IEnumerable<TileReference>
    {
        private readonly Dictionary<int, TileReference> _tileReferencesDictionary;
        public Point2D XY { get; private set; }

        public TileStack(in Point2D xy)
        {
            XY = xy;
            _tileReferencesDictionary = new();
            MapUnitTileReference = null;
        }

        public ReadOnlyDictionary<int, TileReference> TileReferencesDictionary => new(_tileReferencesDictionary);
        public TileReference MapUnitTileReference { get; private set; }

        public void PushTileReference(TileReference tileReference, bool bMapUnit = false)
        {
            _tileReferencesDictionary.Add(tileReference.Index, tileReference);
            
            // we keep track of the map unit in particular
            if (bMapUnit) MapUnitTileReference = tileReference;
        }

        public IEnumerator<TileReference> GetEnumerator()
        {
            return _tileReferencesDictionary.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}