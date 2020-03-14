using System;
using Newtonsoft.Json;

namespace Ultima5Redux
{
    public class Point2DFloat
    {
        public float X { get; set; }
        public float Y { get; set; }

        public bool WithinN (Point2DFloat xy, float nWithin)
        {
            bool bWithinX = Math.Abs(xy.X - X) <= nWithin;
            bool bWithinY = Math.Abs(xy.Y - Y) <= nWithin;
            return (bWithinX && bWithinY);
        }

        public double DistanceBetween(Point2DFloat xy)
        {
            return Math.Sqrt(Math.Pow(this.X - xy.X, 2) + Math.Pow(this.Y - xy.Y, 2));
        }

        public Point2DFloat Copy()
        {
            return new Point2DFloat(X, Y);
        }
        
        public override bool Equals(object obj)
        {
            if (!(obj is Point2DFloat))
                return false;

            return Equals((Point2DFloat)obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(Point2DFloat other)
        {
            if (other == null) return false;
            if (X != other.X)
                return false;

            return Y == other.Y;
        }

        public override string ToString()
        {
            return ("X="+X+",Y="+Y);
        }

        public static bool operator ==(Point2DFloat point1, Point2DFloat point2)
        {
            if (object.ReferenceEquals(point1, null))
            {
                return (object.ReferenceEquals(point2, null));
            }
            
            return point1.Equals(point2);
        }

        public static bool operator !=(Point2DFloat point1, Point2DFloat point2)
        {
            return !(point1==point2);
        }


        public Point2DFloat(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
        
    
    public class Point2D
    {
        public int X { get; set; }
        public int Y { get; set; }

        public bool WithinN (Point2D xy, int nWithin)
        {
            bool bWithinX = Math.Abs(xy.X - X) <= nWithin;
            bool bWithinY = Math.Abs(xy.Y - Y) <= nWithin;
            return (bWithinX && bWithinY);
        }

        public double DistanceBetween(Point2D xy)
        {
            return Math.Sqrt(Math.Pow(this.X - xy.X, 2) + Math.Pow(this.Y - xy.Y, 2));
        }

        public Point2D Copy()
        {
            return new Point2D(X, Y);
        }
        
        
        
        public override bool Equals(object obj)
        {
            if (!(obj is Point2D))
                return false;

            return Equals((Point2D)obj);
        }

        public bool Equals(Point2D other)
        {
            if (other == null) return false;
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

        public override string ToString()
        {
            return ("X="+X+",Y="+Y);
        }

        public static bool operator ==(Point2D point1, Point2D point2)
        {
            if (object.ReferenceEquals(point1, null))
            {
                return (object.ReferenceEquals(point2, null));
            }
            
            return point1.Equals(point2);
        }

        public static bool operator !=(Point2D point1, Point2D point2)
        {
            return !(point1==point2);
        }


        public Point2D(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}