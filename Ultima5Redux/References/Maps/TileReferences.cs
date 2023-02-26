using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Ultima5Redux.Properties;

namespace Ultima5Redux.References.Maps
{
    public class TileReferences
    {
        private const int MILLISECONDS_BETWEEN_FRAMES = 350;

        private readonly Dictionary<TileReference.SpriteIndex, bool> _burningThings = new()
        {
            { TileReference.SpriteIndex.RightSconce, true },
            { TileReference.SpriteIndex.LeftScone, true },
            { TileReference.SpriteIndex.Brazier, true },
            { TileReference.SpriteIndex.CampFire, true },
            { TileReference.SpriteIndex.LampPost, true },
            { TileReference.SpriteIndex.CandleOnTable, true },
            { TileReference.SpriteIndex.CookStove, true },
            { TileReference.SpriteIndex.BlueFlame, true }
        };

        private readonly Dictionary<int, int> _npcOnTopMap;

        private readonly Dictionary<int, int> _npcOnTopMapWithFood;

        /// <summary>
        ///     String references for Ultima 5
        /// </summary>
        private readonly U5StringRef _u5StringRef;

        private double _dLastTime;

        private int _nTick;

        /// <summary>
        ///     Tile reference dictionary referenced by tile string value
        /// </summary>
        private Dictionary<string, TileReference> TileReferenceByStringDictionary { get; } =
            new(1024);

        /// <summary>
        ///     Tile reference dictionary indexed by sprite number
        /// </summary>
        private Dictionary<int, TileReference> TileReferenceDictionary { get; }

        /// <summary>
        ///     Number of tile references
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public int Count => TileReferenceByStringDictionary.Count;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="u5StringRef"></param>
        public TileReferences(U5StringRef u5StringRef)
        {
            _u5StringRef = u5StringRef;
            TileReferenceDictionary = Load();

            TileReferenceByStringDictionary.Add("BareHands", TileReferenceDictionary[261]);
            for (int i = 0; i < TileReferenceDictionary.Count; i++)
            {
                TileReference tileRef = TileReferenceDictionary[i];
                // this is gross, but I can't seem to get the index number in any other way...
                tileRef.Index = i;
                TileReferenceByStringDictionary.Add(tileRef.Name, tileRef);
            }

            _npcOnTopMapWithFood = new Dictionary<int, int>
            {
                [GetTileNumberByName("ChairBackForward")] = GetTileNumberByName("SitChairDownEat1"),
                [GetTileNumberByName("ChairBackBack")] = GetTileNumberByName("SitChairUpEat1")
            };
            _npcOnTopMap = new Dictionary<int, int>
            {
                [GetTileNumberByName("ChairBackForward")] = GetTileNumberByName("SitChairDown"),
                [GetTileNumberByName("ChairBackLeft")] = GetTileNumberByName("SitChairLeft"),
                [GetTileNumberByName("ChairBackBack")] = GetTileNumberByName("SitChairUp"),
                [GetTileNumberByName("ChairBackRight")] = GetTileNumberByName("SitChairRight")
            };
        }

        /// <summary>
        ///     Loads the data from the embedded JSON data
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, TileReference> Load()
        {
            Dictionary<int, TileReference> result =
                JsonConvert.DeserializeObject<Dictionary<int, TileReference>>(Resources.TileData);

            return result;
        }

        /// <summary>
        ///     Is it an unoccupied chair?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public static bool IsChair(int nSprite)
        {
            // optimization
            // 144 - 147
            bool bIsChair = nSprite is >= (int)TileReference.SpriteIndex.ChairBackForward and <=
                (int)TileReference.SpriteIndex.ChairBackRight;
            return bIsChair;
        }

        /// <summary>
        ///     Is the sprite any of the door sprites (lock, unlocked, with a view, magic)
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static bool IsDoor(int nSprite) =>
            (TileReference.SpriteIndex)nSprite is
            TileReference.SpriteIndex.RegularDoor or
            TileReference.SpriteIndex.LockedDoor or
            TileReference.SpriteIndex.RegularDoorView or
            TileReference.SpriteIndex.LockedDoorView;

        /// <summary>
        ///     Is it a locked door?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public static bool IsDoorLocked(int nSprite) =>
            (TileReference.SpriteIndex)nSprite is
            TileReference.SpriteIndex.LockedDoor or
            TileReference.SpriteIndex.LockedDoorView;

        /// <summary>
        ///     Is it a magical door?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public static bool IsDoorMagical(int nSprite) =>
            (TileReference.SpriteIndex)nSprite is
            TileReference.SpriteIndex.MagicLockDoor or
            TileReference.SpriteIndex.MagicLockDoorWithView;

        /// <summary>
        ///     is it a door with a view window? locked or unlocked
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public static bool IsDoorWithView(int nSprite) =>
            (TileReference.SpriteIndex)nSprite is
            TileReference.SpriteIndex.MagicLockDoorWithView or
            TileReference.SpriteIndex.RegularDoorView or
            TileReference.SpriteIndex.LockedDoorView;

        /// <summary>
        ///     Is the sprite a klimbable grate?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public static bool IsGrate(int nSprite) => nSprite == (int)TileReference.SpriteIndex.Grate;

        /// <summary>
        ///     is the sprite the head of a bed
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool IsHeadOfBed(int nSprite) => nSprite == (int)TileReference.SpriteIndex.LeftBed;

        public static bool IsHeadstone(int nSprite) =>
            nSprite is (int)TileReference.SpriteIndex.SimpleCross
                or (int)TileReference.SpriteIndex.StoneHeadstone;

        /// <summary>
        ///     is the sprite an up or a down ladder?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public static bool IsLadder(int nSprite) => IsLadderDown(nSprite) || IsLadderUp(nSprite);

        /// <summary>
        ///     Is the sprite a down ladder?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public static bool IsLadderDown(int nSprite) => nSprite == (int)TileReference.SpriteIndex.LadderDown;

        /// <summary>
        ///     Is the sprite an up ladder?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public static bool IsLadderUp(int nSprite) => nSprite == (int)TileReference.SpriteIndex.LadderUp;

        public static bool IsMagicCarpet(int nSprite) => nSprite == (int)TileReference.SpriteIndex.Carpet2_MagicCarpet;

        /// <summary>
        ///     is the sprite manacles (hand irons)
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool IsManacles(int nSprite) => nSprite == (int)TileReference.SpriteIndex.Manacles;

        /// <summary>
        ///     Is the spirit a mirror? broken, reflected or regular
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static bool IsMirror(int nSprite) =>
            (TileReference.SpriteIndex)nSprite is TileReference.SpriteIndex.Mirror
            or TileReference.SpriteIndex.MirrorAvatar or TileReference.SpriteIndex.MirrorBroken;

        // a bit a rough cut right now, this will need to be refined as the monsters are assigned actual behaviours
        public static bool IsMonster(int nSprite) => nSprite is >= 384 and <= 511;

        /// <summary>
        ///     Is the sprite a staircase sprite
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public static bool IsStaircase(int nSprite)
        {
            bool bIsLadder = nSprite is >= 196 and <= 199;
            // nSprite == GetTileNumberByName("StairsWest") ||
            //              nSprite == GetTileNumberByName("StairsEast") ||
            //              nSprite == GetTileNumberByName("StairsNorth") ||
            //              nSprite == GetTileNumberByName("StairsSouth"); // is it a ladder
            return bIsLadder;
        }

        /// <summary>
        ///     is the sprite a stock
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool IsStocks(int nSprite)
        {
            bool bIsStocks = nSprite == (int)TileReference.SpriteIndex.Stocks;
            return bIsStocks;
        }

        /// <summary>
        ///     Is it an unbroken mirror - reflected or regular
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static bool IsUnbrokenMirror(int nSprite) =>
            (TileReference.SpriteIndex)nSprite is TileReference.SpriteIndex.Mirror
            or TileReference.SpriteIndex.MirrorAvatar;

        public static bool IsUnmountedHorse(int nSprite) =>
            nSprite is (int)TileReference.SpriteIndex.HorseLeft
                or (int)TileReference.SpriteIndex.HorseRight;

        /// <summary>
        ///     does the sprite require a grappling hook to Klimb?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public static bool RequiresGrapplingHook(int nSprite) =>
            nSprite == (int)TileReference.SpriteIndex.SmallMountains;

        public TileReference GetAnimatedTileReference(int nSprite)
        {
            TileReference keyTileReference = GetTileReferenceOfKeyIndex(nSprite);
            if (keyTileReference.TotalAnimationFrames < 2) return keyTileReference;
            double dTotalMilli = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
            if (dTotalMilli - _dLastTime > MILLISECONDS_BETWEEN_FRAMES)
            {
                _nTick = (_nTick + 1) % int.MaxValue;
                _dLastTime = dTotalMilli;
            }

            return GetTileReference(keyTileReference.Index + _nTick % keyTileReference.TotalAnimationFrames);
        }

        /// <summary>
        ///     When building a map, there are times where default tiles need to be replaced with substitute tiles (like klimbing a
        ///     ladder)
        ///     or replaced with an animation (like eating in a chair).
        ///     This method will use the info provided to provide a suitable substitution
        /// </summary>
        /// <param name="nSprite">The default sprite</param>
        /// <param name="bIsNpcTile">Is there an NPC on the tile as well?</param>
        /// <param name="bIsAvatarTile">Is the Avatar on the tile?</param>
        /// <param name="bIsFoodNearby">Is there food within +/- 1 y pos?</param>
        /// <param name="bIsDaylight"> Is it current daylight </param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public int GetCorrectSprite(int nSprite, bool bIsNpcTile, bool bIsAvatarTile, bool bIsFoodNearby,
            bool bIsDaylight)
        {
            int nNewSprite = nSprite;

            if (IsChair(nSprite)) // on a chair
            {
                // this is trickier than you would think because the chair can 
                // be in multiple directions

                if (!bIsNpcTile && !bIsAvatarTile) return nNewSprite;

                if (bIsFoodNearby)
                {
                    if (_npcOnTopMapWithFood.ContainsKey(nSprite)) nNewSprite = _npcOnTopMapWithFood[nSprite];
                }
                else
                {
                    // if there is a mapping of the current sprite for an alternate sprite when an NPC is on it
                    // then remap the sprite
                    if (_npcOnTopMap.ContainsKey(nSprite)) nNewSprite = _npcOnTopMap[nSprite];
                }
            }
            else if (IsLadder(nSprite)) // on a ladder
            {
                if (bIsNpcTile || bIsAvatarTile)
                    nNewSprite = IsLadderUp(nSprite)
                        ? GetTileNumberByName("KlimbLadderUp")
                        : GetTileNumberByName("KlimbLadderDown");
            }
            else if (IsHeadOfBed(nSprite) && (bIsNpcTile || bIsAvatarTile)) // in bed
            {
                nNewSprite = GetTileNumberByName("SleepingInBed");
            }
            else if (IsStocks(nSprite) && (bIsNpcTile || bIsAvatarTile)) // in the stocks
            {
                nNewSprite = GetTileNumberByName("PersonStocks1");
            }
            else if (IsManacles(nSprite) && (bIsNpcTile || bIsAvatarTile)) // shackled up
            {
                nNewSprite = GetTileNumberByName("WallPrisoner1");
            }
            // if it is nighttime and the sprite is a brick archway, then we put the portcullis down
            else if (!bIsDaylight && nSprite == GetTileNumberByName("BrickWallArchway"))
            {
                nNewSprite = GetTileNumberByName("BrickWallArchwayWithPortcullis");
            }

            return nNewSprite;
        }

        /// <summary>
        ///     Returns the travel speed factor of a particular tile
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public int GetMinuteIncrement(int nSprite) => GetTileReference(nSprite).SpeedFactor;

        /// <summary>
        ///     Get the message you should display to the user when travelling on a given sprite
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public string GetSlowMovementString(int nSprite)
        {
            TileReference tileRef = GetTileReference(nSprite);
            switch (tileRef.SpeedFactor)
            {
                case 2:
                    return string.Empty;
                case 4:
                    // this is normal speed
                    return _u5StringRef.GetString(DataOvlReference.WorldStrings.SLOW_PROG);
                case 6:
                    return _u5StringRef.GetString(DataOvlReference.WorldStrings.VERY_SLOW);
                case 1:
                case -1:
                default:
                    return "Untrodden CombatLand";
            }
        }

        /// <summary>
        ///     Gets a tile sprite index by the tiles string name (defined in JSON)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetTileNumberByName(string name) => TileReferenceByStringDictionary[name].Index;

        /// <summary>
        ///     Gets the tile reference based on sprite index
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public TileReference GetTileReference(int nSprite)
        {
            if (!TileReferenceDictionary.ContainsKey(nSprite))
                throw new Ultima5ReduxException("Requested tile reference for sprite " + nSprite +
                                                " but it doesn't exist");
            return TileReferenceDictionary[nSprite] ??
                   throw new Ultima5ReduxException("Null tile reference: " + nSprite);
        }

        public TileReference GetTileReference(TileReference.SpriteIndex spriteIndex) =>
            GetTileReference((int)spriteIndex);

        /// <summary>
        ///     Gets a tile reference by the tiles string name (defined in JSON)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TileReference GetTileReferenceByName(string name) => TileReferenceByStringDictionary[name];

        public TileReference GetTileReferenceOfKeyIndex(int nSprite)
        {
            TileReference tileReference = GetTileReference(nSprite);
            // if it isn't part of an animation then we can trust the tile index provided in the MapUnitState 
            if (!tileReference.IsPartOfAnimation) return tileReference;

            Debug.Assert(tileReference.AnimationIndex >= 0);
            if (tileReference.AnimationIndex == 0) return tileReference;

            // the AnimationIndex is an offset from the index,so by subtracting it, we get the key frame (initial)
            // frame which is handy if the visual asset is described by it's key index
            return GetTileReference(tileReference.Index - tileReference.AnimationIndex);
        }

        public bool IsBurningThing(int nSprite) => _burningThings.ContainsKey((TileReference.SpriteIndex)nSprite);

        public bool IsFrigate(int nSprite) =>
            GetTileReference(nSprite).Name.StartsWith("Ship") ||
            GetTileReference(nSprite).Name.StartsWith("Pirate");

        /// <summary>
        ///     Can you typically bury a moonstone on the tile type?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns>true if you can normally bury a moonstone</returns>
        public bool IsMoonstoneBuriable(int nSprite)
        {
            TileReference tileRef = GetTileReference(nSprite);
            return tileRef.Name is "Grass" or "Swamp" or "Desert1" || tileRef.Name.Contains("Forest");
        }

        /// <summary>
        ///     Is the sprite a dirt path tile?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsPath(int nSprite)
        {
            bool bIsPath = GetTileReference(nSprite).Name.Contains("Path");
            return bIsPath;
        }

        /// <summary>
        ///     is it a readable sign?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsSign(int nSprite) =>
            GetTileNumberByName("Sign") == nSprite || GetTileNumberByName("SimpleCross") == nSprite ||
            GetTileNumberByName("StoneHeadstone") == nSprite || GetTileNumberByName("SmallSign") == nSprite ||
            GetTileNumberByName("SignWarning") == nSprite;

        public bool IsSkiff(int nSprite) => GetTileReference(nSprite).Name.StartsWith("Skiff");
    }
}