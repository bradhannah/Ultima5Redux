using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    [DataContract] public sealed class NonPlayerCharacter : MapUnit
    {
        [DataMember(Name = "PlayerCharacterRecordIndex")]
        private int _playerCharacterRecordIndex;

        [IgnoreDataMember] public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;

        [IgnoreDataMember] public override string BoardXitName => "Board them? You hardly know them!";

        [IgnoreDataMember] public override string FriendlyName => NPCRef.FriendlyName;

        /// <summary>
        ///     Is the map character currently an active character on the current map
        /// </summary>
        [IgnoreDataMember]
        public override bool IsActive
        {
            get
            {
                // if they are in our party then we don't include them in the map 
                if (IsInParty) return false;

                if (NPCState.IsDead) return false;

                // if they are in 0,0 then I am certain they are not real
                if (MapUnitPosition.X == 0 && MapUnitPosition.Y == 0) return false;

                // if there is a small map character state then we prefer to use it to determine if the 
                // unit is active
                Debug.Assert(TheSmallMapCharacterState != null);

                return true;
            }
        }

        [IgnoreDataMember] public override bool IsAttackable => true;

        [IgnoreDataMember]
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } = new();

        [IgnoreDataMember]
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } =
            new();

        [JsonConstructor] public NonPlayerCharacter()
        {
        }

        public NonPlayerCharacter(SmallMapCharacterState smallMapTheSmallMapCharacterState,
            MapUnitMovement mapUnitMovement, bool bLoadedFromDisk,
            SmallMapReferences.SingleMapReference.Location location, MapUnitPosition mapUnitPosition,
            NonPlayerCharacterState npcState) : base(smallMapTheSmallMapCharacterState, mapUnitMovement, location,
            Point2D.Direction.None, npcState,
            GameReferences.SpriteTileReferences.GetTileReference(npcState.NPCRef.NPCKeySprite), mapUnitPosition)
        {
            NPCState = npcState;
            bool bLargeMap = TheSmallMapCharacterState == null && NPCState.NPCRef == null;

            PlayerCharacterRecord record = null;

            // gets the player character record for an NPC if one exists
            // this is commonly used when meeting NPCs who have not yet joined your party 
            if (NPCState.NPCRef != null)
                record = GameStateReference.State.CharacterRecords.GetCharacterRecordByNPC(NPCState.NPCRef);

            _playerCharacterRecordIndex =
                GameStateReference.State.CharacterRecords.GetCharacterIndexByNPC(NPCState.NPCRef);

            // is the NPC you are loading currently in the party?
            IsInParty = record != null && record.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InTheParty;

            // it's a large map so we follow different logic to determine the placement of the character
            if (bLargeMap)
            {
                Move(MapUnitPosition);
            }
            else
            {
                // there is no TheSmallMapCharacterState which indicates that it is a large map
                if (!bLoadedFromDisk)
                {
                    if (NPCState.NPCRef != null)
                        MoveNpcToDefaultScheduledPosition(GameStateReference.State.TheTimeOfDay);
                }
                else
                {
                    Move(MapUnitPosition);
                }
            }
        }

        // ReSharper disable once UnusedMember.Global
        public override string GetDebugDescription(TimeOfDay timeOfDay)
        {
            if (NPCRef != null)
                return "Name=" + NPCRef.FriendlyName + " " + MapUnitPosition + " Scheduled to be at: " +
                       NPCRef.Schedule.GetCharacterDefaultPositionByTime(timeOfDay) + " with AI Mode: " +
                       NPCRef.Schedule.GetCharacterAiTypeByTime(timeOfDay) + " <b>Movement Attempts</b>: " +
                       MovementAttempts + " " + Movement;

            return "MapUnit " + KeyTileReference.Description + " " + MapUnitPosition + " Scheduled to be at: " +
                   " <b>Movement Attempts</b>: " + MovementAttempts + " " + Movement;
        }

        public override TileReference GetNonBoardedTileReference() => KeyTileReference;
    }
}