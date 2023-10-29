namespace Ultima5Redux.References.Maps
{
    public class ShrineReference
    {
        // SHRINE_X_COORDS
        // SHRINE_Y_COORDS
        public Point2D Position { get; }
        public VirtueReference VirtueRef { get; private set; }

        internal ShrineReference(VirtueReference virtueRef, Point2D position) {
            VirtueRef = virtueRef;
            Position = position;
        }
    }
}