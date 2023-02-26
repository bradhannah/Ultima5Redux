using System;
using System.Collections.Generic;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class SingleDungeonMapFloorReference
    {
        public const int N_BYTES_PER_FLOOR = N_DUNGEON_ROWS_PER_MAP * N_DUNGEON_COLS_PER_ROW;
        public const int N_DUNGEON_COLS_PER_ROW = 8;

        public const int N_DUNGEON_ROWS_PER_MAP = 8;

        private readonly DungeonTile[,] _tileData =
            new DungeonTile[N_DUNGEON_ROWS_PER_MAP, N_DUNGEON_COLS_PER_ROW];

        public int DungeonFloor { get; }
        public SmallMapReferences.SingleMapReference.Location DungeonLocation { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        public SmallMapReferences.SingleMapReference SingleMapReference { get; }

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

        public byte[][] GetDefaultDungeonMap()
        {
            byte[][] mapArray = Utils.Init2DByteArray(8, 8);
            for (int nCol = 0; nCol < N_DUNGEON_COLS_PER_ROW; nCol++)
            {
                for (int nRow = 0; nRow < N_DUNGEON_ROWS_PER_MAP; nRow++)
                {
                    TileReference.SpriteIndex spriteIndex = _tileData[nCol, nRow].TheTileType switch
                    {
                        DungeonTile.TileType.Nothing => TileReference.SpriteIndex.Grass,
                        DungeonTile.TileType.LadderUp => TileReference.SpriteIndex.LadderUp,
                        DungeonTile.TileType.LadderDown => TileReference.SpriteIndex.LadderDown,
                        DungeonTile.TileType.LadderUpDown => TileReference.SpriteIndex.LadderUp,
                        DungeonTile.TileType.Chest => TileReference.SpriteIndex.Chest,
                        DungeonTile.TileType.Fountain => TileReference.SpriteIndex.Fountain_KeyIndex,
                        DungeonTile.TileType.Trap => TileReference.SpriteIndex.Swamp,
                        DungeonTile.TileType.OpenChest => TileReference.SpriteIndex.Chest,
                        DungeonTile.TileType.MagicField => TileReference.SpriteIndex.FireField,
                        DungeonTile.TileType.RoomsBroke => TileReference.SpriteIndex.Clock1,
                        DungeonTile.TileType.Wall => TileReference.SpriteIndex.StoneBrickWall,
                        DungeonTile.TileType.SecondaryWall => TileReference.SpriteIndex.LargeRockWall,
                        DungeonTile.TileType.SecretDoor => TileReference.SpriteIndex.MagicLockDoor,
                        DungeonTile.TileType.NormalDoor => TileReference.SpriteIndex.RegularDoor,
                        DungeonTile.TileType.Room => TileReference.SpriteIndex.Nothing,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    mapArray[nCol][nRow] = (byte)spriteIndex;
                }
            }

            return mapArray;
        }

        public DungeonTile GetDungeonTile(in Point2D xy) => _tileData[xy.X, xy.Y];
    }
}