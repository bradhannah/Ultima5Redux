using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public class CharacterPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Floor { get; set; }

        public Point2D XY { get { return new Point2D(X, Y); } set { X = value.X; Y = value.Y; } }

        public override bool Equals(object obj)
        {
            return obj is CharacterPosition position &&
                   X == position.X &&
                   Y == position.Y &&
                   Floor == position.Floor;
        }

        public override int GetHashCode()
        {
            var hashCode = 1832819848;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Floor.GetHashCode();
            return hashCode;
        }
    }
}
