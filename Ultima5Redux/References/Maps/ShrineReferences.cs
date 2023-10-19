using System.Collections.Generic;

namespace Ultima5Redux.References.Maps
{
    public class ShrineReferences
    {
        private readonly Dictionary<VirtueReference.VirtueType, ShrineReference> _shrineReferences = new();
        private readonly Dictionary<Point2D, ShrineReference> _shrineReferencesByPosition = new();

        public ShrineReference GetShrineReferenceByVirtue(VirtueReference.VirtueType virtue) =>
            _shrineReferences[virtue];

        public ShrineReference GetShrineReferenceByPosition(Point2D position) =>
            _shrineReferencesByPosition.ContainsKey(position) ? _shrineReferencesByPosition[position] : null;

        public IEnumerable<ShrineReference> Shrines => _shrineReferences.Values;

        public ShrineReferences(DataOvlReference dataOvlRef) {
            foreach (VirtueReference virtue in GameReferences.Instance.VirtueReferences.Virtues) {
                var shrineReference = new ShrineReference(virtue,
                    new Point2D(
                        dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.SHRINE_X_COORDS).GetAsByteList()[
                            (int)virtue.Virtue],
                        dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.SHRINE_Y_COORDS).GetAsByteList()[
                            (int)virtue.Virtue]
                    )
                );
                _shrineReferences.Add(virtue.Virtue, shrineReference);
                _shrineReferencesByPosition.Add(shrineReference.Position, shrineReference);
            }
        }
    }

    public class ShrineReference
    {
    

        // SHRINE_X_COORDS
        // SHRINE_Y_COORDS
        public Point2D Position { get; }
        public VirtueReference Virtue { get; private set; }

        internal ShrineReference(VirtueReference virtue, Point2D position) {
            Virtue = virtue;
            Position = position;
        }
    }
}