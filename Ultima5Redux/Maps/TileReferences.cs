using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Ultima5Redux.Data;

namespace Ultima5Redux.Maps
{
    public class TileReferences
    {
        /// <summary>
        /// Tile reference dictionary indexed by sprite number
        /// </summary>
        private Dictionary<int, TileReference> TileReferenceDictionary { get; }
        /// <summary>
        /// Tile reference dictionary referenced by tile string value
        /// </summary>
        private Dictionary<string, TileReference> TileReferenceByStringDictionary { get; } = new Dictionary<string, TileReference>(1024);

        /// <summary>
        /// String references for Ultima 5
        /// </summary>
        private readonly U5StringRef _u5StringRef;

        /// <summary>
        /// Number of tile references
        /// </summary>
        public int Count => TileReferenceByStringDictionary.Count;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="u5StringRef"></param>
        public TileReferences(U5StringRef u5StringRef)
        {
            this._u5StringRef = u5StringRef;
            TileReferenceDictionary = TileReferences.Load();

            for (int i = 0; i < TileReferenceDictionary.Count; i++)
            {
                TileReference tileRef = TileReferenceDictionary[i];
                // this is gross, but I can't seem to get the index number in any other way...
                tileRef.Index = i;
                TileReferenceByStringDictionary.Add(tileRef.Name, tileRef);
            }
        }

        /// <summary>
        /// Loads the data from the embedded JSON data
        /// </summary>
        /// <returns></returns>
        private static Dictionary<int, TileReference> Load()
        {
            Dictionary<int, TileReference> result = JsonConvert.DeserializeObject<Dictionary<int, TileReference>>(Properties.Resources.TileData);

            return result;
        }

        /// <summary>
        /// Gets a tile sprite index by the tiles string name (defined in JSON)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetTileNumberByName(string name)
        {
            return TileReferenceByStringDictionary[name].Index;
        }

        /// <summary>
        /// Gets a tile reference by the tiles string name (defined in JSON)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TileReference GetTileReferenceByName(string name)
        {
            return TileReferenceByStringDictionary[name];
        }

        /// <summary>
        /// Gets the tile reference based on sprite index
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public TileReference GetTileReference(int nSprite)
        {
            return TileReferenceDictionary[nSprite];
        }

        /// <summary>
        /// Is the sprite a dirt path tile?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsPath(int nSprite)
        {
            bool bIsPath = GetTileReference(nSprite).Name.Contains("Path");
            return bIsPath;
        }

        /// <summary>
        /// Can you typically bury a moonstone on the tile type?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns>true if you can normally bury a moonstone</returns>
        public bool IsMoonstoneBuriable(int nSprite)
        {
            TileReference tileRef = GetTileReference(nSprite); 
            return (tileRef.Name == "Grass" || tileRef.Name == "Swamp" || tileRef.Name == "Desert1"
                       || tileRef.Name.Contains("Forest")) ;
        }
        
        /// <summary>
        /// Is the sprite any of the door sprites (lock, unlocked, with a view, magic)
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsDoor(int nSprite)
        {
            return GetTileReference(nSprite).IsOpenable;
        }

        /// <summary>
        /// Is the sprite a staircase sprite
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsStaircase(int nSprite)
        {
            bool bIsLadder = nSprite == GetTileNumberByName("StairsWest") || nSprite == GetTileNumberByName("StairsEast")
                || nSprite == GetTileNumberByName("StairsNorth") || nSprite == GetTileNumberByName("StairsSouth"); // is it a ladder
            return bIsLadder;
        }

        /// <summary>
        /// Is the sprite a klimbable grate?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsGrate(int nSprite)
        {
            bool bIsGrate = nSprite == GetTileNumberByName("Grate");
            return bIsGrate;
        }
        

        /// <summary>
        /// Is the sprite an up ladder?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsLadderUp(int nSprite)
        {
            return nSprite == GetTileNumberByName("LadderUp");
        }

        /// <summary>
        /// Is the sprite a down ladder?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsLadderDown(int nSprite)
        {
            return nSprite == GetTileNumberByName("LadderDown");
        }

        /// <summary>
        /// is the sprite an up or a down ladder?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsLadder(int nSprite)
        {
            bool bIsLadder = IsLadderDown(nSprite) || IsLadderUp(nSprite); // is it a ladder
            return bIsLadder;
        }
        /// <summary>
        /// is the sprite the head (with pillor) of a bed 
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsHeadOfBed(int nSprite)
        {
            bool bIsHeadOfBed = nSprite == GetTileNumberByName("LeftBed"); // is it the human sleeping side of the bed?
            return bIsHeadOfBed;
        }

        /// <summary>
        /// is the sprite a stock 
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsStocks(int nSprite)
        {
            bool bIsStocks = nSprite == GetTileNumberByName("Stocks"); // is it the stocks
            return bIsStocks;
        }

        /// <summary>
        /// is the sprite manacles (hand irons)
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsManacles(int nSprite)
        {
            bool bIsManacles = nSprite == GetTileNumberByName("Manacles"); // is it shackles/manacles
            return bIsManacles;
        }

        /// <summary>
        /// Is the spirit a mirror? broken, reflected or regular
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsMirror(int nSprite)
        {
            bool bIsMirror = nSprite == GetTileNumberByName("Mirror") || nSprite == GetTileNumberByName("MirrorAvatar") 
                                                                      || nSprite == GetTileNumberByName("MirrorBroken");
            return bIsMirror;
        }

        /// <summary>
        /// Is it an unbroken mirror - reflected or regular
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsUnbrokenMirror(int nSprite)
        {
            bool bIsMirror = nSprite == GetTileNumberByName("Mirror") || nSprite == GetTileNumberByName("MirrorAvatar");
            return bIsMirror;
        }
        
        
        /// <summary>
        /// Is it a magical door?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsDoorMagical(int nSprite)
        {
            Debug.Assert(GetTileReference(nSprite).IsOpenable);
            bool bIsDoorMagical = nSprite == GetTileNumberByName("MagicLockDoorWithView") || nSprite == GetTileNumberByName("MagicLockDoor");
            return bIsDoorMagical;
        }

        /// <summary>
        /// Is it a locked door?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsDoorLocked(int nSprite)
        {
            Debug.Assert(GetTileReference(nSprite).IsOpenable);
            bool bIsDoorLocked = nSprite == GetTileNumberByName("LockedDoor") || nSprite == GetTileNumberByName("LockedDoorView");
            return bIsDoorLocked;
        }

        /// <summary>
        /// Is it an unoccupied chair?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsChair(int nSprite)
        {
            bool bIsChair = nSprite== GetTileNumberByName("ChairBackForward") ||
                nSprite == GetTileNumberByName("ChairBackLeft") ||
                nSprite == GetTileNumberByName("ChairBackBack") ||
                nSprite == GetTileNumberByName("ChairBackRight");
            return bIsChair;
        }

        /// <summary>
        /// is it a readable sign?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsSign(int nSprite)
        {
            return (GetTileNumberByName("Sign") == nSprite ||
              GetTileNumberByName("SimpleCross") == nSprite ||
              GetTileNumberByName("StoneHeadstone") == nSprite ||
              GetTileNumberByName("SmallSign") == nSprite ||
              GetTileNumberByName("SignWarning") == nSprite);
        }

        /// <summary>
        /// is it a door with a view window? locked or unlocked
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool IsDoorWithView(int nSprite)
        {
            bool bIsDoorWithView = nSprite == GetTileNumberByName("RegularDoorView") || nSprite == GetTileNumberByName("LockedDoorView") ||
                 nSprite == GetTileNumberByName("MagicLockDoorWithView");
            return bIsDoorWithView;
        }

        /// <summary>
        /// does the sprite require a grappling hook to Klimb?
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public bool RequiresGrapplingHook(int nSprite)
        {
            return nSprite == GetTileNumberByName("SmallMountains");

        }

        /// <summary>
        /// Returns the travel speed factor of a particular tile 
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        public int GetMinuteIncrement(int nSprite)
        {
            return GetTileReference(nSprite).SpeedFactor;
        }

        /// <summary>
        /// Get the message you should display to the user when travelling on a given sprite
        /// </summary>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public string GetSlowMovementString(int nSprite)
        {
            TileReference tileRef = GetTileReference(nSprite);
            switch (tileRef.SpeedFactor)
            {
                case 1:
                case -1:
                    throw new Ultima5ReduxException("Asked for a movement string on something that should never be trodden on: " + nSprite.ToString());
                case 2:
                    return string.Empty;
                case 4:
                    // this is normal speed
                    return _u5StringRef.GetString(DataOvlReference.WorldStrings.SLOW_PROG);
                case 6:
                    return _u5StringRef.GetString(DataOvlReference.WorldStrings.VERY_SLOW);
                default:
                    throw new Ultima5ReduxException("Asked for a movement string on something that should never be trodden on: "+nSprite.ToString());
            }

        }

        /// <summary>
        /// When building a map, there are times where default tiles need to be replaced with substitute tiles (like klimbing a ladder)
        /// or replaced with an animation (like eating in a chair). 
        /// 
        /// This method will use the info provided to provide a suitable substitution
        /// </summary>
        /// <param name="nSprite">The default sprite</param>
        /// <param name="bIsNPCTile">Is there an NPC on the tile as well?</param>
        /// <param name="bIsAvatarTile">Is the Avatar on the tile?</param>
        /// <param name="bIsFoodNearby">Is there food within +/- 1 y pos?</param>
        /// <param name="bIsDaylight">Is it current daylight (<8pm)</param>
        /// <returns></returns>
        public int GetCorrectSprite(int nSprite, bool bIsNPCTile, bool bIsAvatarTile, bool bIsFoodNearby, bool bIsDaylight)
        {
            int nNewSprite = nSprite;

            if (IsChair(nSprite)) // on a chair
            {
                // this is trickier than you would think because the chair can 
                // be in multiple directions
                Dictionary<int, int> npcOnTopMapWithFood = new Dictionary<int, int>
                {
                    [GetTileNumberByName("ChairBackForward")] = GetTileNumberByName("SitChairDown1"),
                    [GetTileNumberByName("ChairBackBack")] = GetTileNumberByName("SitChairUp1")
                };
                Dictionary<int, int> npcOnTopMap = new Dictionary<int, int>
                {
                    [GetTileNumberByName("ChairBackForward")] = GetTileNumberByName("SitChairDown"),
                    [GetTileNumberByName("ChairBackLeft")] = GetTileNumberByName("SitChairLeft"),
                    [GetTileNumberByName("ChairBackBack")] = GetTileNumberByName("SitChairUp"),
                    [GetTileNumberByName("ChairBackRight")] = GetTileNumberByName("SitChairRight")
                };

                if (!bIsNPCTile && !bIsAvatarTile) return nNewSprite;
                
                if (bIsFoodNearby)
                {
                    if (npcOnTopMapWithFood.ContainsKey(nSprite)) nNewSprite = npcOnTopMapWithFood[nSprite];
                }
                else
                {
                    // if there is a mapping of the current sprite for an alternate sprite when an NPC is on it
                    // then remap the sprite
                    if (npcOnTopMap.ContainsKey(nSprite)) nNewSprite = npcOnTopMap[nSprite];
                }
            }
            else if (IsLadder(nSprite)) // on a ladder
            {
                if (bIsNPCTile || bIsAvatarTile)
                {
                    nNewSprite = IsLadderUp(nSprite) ?
                        GetTileNumberByName("KlimbLadderUp") : GetTileNumberByName("KlimbLadderDown");
                }
            }
            else if (IsHeadOfBed(nSprite) && (bIsNPCTile || bIsAvatarTile)) // in bed
            {
                nNewSprite = GetTileNumberByName("SleepingInBed");
            }
            else if (IsStocks(nSprite) && (bIsNPCTile || bIsAvatarTile)) // in the stocks
            {
                nNewSprite = GetTileNumberByName("PersonStocks1");
            }
            else if (IsManacles(nSprite) && (bIsNPCTile || bIsAvatarTile)) // shackled up
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
    }
}
