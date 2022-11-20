using System.Collections.Generic;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class SingleDungeonMapFloorReference
    {
        public SmallMapReferences.SingleMapReference SingleMapReference { get; }

        public const int N_DUNGEON_ROWS_PER_MAP = 8;
        public const int N_DUNGEON_COLS_PER_ROW = 8;
        public const int N_BYTES_PER_FLOOR = N_DUNGEON_ROWS_PER_MAP * N_DUNGEON_COLS_PER_ROW;

        private readonly DungeonTile[,] _tileData =
            new DungeonTile[N_DUNGEON_ROWS_PER_MAP, N_DUNGEON_COLS_PER_ROW];

        public int DungeonFloor { get; }
        public SmallMapReferences.SingleMapReference.Location DungeonLocation { get; }

        public SingleDungeonMapFloorReference(SmallMapReferences.SingleMapReference.Location dungeonLocation,
            int nDungeonFloor, IReadOnlyList<byte> rawData)
        {
            DungeonFloor = nDungeonFloor;
            DungeonLocation = dungeonLocation;

            SingleMapReference =
                GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(dungeonLocation,
                    DungeonFloor); // cycle through all of the dungeons

            for (int nDungeonRow = 0; nDungeonRow < N_DUNGEON_ROWS_PER_MAP; nDungeonRow++)
            {
                for (int nDungeonCol = 0; nDungeonCol < N_DUNGEON_COLS_PER_ROW; nDungeonCol++)
                {
                    byte tileByte = rawData[nDungeonRow * N_DUNGEON_COLS_PER_ROW + nDungeonCol];
                    var dungeonTile = new DungeonTile(new Point2D(nDungeonCol, nDungeonRow),
                        (byte)((tileByte >> 4) & 0xF), (byte)(tileByte & 0xF));
                    _tileData[nDungeonCol, nDungeonRow] = dungeonTile;
                }
            }
        }
    }
}