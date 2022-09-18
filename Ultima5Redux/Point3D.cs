using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Ultima5Redux
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    [DataContract]
    public sealed class Point3D
    {
        [DataMember] public int X { get; set; }
        [DataMember] public int Y { get; set; }
        [DataMember] public int Z { get; set; }

        public Point3D(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static bool operator ==(Point3D left, Point3D right) => Equals(left, right);

        public static bool operator !=(Point3D left, Point3D right) => !Equals(left, right);

        public override bool Equals(object obj) => Equals(obj as Point3D);

        public override int GetHashCode()
        {
            int hashCode = -307843816;
            hashCode = hashCode * -1521134295 + X;
            hashCode = hashCode * -1521134295 + Y;
            hashCode = hashCode * -1521134295 + Z;
            return hashCode;
        }

        public bool Equals(Point3D p3d)
        {
            if (ReferenceEquals(null, p3d)) return false;
            if (ReferenceEquals(this, p3d)) return true;
            if (p3d.GetType() != GetType()) return false;
            return p3d.X == X && p3d.Y == Y && p3d.Z == Z;
        }
    }
}