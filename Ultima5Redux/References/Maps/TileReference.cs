using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Ultima5Redux.MapUnits;

namespace Ultima5Redux.References.Maps
{
    [DataContract] [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class TileReference
    {
        /// <summary>
        ///     This is not ideal but leaving unlabelled integers all over the code is a worse offense
        /// </summary>
        public enum SpriteIndex
        {
            Nothing = 0, Water = 1, Swamp = 4, Grass = 5, Desert1 = 7, TableFoodTop = 154, TableFoodBottom = 155,
            TableFoodBoth = 156, TableMiddle = 149, PlowedField = 44, Cactus = 47, WheatInField = 45,
            HitchingPost = 162, Guard_KeyIndex = 368, Rat_KeyIndex = 400, StoneBrickWallSecret = 78, RegularDoor = 184,
            LockedDoor = 185, RegularDoorView = 186, LockedDoorView = 187, RightSconce = 176, LeftScone = 177,
            Brazier = 178, CampFire = 179, LampPost = 189, CandleOnTable = 190, CookStove = 191, BlueFlame = 222,
            ChairBackForward = 144, ChairBackLeft = 145, ChairBackBack = 146, ChairBackRight = 147, MagicLockDoor = 151,
            MagicLockDoorWithView = 152, Grate = 134, LeftBed = 171, SimpleCross = 137, StoneHeadstone = 138,
            HorseRight = 272, HorseLeft = 273, LadderDown = 201, LadderUp = 200, Carpet2_MagicCarpet = 283,
            Manacles = 133, Mirror = 157, MirrorAvatar = 158, MirrorBroken = 159, SmallMountains = 12,
            Daemon1_KeyIndex = 472, StoneGargoyle_KeyIndex = 440, Fighter_KeyIndex = 328, Bard_KeyIndex = 324,
            BardPlaying_KeyIndex = 348, TownsPerson_KeyIndex = 336, Ray_KeyIndex = 400, Fireplace = 188,
            Bat_KeyIndex = 404, ShadowLord_KeyIndex = 508, ItemPotion = 259, ItemScroll = 260, ItemWeapon = 261,
            ItemShield = 262, ItemHelm = 265, ItemRing = 266, ItemArmour = 267, ItemAnkh = 268, Lava = 143,
            ItemKey = 263, DeadBody = 286, BloodSpatter = 287, Chest = 257, Whirlpool_KeyIndex = 492, PoisonField = 488,
            MagicField = 489, FireField = 490, ElectricField = 491, ItemFood = 271, ItemGem = 264, ItemTorch = 269,
            ItemMoney = 258, Beggar_KeyIndex = 365, HolyFloorSymbol = 278, Telescope = 89, Clock1 = 250, Clock2 = 251,
            Well = 161
        }


        public const int N_TYPICAL_ANIMATION_FRAMES = 4;

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
        [DataMember] public int TotalAnimationFrames { get; private set; }

        [IgnoreDataMember]
        public bool IsMonsterSpawnable =>
            IsBoat_Passable || IsCarpet_Passable || IsHorse_Passable || IsWalking_Passable || IsWaterEnemyPassable ||
            IsLandEnemyPassable;

        public bool HasAlternateFlatSprite =>
            GameReferences.Instance.SpriteTileReferences.GetTileReference(Index).FlatTileSubstitutionIndex != -1;

        public bool HasSearchReplacement => Index == (int)SpriteIndex.StoneBrickWallSecret;

        public bool IsNPCCapableSpace => IsWalking_Passable || IsOpenable;

        public bool IsRangeWeaponPassable => RangeWeapon_Passable;

        public bool IsSolidSprite => !IsWalking_Passable;

        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsSolidSpriteButNotDoor => !IsWalking_Passable && !IsOpenable;

        // Exclude the black square from this, it messes up door and tombstone horizontal checks
        public bool IsSolidSpriteButNotDoorAndNotNPC => IsSolidSpriteButNotDoor && !IsNPC && Index != 255;
        public bool IsSolidSpriteButNotNPC => IsSolidSprite && !IsNPC && Index != 255;

        public bool IsTableWithFood =>
            Index is (int)SpriteIndex.TableFoodBoth or (int)SpriteIndex.TableFoodBottom
                or (int)SpriteIndex.TableFoodTop;

        public bool IsWaterTile => Name.ToLower().Contains("water");

        public int KeyTileTileReferenceIndex => Index - AnimationIndex;

        public int SearchReplacementIndex
        {
            get
            {
                if (!HasSearchReplacement) return Index;

                switch ((SpriteIndex)Index)
                {
                    case SpriteIndex.StoneBrickWallSecret:
                        return (int)SpriteIndex.LockedDoor;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override string ToString() => Name;

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

        public int GetRandomAnimationFrameIndex(out bool bNonRandomTime)
        {
            int nNewTileIndex = 0;

            switch (TotalAnimationFrames)
            {
                case -1:
                    // this means there is a custom animation we will need to account for
                    nNewTileIndex = 0;
                    bNonRandomTime = true;
                    break;
                case 2:
                    // we will simply toggle between the two, perhaps we don't do random intervals either?
                    nNewTileIndex = AnimationIndex == 0 ? Index + 1 : Index;
                    bNonRandomTime = false;
                    break;
                default:
                    // make sure it's greater than zero
                    if (TotalAnimationFrames <= 1)
                        throw new Ultima5ReduxException(
                            $"asked for an animation frame with invalid frame total: {TotalAnimationFrames}");
                    // find a new frame that isn't the current one - or do we even care?
                    nNewTileIndex = KeyTileTileReferenceIndex + Utils.GetNumberFromAndTo(0, TotalAnimationFrames);
                    bNonRandomTime = true;
                    break;
            }

            return nNewTileIndex;
        }

        public bool IsIndexWithinAnimationFrames(int nIndex)
        {
            if (TotalAnimationFrames <= 1) return nIndex == Index;
            int nOffset = Index - KeyTileTileReferenceIndex;
            return nOffset >= 0 && nOffset < TotalAnimationFrames;
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