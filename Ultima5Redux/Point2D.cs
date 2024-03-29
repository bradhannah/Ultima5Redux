﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ultima5Redux
{
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public sealed class Point2DFloat
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Point2DFloat(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(Point2DFloat point1, Point2DFloat point2) =>
            point1?.Equals(point2) ?? ReferenceEquals(point2, null);


        public static bool operator !=(Point2DFloat point1, Point2DFloat point2) => !(point1 == point2);

        public override bool Equals(object obj) => obj is Point2DFloat point2DFloat && Equals(point2DFloat);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString() => GetFriendlyString();

        public Point2DFloat Copy() => new(X, Y);

        public double DistanceBetween(Point2DFloat xy) => Math.Sqrt(Math.Pow(X - xy.X, 2) + Math.Pow(Y - xy.Y, 2));

        public bool Equals(Point2DFloat other)
        {
            if (other == null) return false;
            if (Math.Abs(X - other.X) > 0.0000001f)
                return false;

            return Math.Abs(Y - other.Y) < 0.0000001f;
        }

        public string GetFriendlyString() => "X=" + X + ",Y=" + Y;

        public bool WithinN(Point2DFloat xy, float nWithin)
        {
            bool bWithinX = Math.Abs(xy.X - X) <= nWithin;
            bool bWithinY = Math.Abs(xy.Y - Y) <= nWithin;
            return bWithinX && bWithinY;
        }
    }

    [DataContract] public sealed class Point2D
    {
        /// <summary>
        ///     4 way direction
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Direction { Up, Down, Left, Right, None }

        [DataMember] public int X { get; set; }
        [DataMember] public int Y { get; set; }

        public static Point2D Zero => new(0, 0);

        public Point2D()
        {
        }

        public Point2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        ///     Provides a list of intersecting points between two points
        /// </summary>
        /// <param name="startPoint">start point</param>
        /// <param name="endPoint">end point</param>
        /// <returns>A list of intersecting points</returns>
        /// <remarks>stolen from http://playtechs.blogspot.com/2007/03/raytracing-on-grid.html and reworked</remarks>
        private static List<Point2D> Raytrace(Point2D startPoint, Point2D endPoint)
        {
            // if the points are adjacent then we simplify it and just return the first and last point
            // if we don't do this then it will pollute the attack path with another tile which can
            // cause blocked shots when attacking diagonally
            if (Math.Abs(startPoint.X - endPoint.X) <= 1 && Math.Abs(startPoint.Y - endPoint.Y) <= 1)
            {
                return new List<Point2D> { startPoint, endPoint };
            }

            int dx = Math.Abs(endPoint.X - startPoint.X);
            int dy = Math.Abs(endPoint.Y - startPoint.Y);
            int x = startPoint.X;
            int y = startPoint.Y;
            int n = 1 + dx + dy;
            int nXInc = endPoint.X > startPoint.X ? 1 : -1;
            int nYInc = endPoint.Y > startPoint.Y ? 1 : -1;
            int error = dx - dy;
            dx *= 2;
            dy *= 2;

            List<Point2D> intersectingPoints = new();

            for (; n > 0; --n)
            {
                intersectingPoints.Add(new Point2D(x, y));

                if (error > 0)
                {
                    x += nXInc;
                    error -= dy;
                }
                else
                {
                    y += nYInc;
                    error += dx;
                }
            }

            return intersectingPoints;
        }

        public static int AdjustToMax(int nNum, int nMax)
        {
            if (nNum < 0) nNum += nMax;
            if (nNum >= nMax) nNum -= nMax;
            nNum %= nMax;
            return nNum;
        }

        public static double DistanceBetween(int x1, int y1, int x2, int y2) =>
            Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));

        public static bool IsOppositeDirection(Direction direction1, Direction direction2)
        {
            switch (direction1)
            {
                case Direction.Down when direction2 == Direction.Up:
                case Direction.Up when direction2 == Direction.Down:
                case Direction.Left when direction2 == Direction.Right:
                case Direction.Right when direction2 == Direction.Left:
                    return true;
                case Direction.None:
                    return false;
                default:
                    return false;
            }
        }

        public static bool IsOutOfRangeStatic(int nX, int nY, int nMaxX, int nMaxY, int nMinX = 0, int nMinY = 0) =>
            nX < nMinX || nX > nMaxX || nY < nMinY || nY > nMaxY;

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public static bool IsWithinNFourDirections(int x1, int y1, int x2, int y2) =>
            Math.Abs(DistanceBetween(x1, y1, x2, y2) - 1) < 0.01;

        public static bool operator ==(in Point2D point1, in Point2D point2) =>
            point1?.Equals(point2) ?? ReferenceEquals(point2, null);

        public static bool operator !=(in Point2D point1, in Point2D point2) => !(point1 == point2);

        public override bool Equals(object obj) => obj is Point2D point2D && Equals(point2D);

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            int hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            return hashCode;
        }

        /// <summary>
        ///     Magic function that takes care of IsRepeatable maps by wrapping them around
        /// </summary>
        /// <param name="nMax"></param>
        public void AdjustXAndYToMax(int nMax)
        {
            if (X < 0) X += nMax;
            if (X >= nMax) X -= nMax;
            X %= nMax;
            if (Y < 0) Y += nMax;
            if (Y >= nMax) Y -= nMax;
            Y %= nMax;
        }

        public Point2D Copy() => new(X, Y);

        public double DistanceBetween(Point2D xy) => Math.Sqrt(Math.Pow(X - xy.X, 2) + Math.Pow(Y - xy.Y, 2));

        [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
        public double DistanceBetweenWithWrapAround(Point2D xy, int nMax)
        {
            double dX = Math.Abs(X - xy.X);
            double dY = Math.Abs(Y - xy.Y);

            if (dX > nMax / 2.0d) dX = nMax - dX;
            if (dY > nMax / 2.0d) dY = nMax - dY;

            return Math.Sqrt(dX * dX + dY * dY);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public bool Equals(in Point2D other)
        {
            if (other == null) return false;
            if (X != other.X)
                return false;

            return Y == other.Y;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
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

        // ReSharper disable once UnusedMember.Global
        public Point2D GetAdjustXAndYToMax(int nMax)
        {
            Point2D position = this;
            position.AdjustXAndYToMax(nMax);
            return position;
        }

        /// <summary>
        ///     Gets the north, south, east and west points within the zero (only positive) based extents provided
        /// </summary>
        /// <param name="nXExtent"></param>
        /// <param name="nYExtent"></param>
        /// <returns>a list of valid points</returns>
        public List<Point2D> GetConstrainedFourDirectionSurroundingPoints(int nXExtent, int nYExtent)
        {
            List<Point2D> points = new();

            if (X - 1 >= 0) points.Add(new Point2D(X - 1, Y));
            if (Y - 1 >= 0) points.Add(new Point2D(X, Y - 1));
            if (X + 1 <= nXExtent) points.Add(new Point2D(X + 1, Y));
            if (Y + 1 <= nYExtent) points.Add(new Point2D(X, Y + 1));

            return points;
        }

        /// <summary>
        ///     This will get all the points that are closer to the given point - good for "dumbly" tracking
        ///     another character - such as Drudgeworth in jail, since the A* breaks down when a locked door
        ///     is in the way
        /// </summary>
        /// <param name="closerToPosition"></param>
        /// <param name="nXExtent"></param>
        /// <param name="nYExtent"></param>
        /// <returns></returns>
        public IEnumerable<Point2D> GetConstrainedFourDirectionSurroundingPointsCloserTo(
            Point2D closerToPosition, int nXExtent, int nYExtent)
        {
            List<Point2D> fourPointList = GetConstrainedFourDirectionSurroundingPoints(nXExtent, nYExtent);
            double dCurrentDistanceBetween = DistanceBetween(closerToPosition);

            return fourPointList.Where(
                point => point.DistanceBetween(closerToPosition) < dCurrentDistanceBetween);
        }

        /// <summary>
        ///     This will get any point surrounding a single point that is further away from a destination point
        ///     For example - if you had a character who was trying to get further away from another character
        ///     this would give you the potential points that are technically further away
        /// </summary>
        /// <param name="furtherAwayFromPosition"></param>
        /// <param name="nXExtent"></param>
        /// <param name="nYExtent"></param>
        /// <returns></returns>
        public IEnumerable<Point2D> GetConstrainedFourDirectionSurroundingPointsFurtherAway(
            Point2D furtherAwayFromPosition,
            int nXExtent, int nYExtent)
        {
            List<Point2D> fourPointList = GetConstrainedFourDirectionSurroundingPoints(nXExtent, nYExtent);
            double dCurrentDistanceBetween = DistanceBetween(furtherAwayFromPosition);

            return fourPointList.Where(
                point => point.DistanceBetween(furtherAwayFromPosition) > dCurrentDistanceBetween);
        }

        public List<Point2D> GetConstrainedFourDirectionSurroundingPointsWrapAround(int nXExtent, int nYExtent)
        {
            List<Point2D> points = new()
            {
                X - 1 >= 0 ? new Point2D(X - 1, Y) : new Point2D(nXExtent, Y),
                Y - 1 >= 0 ? new Point2D(X, Y - 1) : new Point2D(X, nYExtent),
                X + 1 <= nXExtent ? new Point2D(X + 1, Y) : new Point2D(0, Y),
                Y + 1 <= nYExtent ? new Point2D(X, Y + 1) : new Point2D(X, 0)
            };

            return points;
        }

        /// <summary>
        ///     Gets a list of points that surround a particular point at "n units out". If you outside points
        ///     exceed the given points then it will add the points of the outermost yet valid points
        /// </summary>
        /// <param name="nUnitsOut">how many units from the current point should it go out from</param>
        /// <param name="nXExtent">assuming 0 is left most, what is the x extent?</param>
        /// <param name="nYExtent">assuming 0 is top most, what is the y extent?</param>
        /// <returns></returns>
        public List<Point2D> GetConstrainedSurroundingPoints(int nUnitsOut, int nXExtent, int nYExtent)
        {
            Debug.Assert(nUnitsOut >= 0);
            if (nUnitsOut == 0) return new List<Point2D> { this };

            List<Point2D> points = new();

            AddAcross(Y - nUnitsOut, nUnitsOut, nXExtent, points, nYExtent);
            AddAcross(Y + nUnitsOut, nUnitsOut, nXExtent, points, nYExtent);
            AddDown(X - nUnitsOut, nUnitsOut, nYExtent, points, nXExtent);
            AddDown(X + nUnitsOut, nUnitsOut, nYExtent, points, nXExtent);

            return points;
        }

        private void AddDown(int nX, int nUnitsOut, int nYExtent, ICollection<Point2D> points, int nXExtent)
        {
            for (int nY = Math.Max(0, Y - nUnitsOut + 1); nY < Math.Min(nYExtent, Y + nUnitsOut); nY++)
            {
                points.Add(new Point2D(Math.Min(Math.Max(0, nX), nXExtent), nY));
            }
        }

        private void AddAcross(int nY, int nUnitsOut, int nXExtent, ICollection<Point2D> points, int nYExtent)
        {
            Debug.Assert(nY >= 0);
            for (int nX = Math.Max(0, X - nUnitsOut); nX < Math.Min(nXExtent, X + nUnitsOut + 1); nX++)
            {
                points.Add(new Point2D(nX, Math.Min(Math.Max(0, nY), nYExtent)));
            }
        }

        public string GetFriendlyString() => "X=" + X + ",Y=" + Y;


        /// <summary>
        ///     Determines if the current point is out of the range provided
        ///     zero based (ie. if X > nMaxX)
        /// </summary>
        /// <param name="nMaxX">max X value</param>
        /// <param name="nMaxY">max Y value</param>
        /// <param name="nMinX">min X value</param>
        /// <param name="nMinY">min Y value</param>
        /// <returns></returns>
        public bool IsOutOfRange(int nMaxX, int nMaxY, int nMinX = 0, int nMinY = 0) =>
            X < nMinX || X > nMaxX || Y < nMinY || Y > nMaxY;

        public bool IsWithinN(Point2D xy, int nWithin)
        {
            bool bWithinX = Math.Abs(xy.X - X) <= nWithin;
            bool bWithinY = Math.Abs(xy.Y - Y) <= nWithin;
            return bWithinX && bWithinY;
        }

        // is the point given point in one of the four directions given?
        public bool IsWithinNFourDirections(in Point2D xy) => Math.Abs(DistanceBetween(xy) - 1) < 0.01;

        public bool IsWithinNFourDirections(int x, int y) => Math.Abs(DistanceBetween(x, y, X, Y) - 1) < 0.01;

        /// <summary>
        ///     Provides a list of intersecting points between two points
        /// </summary>
        /// <param name="endPoint">the end point you want to point to</param>
        /// <returns>A list of intersecting points</returns>
        public List<Point2D> Raytrace(in Point2D endPoint) => Raytrace(this, endPoint);
    }
}