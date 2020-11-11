using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Ultima5Redux.Maps
{
    [DataContract] [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TileReference
    {
        [DataMember] public int AnimationIndex;

        [DataMember] public string Description;

        [DataMember] public bool DontDraw;

        [DataMember] public int FlatTileSubstitionIndex;

        [DataMember] public string FlatTileSubstitionName;

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

        public bool IsNPCCapableSpace => IsWalking_Passable || IsOpenable;


        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsSolidSpriteButNotDoor => !IsWalking_Passable && !IsOpenable;

        public bool IsSolidSpriteButNotDoorAndNotNPC => IsSolidSpriteButNotDoor && !IsNPC;

        public bool IsSolidSprite => !IsWalking_Passable;

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///     Makes a best guess on the directionality of the sprite
        /// </summary>
        /// <returns></returns>
        public VirtualMap.Direction GetDirection()
        {
            if (Name.Contains("Left")) return VirtualMap.Direction.Left;
            if (Name.Contains("Right")) return VirtualMap.Direction.Right;
            if (Name.Contains("Down")) return VirtualMap.Direction.Down;
            if (Name.Contains("Up")) return VirtualMap.Direction.Up;
            return VirtualMap.Direction.None;
        }
    }
}