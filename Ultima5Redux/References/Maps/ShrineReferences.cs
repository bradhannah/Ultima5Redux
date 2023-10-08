using System;
using System.Collections.Generic;

namespace Ultima5Redux.References.Maps
{
    public class ShrineReferences
    {
        private readonly Dictionary<ShrineReference.Virtue, ShrineReference> _shrineReferences = new();
        private readonly Dictionary<Point2D, ShrineReference> _shrineReferencesByPosition = new();

        public ShrineReference GetShrineReferenceByVirtue(ShrineReference.Virtue virtue) => _shrineReferences[virtue];

        public ShrineReference GetShrineReferenceByPosition(Point2D position) =>
            _shrineReferencesByPosition.ContainsKey(position) ? _shrineReferencesByPosition[position] : null;

        public IEnumerable<ShrineReference> Shrines => _shrineReferences.Values;

        public ShrineReferences(DataOvlReference dataOvlRef) {
            foreach (ShrineReference.Virtue virtue in Enum.GetValues(typeof(ShrineReference.Virtue))) {
                var shrineReference = new ShrineReference(virtue,
                    new Point2D(
                        dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.SHRINE_X_COORDS).GetAsByteList()[
                            (int)virtue],
                        dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.SHRINE_Y_COORDS).GetAsByteList()[
                            (int)virtue]
                    )
                );
                _shrineReferences.Add(virtue, shrineReference);
                _shrineReferencesByPosition.Add(shrineReference.Position, shrineReference);
            }
        }
    }

    public class ShrineReference
    {
        public enum Virtue
        {
            Honesty = 0, Compassion = 1, Valor = 2, Justice = 3, Sacrifice = 4, Honor = 5, Spirituality = 6,
            Humility = 7
        }

        // SHRINE_X_COORDS
        // SHRINE_Y_COORDS
        public Point2D Position { get; }
        public Virtue TheVirtue { get; private set; }

        internal ShrineReference(Virtue virtue, Point2D position) {
            TheVirtue = virtue;
            Position = position;
        }
    }
}