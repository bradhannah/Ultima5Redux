using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.References;

namespace Ultima5Redux.DayNightMoon
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")] [DataContract]
    public class Moongates
    {
        /// <summary>
        ///     Total number of moonstones in game
        /// </summary>
        private const int TOTAL_MOONSTONES = 8;

        /// <summary>
        ///     All buried positions
        /// </summary>
        [DataMember(Name = "MoongatePositions")]
        private readonly List<Point3D> _moongatePositions = new(TOTAL_MOONSTONES);

        private readonly Dictionary<Point3D, bool> _moongatePositionsDictionary = new(TOTAL_MOONSTONES);

        /// <summary>
        ///     Are moonstones buried?
        /// </summary>
        [DataMember(Name = "MoonstoneBuried")] private readonly List<bool> _moonstonesBuried = new(TOTAL_MOONSTONES);

        [JsonConstructor] private Moongates()
        {
        }

        /// <summary>
        ///     Constructor. Built with DataChunk references from save file
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <param name="buriedFlags"></param>
        /// <param name="zPos"></param>
        public Moongates(DataChunk xPos, DataChunk yPos, DataChunk buriedFlags, DataChunk zPos)
        {
            List<byte> xPositions = xPos.GetAsByteList();
            List<byte> yPositions = yPos.GetAsByteList();
            List<byte> zPositions = zPos.GetAsByteList();
            List<byte> buried = buriedFlags.GetAsByteList();
            Debug.Assert(xPositions.Count == TOTAL_MOONSTONES && yPositions.Count == TOTAL_MOONSTONES &&
                         zPositions.Count == TOTAL_MOONSTONES && buried.Count == TOTAL_MOONSTONES);

            for (int i = 0; i < TOTAL_MOONSTONES; i++)
            {
                Point3D moongatePosition = new(xPositions[i], yPositions[i], zPositions[i]);

                bool bIsBuried = buried[i] == 0;
                _moongatePositions.Add(moongatePosition);
                _moonstonesBuried.Add(bIsBuried);
                if (bIsBuried)
                    _moongatePositionsDictionary.Add(moongatePosition, true);
            }
        }

        /// <summary>
        ///     Gets the position of an indexed moongate (in order from the save file)
        /// </summary>
        /// <param name="nMoongateIndex">0-7</param>
        /// <returns></returns>
        public Point3D GetMoongatePosition(int nMoongateIndex)
        {
            return _moongatePositions[nMoongateIndex];
        }

        public MoonPhaseReferences.MoonPhases GetMoonPhaseByPosition(Point2D position, Map.Maps map)
        {
            if (!IsMoonstoneBuried(position, map))
                throw new Ultima5ReduxException("Can't get a moonphase for a stone that ain't there at " + position);
            int nPos = -1;
            Point3D xyzPos = new(position.X, position.Y, (int)map);
            for (int nMoonstone = 0; nMoonstone < 8; nMoonstone++)
            {
                Point3D xyz = _moongatePositions[nMoonstone];
                if (xyz.Z != (int)map) continue;
                if (xyzPos == xyz)
                {
                    nPos = nMoonstone;
                    break;
                }
            }

            if (nPos == -1) throw new Ultima5ReduxException("Unable to get moon phase by position");
            // the actual position in the array signifies the current moon phase
            return (MoonPhaseReferences.MoonPhases)nPos;
        }

        /// <summary>
        ///     Is the moonstone buried, indexed moongate (in order from the save file)
        /// </summary>
        /// <param name="nMoonstoneIndex"></param>
        /// <returns></returns>
        public bool IsMoonstoneBuried(int nMoonstoneIndex)
        {
            Debug.Assert(nMoonstoneIndex >= 0 && nMoonstoneIndex < TOTAL_MOONSTONES);
            return _moonstonesBuried[nMoonstoneIndex];
        }

        /// <summary>
        ///     Is there a moonstone buried at a particular position?
        /// </summary>
        /// <param name="position"></param>
        /// <returns>true if one is buried</returns>
        public bool IsMoonstoneBuried(Point3D position)
        {
            return _moongatePositionsDictionary.ContainsKey(position);
        }

        public bool IsMoonstoneBuried(in Point2D position, Map.Maps map)
        {
            return IsMoonstoneBuried(new Point3D(position.X, position.Y, map == Map.Maps.Overworld ? 0 : 0xFF));
        }

        /// <summary>
        ///     Sets the buried status of a Moonstone
        /// </summary>
        /// <param name="nMoonstoneIndex"></param>
        /// <param name="bBuried"></param>
        public void SetMoonstoneBuried(int nMoonstoneIndex, bool bBuried)
        {
            Point3D currentPosition = GetMoongatePosition(nMoonstoneIndex);
            bool bPositionRegistered = _moongatePositionsDictionary.ContainsKey(currentPosition);
            if (bBuried)
            {
                if (bPositionRegistered)
                    _moongatePositionsDictionary[currentPosition] = true;
                else
                    _moongatePositionsDictionary.Add(currentPosition, true);
            }
            else
            {
                if (_moongatePositionsDictionary.ContainsKey(currentPosition))
                    _moongatePositionsDictionary.Remove(currentPosition);
            }

            _moonstonesBuried[nMoonstoneIndex] = bBuried;
        }

        public void SetMoonstoneBuried(int nMoonstoneIndex, bool bBuried, Point3D xyz)
        {
            Debug.Assert(xyz.Z == 0 || xyz.Z == 0xFF);
            // this one we can just update since it is a zero based index
            _moongatePositions[nMoonstoneIndex] = xyz;
            // set the moonstone to the appropriate buried sate
            SetMoonstoneBuried(nMoonstoneIndex, bBuried);
        }
    }
}