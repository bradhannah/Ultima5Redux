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
                    TileReference.SpriteIndex spriteIndex;
                    switch (_tileData[nCol, nRow].TheTileType)
                    {
                        case DungeonTile.TileType.Nothing:
                            spriteIndex = TileReference.SpriteIndex.Grass;
                            break;
                        case DungeonTile.TileType.LadderUp:
                            spriteIndex = TileReference.SpriteIndex.LadderUp;
                            break;
                        case DungeonTile.TileType.LadderDown:
                            spriteIndex = TileReference.SpriteIndex.LadderDown;
                            break;
                        case DungeonTile.TileType.LadderUpDown:
                            spriteIndex = TileReference.SpriteIndex.LadderUp;
                            break;
                        case DungeonTile.TileType.Chest:
                            spriteIndex = TileReference.SpriteIndex.Chest;
                            break;
                        case DungeonTile.TileType.Fountain:
                            spriteIndex = TileReference.SpriteIndex.Fountain_KeyIndex;
                            break;
                        case DungeonTile.TileType.Trap:
                            spriteIndex = TileReference.SpriteIndex.Swamp;
                            break;
                        case DungeonTile.TileType.OpenChest:
                            spriteIndex = TileReference.SpriteIndex.Chest;
                            break;
                        case DungeonTile.TileType.MagicField:
                            spriteIndex = TileReference.SpriteIndex.FireField;
                            break;
                        case DungeonTile.TileType.RoomsBroke:
                            spriteIndex = TileReference.SpriteIndex.Clock1;
                            break;
                        case DungeonTile.TileType.Wall:
                            spriteIndex = TileReference.SpriteIndex.StoneBrickWall;
                            break;
                        case DungeonTile.TileType.SecondaryWall:
                            spriteIndex = TileReference.SpriteIndex.LargeRockWall;
                            break;
                        case DungeonTile.TileType.SecretDoor:
                            spriteIndex = TileReference.SpriteIndex.MagicLockDoor;
                            break;
                        case DungeonTile.TileType.NormalDoor:
                            spriteIndex = TileReference.SpriteIndex.RegularDoor;
                            break;
                        case DungeonTile.TileType.Room:
                            spriteIndex = TileReference.SpriteIndex.Nothing;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    mapArray[nCol][nRow] = (byte)spriteIndex;
                }
            }

            return mapArray;
        }

        public DungeonTile GetDungeonTile(in Point2D xy) => _tileData[xy.X, xy.Y];
    }
}