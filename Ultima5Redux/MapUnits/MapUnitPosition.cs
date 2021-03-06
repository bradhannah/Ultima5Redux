﻿using System.Diagnostics.CodeAnalysis;

namespace Ultima5Redux.MapUnits
{
    /// <summary>
    ///     Tracks the position of any character on the screen
    /// </summary>
    public class MapUnitPosition
    {
        private int _floor;

        public MapUnitPosition()
        {
        }

        public MapUnitPosition(int x, int y, int floor)
        {
            X = x;
            Y = y;
            Floor = floor;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public int Floor
        {
            get => _floor;
            set => _floor = value == 0xFF ? -1 : value;
        }

        public Point2D XY
        {
            get => new Point2D(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public static bool operator !=(MapUnitPosition pos1, MapUnitPosition pos2)
        {
            return !(pos1 == pos2);
        }

        public static bool operator ==(MapUnitPosition pos1, MapUnitPosition pos2)
        {
            return ReferenceEquals(pos1, null) ? ReferenceEquals(pos2, null) : pos1.Equals(pos2);
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

            return obj is MapUnitPosition position &&
                   X == position.X &&
                   Y == position.Y &&
                   Floor == position.Floor;
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
    }
}