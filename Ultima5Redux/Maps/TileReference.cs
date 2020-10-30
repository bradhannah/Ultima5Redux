using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Ultima5Redux.Maps
{
    [DataContract]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TileReference
    {
        [DataMember]
        public int Index;
        [DataMember]
        public string Name;
        [DataMember]
        public string Description;
        [DataMember]
        public bool IsWalking_Passable; 
        [DataMember]
        public bool IsBoat_Passable;
        [DataMember]
        public bool IsSkiff_Passable;
        [DataMember]
        public bool IsCarpet_Passable;
        [DataMember]
        public bool IsOpenable;
        [DataMember]
        public bool IsPartOfAnimation;
        [DataMember]
        public int AnimationIndex;
        [DataMember]
        public bool IsUpright;
        [DataMember]
        public int FlatTileSubstitionIndex;
        [DataMember]
        public string FlatTileSubstitionName;
        [DataMember]
        public bool IsEnemy;
        [DataMember]
        public bool IsNPC;
        [DataMember]
        public bool IsBuilding;
        [DataMember]
        public bool DontDraw;
        [DataMember]
        public int SpeedFactor;
        [DataMember]
        public bool IsKlimable;
        [DataMember] 
        public bool IsPushable;
        [DataMember]
        public bool IsTalkOverable;
        [DataMember] public bool IsBoardable;

        public override string ToString()
        {
            return this.Name;
        }

        public bool IsNPCCapableSpace => (this.IsWalking_Passable || this.IsOpenable);


        public bool IsSolidSpriteButNotDoor => (!this.IsWalking_Passable && !this.IsOpenable);

        public bool IsSolidSprite => (!IsWalking_Passable);

        /// <summary>
        /// Makes a best guess on the directionality of the sprite
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