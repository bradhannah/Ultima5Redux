using System;
using System.Diagnostics;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    public abstract class MapUnit
    {
        private readonly TileReferences _tileReferences;

        #region Private and Internal Properties 
        /// <summary>
        /// All the movements for the map character
        /// </summary>
        internal MapUnitMovement Movement { get; private protected set; }
        
        /// <summary>
        /// the state of the animations
        /// </summary>
        public MapUnitState TheMapUnitState { get; protected set; }

        /// <summary>
        /// The location state of the character
        /// </summary>
        internal SmallMapCharacterState TheSmallMapCharacterState { get; }

        private readonly MapUnitPosition _mapMapUnitPosition = new MapUnitPosition();
        
        /// <summary>
        /// Gets the TileReference of the keyframe of the particular MapUnit (typically the first frame)
        /// </summary>
        public TileReference KeyTileReference
        {
            get
            {
                if (NPCRef != null) return _tileReferences.GetTileReference(NPCRef.NPCKeySprite);

                return _tileReferences.GetTileReferenceOfKeyIndex(TheMapUnitState.Tile1Ref.Index);
            }
        }

        /// <summary>
        /// The characters current position on the map
        /// </summary>
        internal MapUnitPosition MapUnitPosition
        {
            get => _mapMapUnitPosition;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                // this is a bit redundant but we have a backing field and also store the XY positions
                // in the TheMapUnitState and TheSmallMapCharacterState, but we have to do this because the .XY
                // of the MapUnitPosition is often edited directly
                _mapMapUnitPosition.X = value.X;
                _mapMapUnitPosition.Y = value.Y;
                _mapMapUnitPosition.Floor = value.Floor;
                
                TheMapUnitState.X = (byte) value.X;
                TheMapUnitState.Y = (byte) value.Y;
                TheMapUnitState.Floor = (byte) value.Floor;

                if (TheSmallMapCharacterState == null) return;
                TheSmallMapCharacterState.TheMapUnitPosition.X = value.X;
                TheSmallMapCharacterState.TheMapUnitPosition.Y = value.Y;
                TheSmallMapCharacterState.TheMapUnitPosition.Floor = value.Floor;
            }
        }

        /// <summary>
        /// How many iterations will I force the character to wander?
        /// </summary>
        internal int ForcedWandering { get; set; }
        
        public bool ArrivedAtLocation { get; private set; }
        
        private int _scheduleIndex = -1;
        #endregion

        #region Public Properties
        public int MovementAttempts = 0;
        /// <summary>
        /// Reference to current NPC (if it's an NPC at all!)
        /// </summary>
        public NonPlayerCharacterReference NPCRef { get; }

        public SmallMapReferences.SingleMapReference.Location MapLocation { get; }

        /// <summary>
        /// Is the character currently active on the map?
        /// </summary>
        public bool IsInParty
        {
            get; set; 
        }

        /// <summary>
        /// Is the map character currently an active character on the current map
        /// </summary>
        public abstract bool IsActive { get; }

        public string GetDebugDescription(TimeOfDay timeOfDay)
        {
            if (NPCRef != null)
            {
                return ("Name=" + NPCRef.FriendlyName
                       + " " + MapUnitPosition + " Scheduled to be at: " +
                       NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay) +
                       " with AI Mode: " +
                       NPCRef.Schedule.GetCharacterAiTypeByTime(timeOfDay) +
                       " <b>Movement Attempts</b>: " + MovementAttempts + " " +
                       this.Movement);
            }
            
            return ("MapUnit "+ KeyTileReference.Description  
                    + " " + MapUnitPosition + " Scheduled to be at: " 
                    + " <b>Movement Attempts</b>: " + MovementAttempts + " "
                    + this.Movement);
        }

        #endregion

        #region Constructors
        /// <summary>
        /// empty constructor if there is nothing in the map character slot
        /// </summary>
        protected MapUnit()
        {
            NPCRef = null;
            TheMapUnitState = null;
            TheSmallMapCharacterState = null;
            Movement = null;
        }

        /// <summary>
        /// Builds a MpaCharacter from pre-instantiated objects - typically loaded from disk in advance
        /// </summary>
        /// <param name="npcRef"></param>
        /// <param name="mapUnitState"></param>
        /// <param name="smallMapTheSmallMapCharacterState"></param>
        /// <param name="mapUnitMovement"></param>
        /// <param name="timeOfDay"></param>
        /// <param name="playerCharacterRecords"></param>
        /// <param name="bLoadedFromDisk"></param>
        /// <param name="tileReferences"></param>
        /// <param name="location"></param>
        protected MapUnit(NonPlayerCharacterReference npcRef, MapUnitState mapUnitState, SmallMapCharacterState smallMapTheSmallMapCharacterState,
            MapUnitMovement mapUnitMovement, TimeOfDay timeOfDay, PlayerCharacterRecords playerCharacterRecords, bool bLoadedFromDisk,
            TileReferences tileReferences, SmallMapReferences.SingleMapReference.Location location)
        {
            _tileReferences = tileReferences;
            MapLocation = location;
            NPCRef = npcRef;
            TheMapUnitState = mapUnitState;
            TheSmallMapCharacterState = smallMapTheSmallMapCharacterState;
            Movement = mapUnitMovement;

            PlayerCharacterRecord record = null;

            bool bLargeMap = TheSmallMapCharacterState == null && npcRef == null;
            
            // Debug.Assert(playerCharacterRecords != null);
            Debug.Assert(TheMapUnitState != null);
            Debug.Assert(Movement != null);
            
            // gets the player character record for an NPC if one exists
            // this is commonly used when meeting NPCs who have not yet joined your party 
            if (npcRef != null)
            {
                record = playerCharacterRecords.GetCharacterRecordByNPC(npcRef);
            }

            // is the NPC you are loading currently in the party?
            IsInParty = record != null && record.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InParty;

            // set the characters position 
            MapUnitPosition = new MapUnitPosition(TheMapUnitState.X, TheMapUnitState.Y, TheMapUnitState.Floor);
            
            //MapUnitPosition.Floor = TheMapUnitState.Floor;
            // MapUnitPosition.Floor = TheSmallMapCharacterState?.TheMapUnitPosition.Floor ?? 0;

            // it's a large map so we follow different logic to determine the placement of the character
            if (bLargeMap)
            {
                Move(MapUnitPosition, null, true);
            }
            else
            {
                // there is no TheSmallMapCharacterState which indicates that it is a large map
                if (!bLoadedFromDisk)
                {
                    if (npcRef != null)
                    {
                        MoveNPCToDefaultScheduledPosition(timeOfDay);
                    }
                }
                else
                {
                    Move(MapUnitPosition, timeOfDay, false);
                }
            }
        }

        /// <summary>
        /// Creates an Avatar MapUnit at the default small map position
        /// Note: this should never need to be called from a LargeMap since the values persist on disk
        /// </summary>
        /// <param name="tileReferences"></param>
        /// <param name="location"></param>
        /// <param name="movement"></param>
        /// <returns></returns>
        public static MapUnit CreateAvatar(TileReferences tileReferences, 
            SmallMapReferences.SingleMapReference.Location location, MapUnitMovement movement)
        {
            Avatar theAvatar = new Avatar(tileReferences, SmallMapReferences.GetStartingXYZByLocation(location), 
                location, movement);

            return theAvatar;
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Moves the NPC to the appropriate floor and location based on the their expected location and position
        /// </summary>
        internal void MoveNPCToDefaultScheduledPosition(TimeOfDay tod)
        {
            MapUnitPosition npcXy = NPCRef.Schedule.GetCharacterDefaultPositionByTime(tod);

            // the NPC is a non-NPC, so we keep looking
            if (npcXy.X == 0 && npcXy.Y == 0) return;

            Move(npcXy, tod, false);
        }

        /// <summary>
        /// move the character to a new position
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="nFloor"></param>
        /// <param name="tod"></param>
        internal void Move(Point2D xy, int nFloor, TimeOfDay tod)
        {
            MapUnitPosition.XY = xy;
            MapUnitPosition.Floor = nFloor;

            UpdateScheduleTracking(tod);
        }

        /// <summary>
        /// Move the character to a new position
        /// </summary>
        /// <param name="mapUnitPosition"></param>
        /// <param name="tod"></param>
        /// <param name="bIsLargeMap"></param>
        internal void Move(MapUnitPosition mapUnitPosition, TimeOfDay tod, bool bIsLargeMap)
        {
            MapUnitPosition = mapUnitPosition;
            if (!bIsLargeMap)
            {
                UpdateScheduleTracking(tod);
            }
        }

        private void UpdateScheduleTracking(TimeOfDay tod)
        {
            if (MapUnitPosition == NPCRef.Schedule.GetCharacterDefaultPositionByTime(tod))
            {
                ArrivedAtLocation = true;
            }

            int nCurrentScheduleIndex = NPCRef.Schedule.GetScheduleIndex(tod);
            // it's the first time, so we don't reset the ArrivedAtLocation flag 
            if (_scheduleIndex == -1)
            {
                _scheduleIndex = nCurrentScheduleIndex;
            }
            else if (_scheduleIndex != nCurrentScheduleIndex)
            {
                _scheduleIndex = nCurrentScheduleIndex;
                ArrivedAtLocation = false;
            }
        }

      
        #endregion
    }
}
