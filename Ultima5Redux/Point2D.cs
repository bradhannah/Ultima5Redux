using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Ultima5Redux.Maps;

namespace Ultima5Redux
{
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")] 
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public class Point2DFloat
    {
        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public Point2DFloat(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; set; }
        public float Y { get; set; }

        public bool WithinN(Point2DFloat xy, float nWithin)
        {
            bool bWithinX = Math.Abs(xy.X - X) <= nWithin;
            bool bWithinY = Math.Abs(xy.Y - Y) <= nWithin;
            return bWithinX && bWithinY;
        }

        public double DistanceBetween(Point2DFloat xy)
        {
            return Math.Sqrt(Math.Pow(X - xy.X, 2) + Math.Pow(Y - xy.Y, 2));
        }

        public Point2DFloat Copy()
        {
            return new Point2DFloat(X, Y);
        }

        public override bool Equals(object obj) => obj is Point2DFloat point2DFloat && Equals(point2DFloat);

        public bool Equals(Point2DFloat other)
        {
            if (other == null) return false;
            if (Math.Abs(X - other.X) > 0.0000001f)
                return false;

            return Math.Abs(Y - other.Y) < 0.0000001f;
        }

        public override string ToString()
        {
            return "X=" + X + ",Y=" + Y;
        }

        public static bool operator ==(Point2DFloat point1, Point2DFloat point2)
        {
            if (ReferenceEquals(point1, null)) return ReferenceEquals(point2, null);

            return point1.Equals(point2);
        }

        public static bool operator !=(Point2DFloat point1, Point2DFloat point2)
        {
            return !(point1 == point2);
        }
    }

    public class Point2D
    {
        /// <summary>
        ///     4 way direction
        /// </summary>
        public enum Direction { Up, Down, Left, Right, None }


        public Point2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public Point2D GetPoint2DOrNullOutOfRange(int nMaxX, int nMaxY, int nMinX = 0, int nMinY = 0)
        {
            return IsOutOfRange(nMaxX, nMaxY, nMinX, nMinY) ? null : this;
        }

        public bool IsOutOfRange(int nMaxX, int nMaxY, int nMinX = 0, int nMinY = 0)
        {
            return (X < nMinX || X > nMaxX || Y < nMinY || Y > nMaxY);
        }

        public Point2D GetAdjustedPosition(Direction direction, int nMaxX, int nMaxY, int nMinX = 0, int nMinY = 0)
        {
            Point2D adjustedPos = GetAdjustedPosition(direction);
            return adjustedPos.IsOutOfRange(nMaxX, nMaxY, nMinX, nMinY) ? null : adjustedPos;
        }

        public Point2D GetAdjustedPosition(Direction direction, int nSpaces = 1)
        {
            Point2D adjustedPos = Copy();

            switch (direction)
            {
                case Direction.None:
                    // no movement
                    break;
                case Direction.Right:
                    adjustedPos.X += nSpaces;
                    break;
                case Direction.Up:
                    adjustedPos.Y -= nSpaces;
                    break;
                case Direction.Left:
                    adjustedPos.X -= nSpaces;
                    break;
                case Direction.Down:
                    adjustedPos.Y += nSpaces;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            return adjustedPos;
        }

        public void AdjustXAndYToMax(int nMax)
        {
            if (X < 0) X += nMax;
            if (X >= nMax) X -= nMax;
            X %= nMax;
            if (Y < 0) Y += nMax;
            if (Y >= nMax) Y -= nMax;
            Y %= nMax;
        }

        public bool IsWithinN(Point2D xy, int nWithin)
        {
            bool bWithinX = Math.Abs(xy.X - X) <= nWithin;
            bool bWithinY = Math.Abs(xy.Y - Y) <= nWithin;
            return bWithinX && bWithinY;
        }

        // is the point given point in one of the four directions given?
        public bool IsWithinNFourDirections(Point2D xy)
        {
            return Math.Abs(DistanceBetween(xy) - 1) < 0.01;
        }

        public double DistanceBetween(Point2D xy)
        {
            return Math.Sqrt(Math.Pow(X - xy.X, 2) + Math.Pow(Y - xy.Y, 2));
        }

        /// <summary>
        /// Gets the north, south, east and west points within the zero (only positive) based extents provided
        /// </summary>
        /// <param name="nXExtent"></param>
        /// <param name="nYExtent"></param>
        /// <returns>a list of valid points</returns>
        public List<Point2D> GetConstrainedFourDirectionSurroundingPoints(int nXExtent, int nYExtent)
        {
            List<Point2D> points = new List<Point2D>();
            
            if (X - 1 >= 0) points.Add(new Point2D(X - 1, Y));
            if (Y - 1 >= 0) points.Add(new Point2D(X, Y - 1));
            if (X + 1 <= nXExtent) points.Add(new Point2D(X + 1, Y));
            if (Y + 1 <= nYExtent) points.Add(new Point2D(X, Y + 1));

            return points;
        }

        /// <summary>
        /// Gets a list of points that surround a particular point at "n units out". If you outside points
        /// exceed the given points then it will add the points of the outermost yet valid points
        /// </summary>
        /// <param name="nUnitsOut">how many units from the current point should it go out from</param>
        /// <param name="nXExtent">assuming 0 is left most, what is the x extent?</param>
        /// <param name="nYExtent">assuming 0 is up most, what is the y extent?</param>
        /// <returns></returns>
        public List<Point2D> GetConstrainedSurroundingPoints(int nUnitsOut, int nXExtent, int nYExtent)
        {
            Debug.Assert(nUnitsOut >= 0);
            if (nUnitsOut == 0) return new List<Point2D>() {this};

            List<Point2D> points = new List<Point2D>();

            void addAcross(int nY)
            {
                Debug.Assert(nY >= 0);
                for (int nX = Math.Max(0, this.X - nUnitsOut); nX < Math.Min(nXExtent, X + nUnitsOut + 1); nX++)
                {
                    points.Add(new Point2D(nX, Math.Min(Math.Max(0,nY), nYExtent)));
                }
            }

            void addDown(int nX)
            {
                for (int nY = Math.Max(0, this.Y - nUnitsOut + 1); nY < Math.Min(nYExtent, Y + nUnitsOut); nY++)
                {
                    points.Add(new Point2D(Math.Min(Math.Max(0,nX),nXExtent), nY));
                }
            }
            
            addAcross(Y - nUnitsOut);
            addAcross(Y + nUnitsOut);
            addDown(X - nUnitsOut);
            addDown(X + nUnitsOut);

            return points;
        }

        /// <summary>
        /// Provides a list of intersecting points between two points
        /// </summary>
        /// <param name="endPoint">the end point you want to point to</param>
        /// <returns>A list of intersecting points</returns>
        public List<Point2D> Raytrace(Point2D endPoint) => Raytrace(this, endPoint);
        
        /// <summary>
        /// Provides a list of intersecting points between two points
        /// </summary>
        /// <param name="startPoint">start point</param>
        /// <param name="endPoint">end point</param>
        /// <returns>A list of intersecting points</returns>
        /// <remarks>stolen from http://playtechs.blogspot.com/2007/03/raytracing-on-grid.html and reworked</remarks>
        public static List<Point2D> Raytrace(Point2D startPoint, Point2D endPoint)
        {
            int dx = Math.Abs(endPoint.X - startPoint.X);
            int dy = Math.Abs(endPoint.Y - startPoint.Y);
            int x = startPoint.X;
            int y = startPoint.Y;
            int n = 1 + dx + dy;
            int x_inc = (endPoint.X > startPoint.X) ? 1 : -1;
            int y_inc = (endPoint.Y > startPoint.Y) ? 1 : -1;
            int error = dx - dy;
            dx *= 2;
            dy *= 2;

            List<Point2D> intersectingPoints = new List<Point2D>(); 

            for (; n > 0; --n)
            {
                //visit(x, y);
                intersectingPoints.Add(new Point2D(x, y));

                if (error > 0)
                {
                    x += x_inc;
                    error -= dy;
                }
                else
                {
                    y += y_inc;
                    error += dx;
                }
            }

            return intersectingPoints;
        }
        

        public Point2D Copy()
        {
            return new Point2D(X, Y);
        }


        public override bool Equals(object obj)
        {
            return obj is Point2D point2D && Equals(point2D);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public bool Equals(Point2D other)
        {
            if (other == null) return false;
            if (X != other.X)
                return false;

            return Y == other.Y;
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")] 
        public override int GetHashCode()
        {
            int hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return "X=" + X + ",Y=" + Y;
        }

        public static bool operator ==(Point2D point1, Point2D point2)
        {
            return point1?.Equals(point2) ?? ReferenceEquals(point2, null);
        }

        public static bool operator !=(Point2D point1, Point2D point2)
        {
            return !(point1 == point2);
        }

    }
}