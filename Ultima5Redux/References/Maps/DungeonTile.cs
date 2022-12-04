using System;
using System.Collections.Generic;
using System.Linq;

namespace Ultima5Redux.References
{
    public class DungeonTile
    {
        public enum TileType
        {
            Nothing = 0, LadderUp = 1, LadderDown = 2, LadderUpDown = 3, Chest = 4, Fountain = 5, Trap = 6,
            OpenChest = 7, MagicField = 8, RoomsBroke = 0xA, Wall = 0xB,

            // secondary wall is the kind with skeletons on
            SecondaryWall = 0xC, SecretDoor = 0xD,
            NormalDoor = 0xE, Room = 0xF
        }

        public enum LadderTrap { NoTrap = 0, IsTrapped = 8 }

        public enum ChestType { Normal = 0, Trapped_1 = 1, Trapped_2 = 2, Poisoned = 4 }

        //char fountStr[3][7] = { "Heal", "Poison", "Hurt" };
        public enum FountainType { CurePoison = 0, Heal = 1, PoisonFountain = 2, BadTasteDamage = 3 }

        public enum TrapType { LowerTrapVisible, BombTrap, InvisibleTrap, UpperTrapVisible }

        //char fieldStr[4][10] = { "Sleep", "Poison", "Fire", "Lightning" };
        public enum MagicFieldType { Poison = 0, Sleep = 1, Fire = 2, Energy = 3 }

        private readonly int[] messageStarts = { 0, 1, -1, 2, 3, 7, 10, -1 };

        private readonly List<string> messages = new()
        {
            "BOTTOMLESS PIT", "THE MAZE OF LOST SOULS",
            // floor 0 Wrong
            "THE PRISON WRONG", "THE CRYPT", "UPPER CRYPTS", "LOWER CRYPTS",
            "DEBTORS ALLY", "DEEP", "DEEPER", "DEEPEST", "MOTHER LODE MAZE"
        };


        public string WallText
        {
            get
            {
                if (messageStarts.Contains(_subTileType))
                {
                    for (int i = 0; i < messageStarts.Length; i++)
                        if (messageStarts[i] == _subTileType)
                            return messages[i];
                }

                return "";
            }
        }

        public Point2D TilePosition { get; }
        public TileType TheTileType { get; }
        public LadderTrap TheLadderTrap { get; }
        public ChestType TheChestType { get; }
        public FountainType TheFountainType { get; }
        public TrapType TheTrapType { get; }
        public MagicFieldType TheMagicFieldType { get; }

        public int RoomNumber { get; }

        private byte _subTileType;

        public DungeonTile(Point2D tilePosition, byte typeByte, byte subTypeByte)
        {
            TilePosition = tilePosition;
            TheTileType = (TileType)typeByte;
            _subTileType = subTypeByte;

            switch (TheTileType)
            {
                case TileType.Nothing:
                    // we cool
                    if (subTypeByte == 0x8) TheTileType = TileType.LadderUp;
                        
                    break;
                case TileType.LadderUp:
                case TileType.LadderDown:
                case TileType.LadderUpDown:
                    TheLadderTrap = (LadderTrap)subTypeByte;
                    break;
                case TileType.Chest:
                case TileType.OpenChest:
                    TheChestType = (ChestType)subTypeByte;
                    break;
                case TileType.Fountain:
                    TheFountainType = (FountainType)subTypeByte;
                    break;
                case TileType.Trap:
                    TheTrapType = (TrapType)subTypeByte;
                    break;
                case TileType.MagicField:
                    TheMagicFieldType = (MagicFieldType)subTypeByte;
                    break;
                case TileType.RoomsBroke:
                    break;
                case TileType.Wall:
                    break;
                case TileType.SecondaryWall:
                    break;
                case TileType.SecretDoor:
                    break;
                case TileType.NormalDoor:
                    break;
                case TileType.Room:
                    RoomNumber = subTypeByte;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}