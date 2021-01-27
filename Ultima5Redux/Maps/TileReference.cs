using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Ultima5Redux.MapUnits;

namespace Ultima5Redux.Maps
{
    [DataContract] [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TileReference
    {
        [DataMember] public int AnimationIndex;
        [DataMember] public string Description;
        [DataMember] public bool DontDraw;
        [DataMember] public int FlatTileSubstitutionIndex;
        [DataMember] public string FlatTileSubstitutionName;
        [DataMember] public int Index;
        [DataMember] public bool IsBoardable;
        [DataMember] public bool IsBoat_Passable;
        [DataMember] public bool IsBuilding;
        [DataMember] public bool IsCarpet_Passable;
        [DataMember] public bool IsEnemy;
        [DataMember] public bool IsKlimable;
        [DataMember] public bool IsNPC;
        [DataMember] public bool IsOpenable;
        [DataMember] public bool IsPartOfAnimation;
        [DataMember] public bool IsPushable;
        [DataMember] public bool IsSkiff_Passable;
        [DataMember] public bool IsTalkOverable;
        [DataMember] public bool IsUpright;
        [DataMember] public bool IsWalking_Passable;
        [DataMember] public string Name;
        [DataMember] public int SpeedFactor;
        [DataMember] public bool IsHorse_Passable;
        [DataMember] public bool IsGuessableFloor;
        [DataMember] public bool BlocksLight;
        [DataMember] public bool IsWindow;
        [DataMember] public SingleCombatMapReference.BritanniaCombatMaps CombatMapIndex;
        [DataMember] public bool IsLandEnemyPassable;
        [DataMember] public bool IsWaterEnemyPassable;
        	

        public bool IsNPCCapableSpace => IsWalking_Passable || IsOpenable;

        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsSolidSpriteButNotDoor => !IsWalking_Passable && !IsOpenable;

        public bool IsSolidSpriteButNotDoorAndNotNPC => IsSolidSpriteButNotDoor && !IsNPC;

        public bool IsSolidSprite => !IsWalking_Passable;

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
    }
}