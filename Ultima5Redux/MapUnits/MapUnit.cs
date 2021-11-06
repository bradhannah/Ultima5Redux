using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.External;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.Monsters;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    [DataContract]
    public abstract class MapUnitDetails
    {
        [DataMember] protected internal int MovementAttempts { get; set; }
        /// <summary>
        ///     How many iterations will I force the character to wander?
        /// </summary>
        [DataMember] internal int ForcedWandering { get; set; }
        /// <summary>
        ///     All the movements for the map character
        /// </summary>
        [DataMember] internal MapUnitMovement Movement { get; private protected set; }
        /// <summary>
        ///     The location state of the character
        /// </summary>
        [DataMember] protected internal SmallMapCharacterState TheSmallMapCharacterState { get; set; }

        /// <summary>
        ///     Is the map character currently an active character on the current map
        /// </summary>
        [DataMember] public abstract bool IsActive { get; }
        [DataMember] public abstract bool IsAttackable { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        [DataMember] public bool IsOccupiedByAvatar { get; protected internal set; }
        [DataMember] public bool UseFourDirections { get; set; } = false;
        [DataMember] public Point2D.Direction Direction { get; set; }
        [DataMember] public SmallMapReferences.SingleMapReference.Location MapLocation { get; set; }
        [DataMember] public virtual MapUnitPosition MapUnitPosition { get; internal set; }
        [DataMember] public abstract Avatar.AvatarState BoardedAvatarState { get; }
        [DataMember] public abstract string BoardXitName { get; }
        [DataMember] public abstract string FriendlyName { get; }
        /// <summary>
        ///     Is the character currently active on the map?
        /// </summary>
        [DataMember] protected internal bool IsInParty { get; set; }
    }

    // [DataContract]
    // public sealed class MapUnitSave : MapUnitDetails
    // {
    //     public enum MapUnitType
    //     {
    //         Avatar, CombatPlayer, Enemy, EmptyMapUnit, Horse, MagicCarpet, NonPlayerCharacter, Frigate, Skiff
    //     }
    //     
    //     [DataMember] public int TileReferenceIndex { get; }
    //     [DataMember] public int NPCRefIndex { get; }
    //     [DataMember] public MapUnitType TheMapUnitType { get; }
    //
    //     [DataMember] public override bool IsActive { get; }
    //     [DataMember] public override bool IsAttackable { get; }
    //     [DataMember] public override Avatar.AvatarState BoardedAvatarState { get; }
    //     [DataMember] public override string BoardXitName { get; }
    //     [DataMember] public override string FriendlyName { get; }
    //
    //     public MapUnitSave(MapUnit mapUnit)
    //     {
    //         MovementAttempts = mapUnit.MovementAttempts;
    //         ForcedWandering = mapUnit.ForcedWandering;
    //         Movement = mapUnit.Movement;
    //         TheSmallMapCharacterState = mapUnit.TheSmallMapCharacterState;
    //         IsActive = mapUnit.IsActive;
    //         IsAttackable = mapUnit.IsAttackable;
    //         IsOccupiedByAvatar = mapUnit.IsOccupiedByAvatar;
    //         UseFourDirections = mapUnit.UseFourDirections;
    //         Direction = mapUnit.Direction;
    //         MapLocation = mapUnit.MapLocation;
    //         MapUnitPosition = mapUnit.MapUnitPosition;
    //         BoardedAvatarState = mapUnit.BoardedAvatarState;
    //         BoardXitName = mapUnit.BoardXitName;
    //         FriendlyName = mapUnit.FriendlyName;
    //         IsInParty = mapUnit.IsInParty;
    //
    //         TileReferenceIndex = mapUnit.KeyTileReference.Index;
    //         NPCRefIndex = mapUnit?.NPCRef.DialogIndex ?? -1;
    //         TheMapUnitType = GetMapUnitTypeFromMapUnit(mapUnit);
    //     }
    //
    //     public MapUnit CreateMapUnit(TileReferences tileReferences, DataOvlReference dataOvlReference,
    //         EnemyReferences enemyReferences, NonPlayerCharacterReferences npcRefs,
    //         TimeOfDay timeOfDay, PlayerCharacterRecords records, 
    //         bool bUseExtendedSprites = true)
    //     {
    //         MapUnitState getQuickMapUnitState() =>
    //             MapUnitState.CreateMapUnitState(tileReferences, MapUnitPosition, TileReferenceIndex);
    //         
    //         switch (TheMapUnitType)
    //         {
    //             case MapUnitType.Avatar:
    //                 return Avatar.CreateAvatar(tileReferences, MapLocation, Movement, null, dataOvlReference, bUseExtendedSprites);
    //             case MapUnitType.CombatPlayer:
    //                 throw new Ultima5ReduxException("Can't restore a combat player from a save file");
    //             case MapUnitType.Enemy:
    //                 return new Enemy(getQuickMapUnitState(), Movement, tileReferences, 
    //                     enemyReferences.GetEnemyReference(TileReferenceIndex), MapLocation, dataOvlReference);
    //             case MapUnitType.EmptyMapUnit:
    //                 return new EmptyMapUnit();
    //             case MapUnitType.Horse:
    //                 return new Horse(getQuickMapUnitState(), Movement, tileReferences, MapLocation, 
    //                     dataOvlReference, Direction);
    //             case MapUnitType.MagicCarpet:
    //                 return new MagicCarpet(
    //                     MapUnitState.CreateMapUnitState(tileReferences, MapUnitPosition, TileReferenceIndex),
    //                     Movement, tileReferences, MapLocation, dataOvlReference, Direction);
    //             case MapUnitType.NonPlayerCharacter:
    //                 return new NonPlayerCharacter(npcRefs.NPCs[NPCRefIndex], getQuickMapUnitState(),
    //                     TheSmallMapCharacterState,
    //                     Movement, timeOfDay, records, false, tileReferences, MapLocation, dataOvlReference);
    //             case MapUnitType.Frigate:
    //                 return new Frigate(getQuickMapUnitState(), Movement, tileReferences, MapLocation, dataOvlReference,
    //                     Direction);
    //             case MapUnitType.Skiff:
    //                 return new Skiff(getQuickMapUnitState(), Movement, tileReferences, MapLocation, dataOvlReference,
    //                     Direction);
    //             default:
    //                 throw new ArgumentOutOfRangeException();
    //         }
    //     }
    //
    //     private MapUnitType GetMapUnitTypeFromMapUnit(MapUnit mapUnit)
    //     {
    //         return mapUnit switch
    //         {
    //             Avatar _ => MapUnitType.Avatar,
    //             EmptyMapUnit _ => MapUnitType.EmptyMapUnit,
    //             Horse _ => MapUnitType.Horse,
    //             MagicCarpet _ => MapUnitType.MagicCarpet,
    //             NonPlayerCharacter _ => MapUnitType.NonPlayerCharacter,
    //             CombatPlayer _ => MapUnitType.CombatPlayer,
    //             Enemy _ => MapUnitType.Enemy,
    //             _ => throw new Ultima5ReduxException("Tried to GetMapUnitType from " + mapUnit.GetType().ToString())
    //         };
    //     }
    //     
    // }
    
    public abstract class MapUnit : MapUnitDetails
    {
        private readonly NonPlayerCharacterReferences _npcRefs;
        [IgnoreDataMember] private readonly MapUnitPosition _mapMapUnitPosition = new MapUnitPosition();

        /// <summary>
        ///     The characters current position on the map
        /// </summary>
        [DataMember] public override MapUnitPosition MapUnitPosition
        {
            get => _mapMapUnitPosition;
            internal set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                // this is a bit redundant but we have a backing field and also store the XY positions
                // in the TheMapUnitState and TheSmallMapCharacterState, but we have to do this because the .XY
                // of the MapUnitPosition is often edited directly
                _mapMapUnitPosition.X = value.X;
                _mapMapUnitPosition.Y = value.Y;
                _mapMapUnitPosition.Floor = value.Floor;

                TheMapUnitState.X = (byte)value.X;
                TheMapUnitState.Y = (byte)value.Y;
                TheMapUnitState.Floor = (byte)value.Floor;

                if (TheSmallMapCharacterState == null) return;
                TheSmallMapCharacterState.TheMapUnitPosition.X = value.X;
                TheSmallMapCharacterState.TheMapUnitPosition.Y = value.Y;
                TheSmallMapCharacterState.TheMapUnitPosition.Floor = value.Floor;
            }
        }

        /// <summary>
        ///     the state of the animations
        /// </summary>
        [IgnoreDataMember] public MapUnitState TheMapUnitState { get; protected set; }


        [IgnoreDataMember] public TileReference BoardedTileReference =>
            TileReferences.GetTileReferenceByName(UseFourDirections
                ? FourDirectionToTileNameBoarded[Direction]
                : DirectionToTileNameBoarded[Direction]);

        // [IgnoreDataMember] public virtual TileReference KeyTileReference
        // {
        //     get => TileReferences.GetTileReferenceOfKeyIndex(TheMapUnitState.Tile1Ref.Index);
        //     set
        //     {
        //         TheMapUnitState.Tile1Ref = value;
        //         TheMapUnitState.Tile2Ref = value;
        //     }
        // }

        // ReSharper disable once MemberCanBeProtected.Global
        [IgnoreDataMember] public virtual TileReference NonBoardedTileReference =>
            TileReferences.GetTileReferenceByName(DirectionToTileName[Direction]);


        [IgnoreDataMember] protected abstract Dictionary<Point2D.Direction, string> DirectionToTileName { get; }
        [IgnoreDataMember] protected abstract Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; }
        [IgnoreDataMember] protected virtual Dictionary<Point2D.Direction, string> FourDirectionToTileNameBoarded =>
            DirectionToTileNameBoarded;

        [IgnoreDataMember] protected DataOvlReference DataOvlRef { get; set; }
        [IgnoreDataMember] protected TileReferences TileReferences { get; set; }
        [IgnoreDataMember] protected NonPlayerCharacterReferences NPRefs { get; set; }

        [DataMember] private int _keyTileIndex = -1;
        [DataMember] private int _npcRefIndex = -1;
        
        [IgnoreDataMember]
        public NonPlayerCharacterReference NPCRef
        {
            get
            {
                if (_npcRefs == null || _npcRefIndex == -1) return null;
                return _npcRefs?.GetNonPlayerCharactersByLocation(MapLocation)[_npcRefIndex];
            }
            private set
            {
                if (value == null) 
                    _npcRefIndex = -1;
                else
                    _npcRefIndex = value.DialogIndex;
            }
        }

        [IgnoreDataMember]
        public virtual TileReference KeyTileReference
        {
            get => NPCRef == null
                ? TileReferences.GetTileReferenceOfKeyIndex(TheMapUnitState.Tile1Ref.Index)
                : TileReferences.GetTileReference(NPCRef.NPCKeySprite);
            set
            {
                TheMapUnitState.Tile1Ref = value;
                TheMapUnitState.Tile2Ref = value;
            }
        }

        /// <summary>
        /// empty constructor if there is nothing in the map character slot
        /// </summary>
        protected MapUnit()
        {
            TheMapUnitState = null;
            TheSmallMapCharacterState = null;
            Movement = null;
            Direction = Point2D.Direction.None;
        }

        /// <summary>
        ///     Builds a MpaCharacter from pre-instantiated objects - typically loaded from disk in advance
        /// </summary>
        /// <param name="npcRef"></param>
        /// <param name="mapUnitState"></param>
        /// <param name="smallMapTheSmallMapCharacterState"></param>
        /// <param name="mapUnitMovement"></param>
        /// <param name="tileReferences"></param>
        /// <param name="location"></param>
        /// <param name="dataOvlRef"></param>
        /// <param name="direction"></param>
        /// <param name="npcRefs"></param>
        protected MapUnit(MapUnitState mapUnitState,
            SmallMapCharacterState smallMapTheSmallMapCharacterState,
            MapUnitMovement mapUnitMovement, 
            TileReferences tileReferences, SmallMapReferences.SingleMapReference.Location location,
            DataOvlReference dataOvlRef, Point2D.Direction direction,
            NonPlayerCharacterReference npcRef = null, NonPlayerCharacterReferences npcRefs = null)
        {
            _npcRefs = npcRefs;
            DataOvlRef = dataOvlRef;
            TileReferences = tileReferences;
            MapLocation = location;
            TheMapUnitState = mapUnitState;
            TheSmallMapCharacterState = smallMapTheSmallMapCharacterState;
            Movement = mapUnitMovement;
            Direction = direction;

            _npcRefIndex = npcRef?.DialogIndex ?? -1;

            // Debug.Assert(playerCharacterRecords != null);
            Debug.Assert(TheMapUnitState != null);
            Debug.Assert(Movement != null);

            _keyTileIndex = mapUnitState.Tile1Ref.Index;

            // set the characters position 
            MapUnitPosition = new MapUnitPosition(TheMapUnitState.X, TheMapUnitState.Y, TheMapUnitState.Floor);
        }        
        
        public virtual void CompleteNextMove(VirtualMap virtualMap, TimeOfDay timeOfDay, AStar aStar)
        {
            // by default the thing doesn't move on it's own
        }

        public virtual bool CanBeExited(VirtualMap virtualMap)
        {
            return true;
        }

        // ReSharper disable once UnusedMember.Global
        public virtual string GetDebugDescription(TimeOfDay timeOfDay)
        {
            return "MapUnit " + KeyTileReference.Description
                              + " " + MapUnitPosition + " Scheduled to be at: "
                              + " <b>Movement Attempts</b>: " + MovementAttempts + " "
                              + Movement;
        }

        /// <summary>
        ///     move the character to a new position
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="nFloor"></param>
        protected void Move(Point2D xy, int nFloor)
        {
            MapUnitPosition.XY = xy;
            MapUnitPosition.Floor = nFloor;
        }

        /// <summary>
        ///     Move the character to a new position
        /// </summary>
        /// <param name="mapUnitPosition"></param>
        protected void Move(MapUnitPosition mapUnitPosition)
        {
            MapUnitPosition = mapUnitPosition;
        }

        /// <summary>
        ///     Builds the actual path for the character to travel based on their current position and their target position
        /// </summary>
        /// <param name="currentMap"></param>
        /// <param name="mapUnit">where the character is presently</param>
        /// <param name="targetXy">where you want them to go</param>
        /// <returns>returns true if a path was found, false if it wasn't</returns>
        protected static bool BuildPath(Map currentMap, MapUnit mapUnit, Point2D targetXy, AStar aStar)
        {
            if (mapUnit.MapUnitPosition.XY == targetXy)
                throw new Ultima5ReduxException("Asked to build a path, but " + mapUnit.FriendlyName +
                                                " is already at " + targetXy.X + "," +
                                                targetXy.Y);

            // todo: need some code that checks for different floors and directs them to closest ladder or staircase instead of same floor position

            Stack<Node> nodeStack = aStar.FindPath(mapUnit.MapUnitPosition.XY, targetXy);

            MapUnitMovement.MovementCommandDirection prevDirection = MapUnitMovement.MovementCommandDirection.None;
            MapUnitMovement.MovementCommandDirection newDirection = MapUnitMovement.MovementCommandDirection.None;
            Point2D prevPosition = mapUnit.MapUnitPosition.XY;

            // temporary while I figure out why this happens
            if (nodeStack == null) return false;

            int nInARow = 0;
            // builds the movement list that is compatible with the original U5 movement instruction queue stored in the state file
            foreach (Node node in nodeStack)
            {
                //Point2D newPosition = Vector2ToPoint2D(node.Position);
                newDirection = GetCommandDirection(prevPosition, node.Position);

                // if the previous direction is the same as the current direction, then we keep track so that we can issue a single instruction
                // that has N iterations (ie. move East 5 times)
                if (prevDirection == newDirection || prevDirection == MapUnitMovement.MovementCommandDirection.None)
                {
                    nInARow++;
                }
                else
                {
                    // if the direction has changed then we add the previous direction and reset the concurrent counter
                    mapUnit.Movement.AddNewMovementInstruction(
                        new MapUnitMovement.MovementCommand(prevDirection, nInARow));
                    nInARow = 1;
                }

                prevDirection = newDirection;
                prevPosition = node.Position;
            }

            if (nInARow > 0)
                mapUnit.Movement.AddNewMovementInstruction(new MapUnitMovement.MovementCommand(newDirection, nInARow));
            return true;
        }

        private static Point2D Vector2ToPoint2D(Vector2 vector) => new Point2D((int)vector.X, (int)vector.Y);

        private static MapUnitMovement.MovementCommandDirection GetCommandDirection(Point2D fromXy, Point2D toXy)
        {
            if (fromXy == toXy) return MapUnitMovement.MovementCommandDirection.None;
            if (fromXy.X < toXy.X) return MapUnitMovement.MovementCommandDirection.East;
            if (fromXy.Y < toXy.Y) return MapUnitMovement.MovementCommandDirection.South;
            if (fromXy.X > toXy.X) return MapUnitMovement.MovementCommandDirection.West;
            if (fromXy.Y > toXy.Y) return MapUnitMovement.MovementCommandDirection.North;
            throw new Ultima5ReduxException(
                "For some reason we couldn't determine the path of the command direction in getCommandDirection");
        }
    }
}