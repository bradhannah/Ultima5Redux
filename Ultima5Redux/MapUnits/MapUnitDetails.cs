using System.Runtime.Serialization;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public abstract class MapUnitDetails
    {
        [IgnoreDataMember] public abstract Avatar.AvatarState BoardedAvatarState { get; }
        [IgnoreDataMember] public abstract string BoardXitName { get; }
        [IgnoreDataMember] public abstract string FriendlyName { get; }

        /// <summary>
        ///     Is the map character currently an active character on the current map
        /// </summary>
        [IgnoreDataMember] public abstract bool IsActive { get; }

        [IgnoreDataMember] public abstract bool IsAttackable { get; }
        [IgnoreDataMember] public abstract MapUnitPosition MapUnitPosition { get; internal set; }

        [DataMember] public Point2D.Direction Direction { get; set; }

        /// <summary>
        ///     How many iterations will I force the character to wander?
        /// </summary>
        [DataMember] internal int ForcedWandering { get; set; }

        /// <summary>
        ///     Is the character currently active on the map?
        /// </summary>
        [DataMember] protected internal bool IsInParty { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        [DataMember] public bool IsOccupiedByAvatar { get; protected internal set; }
        [DataMember] public SmallMapReferences.SingleMapReference.Location MapLocation { get; set; }

        /// <summary>
        ///     All the movements for the map character
        /// </summary>
        [DataMember] internal MapUnitMovement Movement { get; private protected set; }

        [DataMember] protected internal int MovementAttempts { get; set; }

        /// <summary>
        ///     The location state of the character
        /// </summary>
        [DataMember] protected internal SmallMapCharacterState TheSmallMapCharacterState { get; set; }

        [IgnoreDataMember] public bool UseFourDirections { get; set; }
    }
}