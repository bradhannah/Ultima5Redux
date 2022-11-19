namespace Ultima5Redux.References
{
    // maybe just this one class because a single tile can transform

    public class SingleDungeonMapReference
    {
        public const int N_DUNGEONS = 8;
        public const int N_DUNGEON_FLOORS_PER_MAP = 8;
        public const int N_DUNGEON_ROWS_PER_MAP = 8;
        public const int N_DUNGEON_TILE_PER_ROW = 8;
        public const int N_BYTES_PER_DUNGEON = N_DUNGEON_ROWS_PER_MAP * N_DUNGEON_TILE_PER_ROW;

        private readonly DungeonTile[,,] _tileData =
            new DungeonTile[N_DUNGEON_FLOORS_PER_MAP, N_DUNGEON_ROWS_PER_MAP, N_DUNGEON_TILE_PER_ROW];

        public SingleDungeonMapReference(byte[] rawData)
        {
            // cycle through all of the dungeons
            for (int nDungeonFloor = 0; nDungeonFloor < N_DUNGEON_TILE_PER_ROW; nDungeonFloor++)
            {
                for (int nDungeonRow = 0; nDungeonRow < N_DUNGEON_ROWS_PER_MAP; nDungeonRow++)
                {
                    for (int nDungeonCol = 0; nDungeonCol < N_DUNGEON_TILE_PER_ROW; nDungeonCol++)
                    {
                        byte tileByte = rawData[nDungeonRow * N_DUNGEON_TILE_PER_ROW + nDungeonCol];
                        var dungeonTile = new DungeonTile(new Point2D(nDungeonCol, nDungeonRow),
                            (byte)((tileByte >> 4) & 0xF), (byte)(tileByte & 0xF));
                        _tileData[nDungeonFloor, nDungeonCol, nDungeonRow] = dungeonTile;
                    }
                }
            }
        }
    }
}