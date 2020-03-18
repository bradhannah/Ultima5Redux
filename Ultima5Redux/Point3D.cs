using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public class Point3D
    {
        protected bool Equals(Point3D other)
        {
            throw new System.NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            Point3D p3d = (Point3D) obj;
            return Equals(p3d.X == X && p3d.Y == Y && p3d.Z == Z);
        }

        public override int GetHashCode()
        {
            var hashCode = 1861411794;
            hashCode = hashCode * -1521134294 + X.GetHashCode();
            hashCode = hashCode * -1521134294 + Y.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Point3D left, Point3D right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Point3D left, Point3D right)
        {
            return !Equals(left, right);
        }

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
