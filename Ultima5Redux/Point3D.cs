using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public class Point2D
    {
        public int X { get; set; }
        public int Y { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is Point2D))
                return false;

            return Equals((Point2D)obj);
        }

        public bool Equals(Point2D other)
        {
            if (X != other.X)
                return false;

            return Y == other.Y;
        }

        public override int GetHashCode()
        {
            var hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Point2D point1, Point2D point2)
        {
            return point1.Equals(point2);
        }

        public static bool operator !=(Point2D point1, Point2D point2)
        {
            return !point1.Equals(point2);
        }


        public Point2D(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
    public class Point3D
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Point3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
