using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ultima5Redux.References.Maps
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class DungeonTile
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum ChestType { Normal = 0, Trapped_1 = 1, Trapped_2 = 2, Poisoned = 4 }

        //char fountStr[3][7] = { "Heal", "Poison", "Hurt" };
        public enum FountainType { CurePoison = 0, Heal = 1, PoisonFountain = 2, BadTasteDamage = 3 }

        public enum LadderTrap { NoTrap = 0, IsTrapped = 8 }

        //char fieldStr[4][10] = { "Sleep", "Poison", "Fire", "Lightning" };
        public enum MagicFieldType { Poison = 0, Sleep = 1, Fire = 2, Energy = 3 }

        public enum TileType
        {
            Nothing = 0, LadderUp = 1, LadderDown = 2, LadderUpDown = 3, Chest = 4, Fountain = 5, Trap = 6,
            OpenChest = 7, MagicField = 8, RoomsBroke = 0xA, Wall = 0xB,

            // secondary wall is the kind with skeletons on
            SecondaryWall = 0xC, SecretDoor = 0xD, NormalDoor = 0xE, Room = 0xF
        }

        public enum TrapType { LowerTrapVisible, BombTrap, InvisibleTrap, UpperTrapVisible }

        private readonly int[] _messageStarts = { 0, 1, -1, 2, 3, 7, 10, -1 };

        private readonly byte _subTileType;

        private readonly List<string> _messages = new()
        {
            "BOTTOMLESS PIT", "THE MAZE OF LOST SOULS",
            // floor 0 Wrong
            "THE PRISON WRONG", "THE CRYPT", "UPPER CRYPTS", "LOWER CRYPTS",
            "DEBTORS ALLY", "DEEP", "DEEPER", "DEEPEST", "MOTHER LODE MAZE"
        };

        public int RoomNumber { get; }
        public ChestType TheChestType { get; }
        public FountainType TheFountainType { get; }
        public LadderTrap TheLadderTrap { get; }
        public MagicFieldType TheMagicFieldType { get; }
        public TileType TheTileType { get; }
        public TrapType TheTrapType { get; }

        public Point2D TilePosition { get; }


        public string WallText
        {
            get
            {
                if (!_messageStarts.Contains(_subTileType)) return "";

                for (int i = 0; i < _messageStarts.Length; i++)
                    if (_messageStarts[i] == _subTileType)
                        return _messages[i];

                return "";
            }
        }

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