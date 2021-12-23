using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.MapUnits
{
    /// <summary>
    ///     Tracks the position of any character on the screen
    /// </summary>
    [DataContract] public class MapUnitPosition
    {
        [DataMember]
        public int Floor
        {
            get => _floor;
            set => _floor = value == 0xFF ? -1 : value;
        }

        [DataMember]
        public int X
        {
            get;
            set;
            //XY.X = value;
        }

        [DataMember]
        public int Y
        {
            get;
            set;
            //XY.Y = value;
        }

        [IgnoreDataMember] private int _floor;
        //private Point2D _xy = new();

        [IgnoreDataMember]
        public Point2D XY
        {
            // get => _xy.Copy();
            // set => _xy = value.Copy();
            get => new(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        [JsonConstructor] public MapUnitPosition()
        {
        }

        public MapUnitPosition(int x, int y, int floor)
        {
            X = x;
            Y = y;
            Floor = floor;
        }

        public static bool operator ==(MapUnitPosition pos1, MapUnitPosition pos2)
        {
            return pos1?.Equals(pos2) ?? ReferenceEquals(pos2, null);
        }

        public static bool operator !=(MapUnitPosition pos1, MapUnitPosition pos2)
        {
            return !(pos1 == pos2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj is MapUnitPosition position && X == position.X && Y == position.Y && Floor == position.Floor;
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            int hashCode = 1832819848;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Floor.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return "X=" + X + ",Y=" + Y + ", Floor=" + Floor;
        }

        public string FriendlyString => $"{X},{Y} ({GetFriendlyFloorString(Floor)})";

        public string GetFriendlyFloorString(int nFloor) => nFloor switch
        {
            -1 => "Basement",
            0 => "Main Floor",
            1 => "First Floor",
            2 => "Second Floor",
            3 => "Third Floor",
            _ => "Unknown Floor"
        };
    }
}