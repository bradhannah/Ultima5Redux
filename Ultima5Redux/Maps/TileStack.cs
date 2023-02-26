using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class TileStack : IEnumerable<TileReference>
    {
        private readonly Dictionary<int, TileReference> _tileReferencesDictionary;

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public TileReference MapUnitTileReference { get; private set; }

        public ReadOnlyDictionary<int, TileReference> TileReferencesDictionary => new(_tileReferencesDictionary);

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public Point2D Xy { get; }

        public TileStack(in Point2D xy)
        {
            Xy = xy;
            _tileReferencesDictionary = new Dictionary<int, TileReference>();
            MapUnitTileReference = null;
        }

        public void PushTileReference(TileReference tileReference, bool bMapUnit = false)
        {
            if (_tileReferencesDictionary.ContainsKey(tileReference.Index))
            {
                throw new Ultima5ReduxException(
                    $"Tried to push tile index: {tileReference.Index} onto {Xy.X}, {Xy.Y} but it already existed.");
            }

            _tileReferencesDictionary.Add(tileReference.Index, tileReference);

            // we keep track of the map unit in particular
            if (bMapUnit) MapUnitTileReference = tileReference;
        }

        public IEnumerator<TileReference> GetEnumerator() => _tileReferencesDictionary.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}