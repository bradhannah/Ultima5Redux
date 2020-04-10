namespace Ultima5Redux.MapCharacters
{
    /// <summary>
    /// Tracks the postion of any character on the screen
    /// </summary>
    public class CharacterPosition
    {
        public CharacterPosition()
        {}
        
        public CharacterPosition(int x, int y, int floor)
        {
            X = x;
            Y = y;
            Floor = floor;
        }
        
        
        private int _floor;
        public int X { get; set; }
        public int Y { get; set; }

        public int Floor
        {
            get => _floor;
            set => _floor = value==0xFF?-1:value;
        }

        public Point2D XY { get { return new Point2D(X, Y); } set { X = value.X; Y = value.Y; } }

        public static bool operator !=(CharacterPosition pos1, CharacterPosition pos2)
        {
            return !(pos1 == pos2);
        }
        public static bool operator ==(CharacterPosition pos1, CharacterPosition pos2)
        {
            if (object.ReferenceEquals(pos1, null))
            {
                return (object.ReferenceEquals(pos2, null));
            }

            return pos1.Equals(pos2);
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;

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

        public override string ToString()
        {
            return ("X=" + X + ",Y=" + Y+", Floor="+Floor);
        }
    }
}
