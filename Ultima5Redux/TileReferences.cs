using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class TileReferences
    {
        public Dictionary<int, TileReference> TileReferenceDictionary { get; }
        public Dictionary<string, TileReference> TileReferenceByStringDictionary { get; } = new Dictionary<string, TileReference>(1024);

        public int Count { get { return TileReferenceByStringDictionary.Count; } }

        private U5StringRef u5StringRef;

        public TileReferences(U5StringRef u5StringRef)
        {
            this.u5StringRef = u5StringRef;
            TileReferenceDictionary = TileReferences.Load();

            //foreach (TileReference tileRef in TileReferenceDictionary)
            for (int i = 0; i < TileReferenceDictionary.Count; i++)
            {
                TileReference tileRef = TileReferenceDictionary[i];
                // this is gross, but I can't seem to get the index number in any other way...
                tileRef.Index = i;
                TileReferenceByStringDictionary.Add(tileRef.Name, tileRef);
            }
        }

        static public Dictionary<int, TileReference> Load()
        {
            Dictionary<int, TileReference> result = JsonConvert.DeserializeObject<Dictionary<int, TileReference>>(Properties.Resources.TileData);

            return result;
        }

        public int GetTileNumberByName(string name)
        {
            return TileReferenceByStringDictionary[name].Index;
        }

        public TileReference GetTileReferenceByName(string name)
        {
            return TileReferenceByStringDictionary[name];
        }

        public TileReference GetTileReference(int nSprite)
        {
            return TileReferenceDictionary[nSprite];
        }

        public bool IsDoor(int nSprite)
        {
            return GetTileReference(nSprite).IsOpenable;
        }

        public bool IsStaircase(int nSprite)
        {
            bool bIsLadder = nSprite == GetTileNumberByName("StairsWest") || nSprite == GetTileNumberByName("StairsEast")
                || nSprite == GetTileNumberByName("StairsNorth") || nSprite == GetTileNumberByName("StairsSouth"); // is it a ladder
            return bIsLadder;
        }

        public bool IsLadderUp(int nSprite)
        {
            return nSprite == GetTileNumberByName("LadderUp");
        }

        public bool IsLadderDown(int nSprite)
        {
            return nSprite == GetTileNumberByName("LadderDown");
        }


        public bool IsLadder(int nSprite)
        {
            bool bIsLadder = IsLadderDown(nSprite) || IsLadderUp(nSprite); // is it a ladder
            return bIsLadder;
        }

        public bool IsHeadOfBed(int nSprite)
        {
            bool bIsHeadOfBed = nSprite == GetTileNumberByName("LeftBed"); // is it the human sleeping side of the bed?
            return bIsHeadOfBed;
        }

        public bool IsStocks(int nSprite)
        {
            bool bIsStocks = nSprite == GetTileNumberByName("Stocks"); // is it the stocks
            return bIsStocks;
        }

        public bool IsManacles(int nSprite)
        {
            bool bIsManacles = nSprite == GetTileNumberByName("Manacles"); // is it shackles/manacles
            return bIsManacles;
        }

        public bool IsDoorMagical(int nSprite)
        {
            Debug.Assert(GetTileReference(nSprite).IsOpenable);
            bool bIsDoorMagical = nSprite == GetTileNumberByName("MagicLockDoorWithView") || nSprite == GetTileNumberByName("MagicLockDoor");
            return bIsDoorMagical;
        }

        public bool IsDoorLocked(int nSprite)
        {
            Debug.Assert(GetTileReference(nSprite).IsOpenable);
            bool bIsDoorLocked = nSprite == GetTileNumberByName("LockedDoor") || nSprite == GetTileNumberByName("LockedDoorView");
            return bIsDoorLocked;
        }

        public bool IsChair(int nSprite)
        {
            bool bIsChair = nSprite== GetTileNumberByName("ChairBackForward") ||
                nSprite == GetTileNumberByName("ChairBackLeft") ||
                nSprite == GetTileNumberByName("ChairBackBack") ||
                nSprite == GetTileNumberByName("ChairBackRight");
            return bIsChair;
        }

        public bool IsSign(int nSprite)
        {
            return (GetTileNumberByName("Sign") == nSprite ||
              GetTileNumberByName("SimpleCross") == nSprite ||
              GetTileNumberByName("StoneHeadstone") == nSprite ||
              GetTileNumberByName("SmallSign") == nSprite ||
              GetTileNumberByName("SignWarning") == nSprite);
        }

        public bool IsDoorWithView(int nSprite)
        {
            bool bIsDoorWithView = nSprite == GetTileNumberByName("RegularDoorView") || nSprite == GetTileNumberByName("LockedDoorView") ||
                 nSprite == GetTileNumberByName("MagicLockDoorWithView");
            return bIsDoorWithView;
        }

        public bool RequiresGrapplingHook(int nSprite)
        {
            return nSprite == GetTileNumberByName("SmallMountains");

        }

        //public bool IsKlimbable(int nSprite)
        //{
        //    return GetTileReference(nSprite).IsKlimable;
        //}

        public int GetMinuteIncrement(int nSprite)
        {
            return GetTileReference(nSprite).SpeedFactor;
        }

        public string GetSlowMovementString(int nSprite)
        {
            TileReference tileRef = GetTileReference(nSprite);
            switch (tileRef.SpeedFactor)
            {
                case 1:
                case -1:
                    throw new Ultima5ReduxException("Asked for a movement string on something that should never be trodden on: " + nSprite.ToString());
                case 2:
                case 4:
                    // this is normal speed
                    return String.Empty;
                case 6:
                    return u5StringRef.GetString(DataOvlReference.WORLD_STRINGS.SLOW_PROG);
                case 8:
                    return u5StringRef.GetString(DataOvlReference.WORLD_STRINGS.VERY_SLOW);
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
                Dictionary<int, int> NPCOnTopMap = new Dictionary<int, int>();
                Dictionary<int, int> NPCOnTopMapWithFood = new Dictionary<int, int>();
                NPCOnTopMapWithFood[GetTileNumberByName("ChairBackForward")] = GetTileNumberByName("SitChairDown1");  // sitting in chair
                NPCOnTopMapWithFood[GetTileNumberByName("ChairBackBack")] = GetTileNumberByName("SitChairUp1");  // sitting in chair
                NPCOnTopMap[GetTileNumberByName("ChairBackForward")] = GetTileNumberByName("SitChairDown");
                NPCOnTopMap[GetTileNumberByName("ChairBackLeft")] = GetTileNumberByName("SitChairLeft");
                NPCOnTopMap[GetTileNumberByName("ChairBackBack")] = GetTileNumberByName("SitChairUp");
                NPCOnTopMap[GetTileNumberByName("ChairBackRight")] = GetTileNumberByName("SitChairRight");

                if (bIsNPCTile || bIsAvatarTile)
                {
                    //bool bIsFoodNearby = IsFoodNearby(voxelPos);
                    if (bIsFoodNearby)
                    {
                        if (NPCOnTopMapWithFood.ContainsKey(nSprite)) nNewSprite = NPCOnTopMapWithFood[nSprite];
                    }
                    else
                    {
                        // if there is a mapping of the current sprite for an alternate sprite when an NPC is on it
                        // then remap the sprite
                        if (NPCOnTopMap.ContainsKey(nSprite)) nNewSprite = NPCOnTopMap[nSprite];
                    }
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
