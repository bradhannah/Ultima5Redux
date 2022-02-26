using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Ultima5Redux.MapUnits;

namespace Ultima5Redux.References.Maps
{
    [DataContract] [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TileReference
    {
        [DataMember] public int AnimationIndex { get; private set; }
        [DataMember] public bool BlocksLight { get; private set; }
        [DataMember] public SingleCombatMapReference.BritanniaCombatMaps CombatMapIndex { get; private set; }
        [DataMember] public string Description { get; private set; }
        [DataMember] public bool DontDraw { get; private set; }
        [DataMember] public int FlatTileSubstitutionIndex { get; private set; }
        [DataMember] public string FlatTileSubstitutionName { get; private set; }
        [DataMember] public int Index { get; internal set; }
        [DataMember] public bool IsBoardable { get; private set; }
        [DataMember] public bool IsBoat_Passable { get; private set; }
        [DataMember] public bool IsBuilding { get; private set; }
        [DataMember] public bool IsCarpet_Passable { get; private set; }
        [DataMember] public bool IsEnemy { get; private set; }
        [DataMember] public bool IsGuessableFloor { get; private set; }
        [DataMember] public bool IsHorse_Passable { get; private set; }
        [DataMember] public bool IsKlimable { get; private set; }
        [DataMember] public bool IsLandEnemyPassable { get; private set; }
        [DataMember] public bool IsNPC { get; private set; }
        [DataMember] public bool IsOpenable { get; private set; }
        [DataMember] public bool IsPartOfAnimation { get; private set; }
        [DataMember] public bool IsPushable { get; private set; }
        [DataMember] public bool IsSkiff_Passable { get; private set; }
        [DataMember] public bool IsTalkOverable { get; private set; }
        [DataMember] public bool IsUpright { get; private set; }
        [DataMember] public bool IsWalking_Passable { get; private set; }
        [DataMember] public bool IsWaterEnemyPassable { get; private set; }
        [DataMember] public bool IsWindow { get; private set; }
        [DataMember] public string Name { get; private set; }
        [DataMember] public bool RangeWeapon_Passable { get; private set; }
        [DataMember] public int SpeedFactor { get; private set; }
        public bool IsNPCCapableSpace => IsWalking_Passable || IsOpenable;

        public bool IsRangeWeaponPassable => RangeWeapon_Passable;

        public bool IsSolidSprite => !IsWalking_Passable;

        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsSolidSpriteButNotDoor => !IsWalking_Passable && !IsOpenable;

        public bool IsSolidSpriteButNotDoorAndNotNPC => IsSolidSpriteButNotDoor && !IsNPC;

        public bool IsWaterTile => Name.ToLower().Contains("water");

        [IgnoreDataMember]
        public bool IsMonsterSpawnable =>
            IsBoat_Passable || IsCarpet_Passable || IsHorse_Passable || IsWalking_Passable || IsWaterEnemyPassable ||
            IsLandEnemyPassable;

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///     Makes a best guess on the directionality of the sprite
        /// </summary>
        /// <returns></returns>
        public Point2D.Direction GetDirection()
        {
            if (Name.Contains("Left")) return Point2D.Direction.Left;
            if (Name.Contains("Right")) return Point2D.Direction.Right;
            if (Name.Contains("Down")) return Point2D.Direction.Down;
            if (Name.Contains("Up")) return Point2D.Direction.Up;
            return Point2D.Direction.None;
        }

        public bool IsPassable(Avatar.AvatarState avatarState)
        {
            switch (avatarState)
            {
                case Avatar.AvatarState.Horse:
                    return IsHorse_Passable;
                case Avatar.AvatarState.Regular:
                    return IsWalking_Passable;
                case Avatar.AvatarState.Carpet:
                    return IsCarpet_Passable;
                case Avatar.AvatarState.Frigate:
                    return IsBoat_Passable;
                case Avatar.AvatarState.Skiff:
                    return IsSkiff_Passable;
                case Avatar.AvatarState.Hidden:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(avatarState), avatarState, null);
            }
        }
    }
}