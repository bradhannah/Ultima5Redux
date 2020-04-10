using System.Collections.Generic;
using Newtonsoft.Json;
using Ultima5Redux.MapCharacters;

namespace Ultima5Redux.Maps
{
    public class TileOverrides
    {
        private readonly Dictionary<int, List<TileOverride>> _tileOverrides;

        /// <summary>
        /// Gets all tile overrides by a single map location (which includes a single floor)
        /// </summary>
        /// <param name="singleMapReference"></param>
        /// <returns>a list of TileOverride object, can be empty, but never null</returns>
        public List<TileOverride> GetTileOverridesBySingleMap(SmallMapReferences.SingleMapReference singleMapReference)
        {
            List<TileOverride> tileOverrideList = new List<TileOverride>();
            
            if (!_tileOverrides.ContainsKey((int) singleMapReference.MapLocation)) return null;
            
            foreach (TileOverride tileOverride in _tileOverrides[(int) singleMapReference.MapLocation])
            {
                if (singleMapReference.Floor == tileOverride.Position.Floor) tileOverrideList.Add(tileOverride);
            }

            return tileOverrideList;
        }
        
        public Dictionary<Point2D, TileOverride> GetTileXYOverridesBySingleMap(SmallMapReferences.SingleMapReference singleMapReference)
        {
            Dictionary<Point2D, TileOverride> tileOverrideList = new Dictionary<Point2D, TileOverride>();
            
            if (!_tileOverrides.ContainsKey((int) singleMapReference.MapLocation)) return null;
            
            foreach (TileOverride tileOverride in _tileOverrides[(int) singleMapReference.MapLocation])
            {
                if (singleMapReference.Floor == tileOverride.Position.Floor)
                {
                    Point2D xy = new Point2D(tileOverride.X, tileOverride.Y);
                    if (tileOverrideList.ContainsKey(xy))
                    {
                        throw new Ultima5ReduxException("You have a duplicate record in TileOverrides: "+singleMapReference.MapLocation + " "+ xy);
                    }
                    tileOverrideList.Add(xy, tileOverride);
                }
            }

            return tileOverrideList;
        }
        
        /// <summary>
        /// Gets a replacement sprite for a specific location and position on a map
        /// </summary>
        /// <param name="location"></param>
        /// <param name="position"></param>
        /// <param name="bOverworld"></param>
        /// <returns>TileOverride if one exists, otherwise null</returns>
        public TileOverride GetReplacementSprite(SmallMapReferences.SingleMapReference.Location location,
            CharacterPosition position, bool bOverworld)
        {
            if (!_tileOverrides.ContainsKey((int) location)) return null;

            if (location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld) // it's a large map
            {
                foreach (TileOverride tileOverride in _tileOverrides[(int) location])
                {
                    if (bOverworld && tileOverride.IsOverworld && position == tileOverride.Position)
                        return tileOverride;
                    if (!bOverworld && tileOverride.IsUnderworld && position == tileOverride.Position)
                        return tileOverride;
                }
            }
            else  // this is a small map
            {
                foreach (TileOverride tileOverride in _tileOverrides[(int) location])
                {
                    if (position == tileOverride.Position) return tileOverride;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Constructor loads resource and builds tables
        /// </summary>
        public TileOverrides()
        {
            string textAsset = Properties.Resources.TileOverrides;
            _tileOverrides = JsonConvert.DeserializeObject<Dictionary<int, List<TileOverride>>>(textAsset);

            // we cycle through each map
            // we must assign the map to the tile override on the inside
            foreach (int nMap in _tileOverrides.Keys)
            {
                // for each TileOverride within the current map
                foreach (TileOverride tileOverride in _tileOverrides[nMap])
                {
                    // I hate that I have to assign it this way, but it seems to be the only way for now
                    tileOverride.MapNumber = nMap;
                    if (((tileOverride.IsOverworld ? 1 : 0) + (tileOverride.IsUnderworld ? 1 : 0) +
                         (tileOverride.IsSmallMap ? 1 : 0)) != 1)
                    {
                        throw new Ultima5ReduxException(
                            "You have set multiple overworld, underworld and small map flags which is a big no no");
                    }
                }
                
            }
            
                
        }

    }

    /// <summary>
    /// A single overriden tile 
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TileOverride
    {
        [JsonProperty]
        public int MapNumber;

        public bool IsOverworld
        {
            get => MapNumber == 0 && Z == 0;
        }
        public bool IsUnderworld
        {
            get => MapNumber == 0 && Z == -1;
        }
        public bool IsSmallMap
        {
            get => MapNumber != 0;
        }
        [JsonProperty]
        public int X { get; set; }
        [JsonProperty]
        public int Y { get; set; }
        [JsonProperty]
        public int Z { get; set; }
        [JsonProperty]
        public int SpriteNum { get; set; }
        [JsonProperty]
        public string SpriteName { get; set; }
        [JsonProperty]
        public string Comment { get; set; }

        public CharacterPosition Position => new CharacterPosition(X, Y, Z);
    }
}