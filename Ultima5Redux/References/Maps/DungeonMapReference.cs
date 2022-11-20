using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.Maps;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References
{
    // maybe just this one class because a single tile can transform

    public class DungeonMapReference
    {
        public const int N_DUNGEONS = 8;
        public const int N_DUNGEON_FLOORS_PER_MAP = 8;

        public const int N_BYTES_PER_DUNGEON = SingleDungeonMapFloorReference.N_BYTES_PER_FLOOR *
                                               N_DUNGEON_FLOORS_PER_MAP;

        public const int N_BYTES_PER_DUNGEON_FLOOR = SingleDungeonMapFloorReference.N_DUNGEON_ROWS_PER_MAP *
                                                     SingleDungeonMapFloorReference.N_DUNGEON_COLS_PER_ROW;

        private readonly SingleDungeonMapFloorReference[] _singleDungeonMapFloorReferences = new
            SingleDungeonMapFloorReference[N_DUNGEON_FLOORS_PER_MAP];

        public SmallMapReferences.SingleMapReference.Location DungeonLocation { get; }

        public SingleDungeonMapFloorReference GetSingleDungeonMapFloorReferenceByFloor(int nFloor) =>
            _singleDungeonMapFloorReferences[nFloor];

        public DungeonMapReference(SmallMapReferences.SingleMapReference.Location dungeonLocation,
            IReadOnlyList<byte> rawData)
        {
            DungeonLocation = dungeonLocation;

            for (int nDungeonFloor = 0; nDungeonFloor < N_DUNGEON_FLOORS_PER_MAP; nDungeonFloor++)
            {
                SingleDungeonMapFloorReference singleDungeonMapFloorReference = new(dungeonLocation, nDungeonFloor,
                    rawData.ToList().GetRange(SingleDungeonMapFloorReference.N_BYTES_PER_FLOOR * nDungeonFloor,
                        SingleDungeonMapFloorReference.N_BYTES_PER_FLOOR));
                _singleDungeonMapFloorReferences[nDungeonFloor] = singleDungeonMapFloorReference;
            }
        }
    }
}