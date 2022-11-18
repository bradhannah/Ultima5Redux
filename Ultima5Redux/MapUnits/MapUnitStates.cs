using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.References;

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

        private readonly List<MapUnitState> _mapUnitStates = new(MAX_CHARACTER_STATES);

        private readonly DataChunk _mapUnitStatesDataChunk;

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private MapUnitStatesFiles MapUnitStatesType { get; set; }

        public MapUnitStates(DataChunk mapUnitStatesDataChunk) => _mapUnitStatesDataChunk = mapUnitStatesDataChunk;

        public MapUnitStates()
        {
            _mapUnitStatesDataChunk = null;
            // initialize an empty set of character states (for combat map likely)
            for (int i = 0; i < MAX_CHARACTER_STATES; i++)
            {
                MapUnitState mapUnitState = new();
                _mapUnitStates.Add(mapUnitState);
            }
        }

        public MapUnitState GetCharacterState(int nIndex) => _mapUnitStates[nIndex];

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
        public void InitializeMapUnits(MapUnitStatesFiles mapUnitStatesType, bool bLoadFromDisk, int nOffset = 0x00)
        {
            MapUnitStatesType = mapUnitStatesType;

            _mapUnitStates.Clear();

            if (!bLoadFromDisk)
            {
                _mapUnitStates.AddRange(new MapUnitState[MAX_CHARACTER_STATES]);
                return;
            }

            List<byte> characterStateBytes = _mapUnitStatesDataChunk.GetAsByteList();

            for (int i = 0; i < MAX_CHARACTER_STATES; i++)
            {
                MapUnitState mapUnitState = new(characterStateBytes
                    .GetRange(i * MapUnitState.NBYTES + nOffset, MapUnitState.NBYTES).ToArray());

                // cute little hack that makes sure the first unit is ALWAYS the Avatar. The init.gam does not properly
                // set this up otherwise
                if (i == 0 && mapUnitState.Tile1Ref.Index == 256)
                    mapUnitState.SetTileReference(
                        GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("BasicAvatar"));

                _mapUnitStates.Add(mapUnitState);
            }
        }
    }
}