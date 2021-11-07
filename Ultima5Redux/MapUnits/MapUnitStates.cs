using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits
{
    /// <summary>
    ///     Map character animation states
    ///     This is a generic application of it. The raw data must be passed in during construction.
    /// </summary>
    public class MapUnitStates
    {
        public enum MapUnitStatesFiles { SAVED_GAM, BRIT_OOL, UNDER_OOL }

        private const int MAX_CHARACTER_STATES = 0x20;

        private readonly List<MapUnitState> _mapUnitStates = new List<MapUnitState>(MAX_CHARACTER_STATES);
        private readonly TileReferences _tileReferences;

        private DataChunk _mapUnitStatesDataChunk;

        public MapUnitStates(DataChunk mapUnitStatesDataChunk, TileReferences tileReferences)
        {
            _tileReferences = tileReferences;
            _mapUnitStatesDataChunk = mapUnitStatesDataChunk;
        }

        public MapUnitStates(TileReferences tileReferences)
        {
            _tileReferences = tileReferences;
            _mapUnitStatesDataChunk = null;
            // initialize an empty set of character states (for combat map likely)
            for (int i = 0; i < MAX_CHARACTER_STATES; i++)
            {
                MapUnitState mapUnitState = new MapUnitState();
                _mapUnitStates.Add(mapUnitState);
            }
        }

        private MapUnitStatesFiles MapUnitStatesType { get; set; }

        public MapUnitState GetCharacterState(int nIndex)
        {
            return _mapUnitStates[nIndex];
        }

        public MapUnitState GetCharacterStateByPosition(Point2D xy, int nFloor)
        {
            foreach (MapUnitState characterState in _mapUnitStates)
            {
                if (characterState.X == xy.X && characterState.Y == xy.Y && characterState.Floor == nFloor)
                    return characterState;
            }

            return null;
        }

        /// <summary>
        ///     Load the character animation states into the object
        /// </summary>
        /// <param name="mapUnitStatesType"></param>
        /// <param name="bLoadFromDisk"></param>
        /// <param name="nOffset"></param>
        public void Load(MapUnitStatesFiles mapUnitStatesType, bool bLoadFromDisk, int nOffset = 0x00)
        {
            MapUnitStatesType = mapUnitStatesType;
            
            _mapUnitStates.Clear();

            if (!bLoadFromDisk) return;

            List<byte> characterStateBytes = _mapUnitStatesDataChunk.GetAsByteList();

            for (int i = 0; i < MAX_CHARACTER_STATES; i++)
            {
                _mapUnitStates.Add(new MapUnitState(_tileReferences,
                    characterStateBytes.GetRange(i * MapUnitState.NBYTES + nOffset,
                        MapUnitState.NBYTES).ToArray()));
            }
        }

    }
}