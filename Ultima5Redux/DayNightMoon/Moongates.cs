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
        private readonly List<Point3D> _moongatePositions = new List<Point3D>(TOTAL_MOONSTONES);

        /// <summary>
        ///     Are moonstones buried?
        /// </summary>
        [DataMember(Name = "MoonstoneBuried")]
        private readonly List<bool> _moonstonesBuried = new List<bool>(TOTAL_MOONSTONES);

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
                Point3D moongatePos = new Point3D(xPositions[i], yPositions[i], zPositions[i]);
                _moongatePositions.Add(moongatePos);
                _moonstonesBuried.Add(buried[i] == 0);
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
            Point3D xyzPos = new Point3D(position.X, position.Y, (int)map);
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
            if (!_moongatePositions.Contains(position)) return false;

            // feels inefficient but it's only 8 loops
            for (int i = 0; i < _moongatePositions.Count; i++)
            {
                if (_moongatePositions[i] == position)
                {
                    return _moonstonesBuried[i];
                }
            }

            return false;
        }

        public bool IsMoonstoneBuried(Point2D position, Map.Maps map)
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