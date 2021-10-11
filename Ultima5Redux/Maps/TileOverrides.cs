using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits;
using Ultima5Redux.Properties;

namespace Ultima5Redux.Maps
{
    public class TileOverrides
    {
        private readonly Dictionary<AllTerritories, List<TileOverride>> _tileOverrides;

        /// <summary>
        ///     Constructor loads resource and builds tables
        /// </summary>
        public TileOverrides()
        {
            string textAsset = Resources.TileOverrides;
            _tileOverrides = JsonConvert.DeserializeObject<Dictionary<AllTerritories, List<TileOverride>>>(textAsset);
        }

        private bool TileOverrideExists(AllTerritories territory, int nMapNumber, int nFloor)
        {
            if (!_tileOverrides.ContainsKey(territory)) return false;
            if (!_tileOverrides[territory].Exists(s => s.MapNumber == nMapNumber)) return false;
            return (_tileOverrides[territory].FindAll(s => s.MapNumber == nMapNumber)
                .FirstOrDefault(tileOverride => nFloor == tileOverride.Position.Floor) != null);
        }

        private List<TileOverride> GetTileOverrides(AllTerritories territory, int nMapNumber, int nFloor) =>
            !TileOverrideExists(territory, nMapNumber, nFloor)
                ? null
                : _tileOverrides[territory].FindAll(s => s.MapNumber == nMapNumber)
                    .Where(tile => nFloor == tile.Position.Floor).ToList();

        private static AllTerritories GetOverrideTerritory(SingleCombatMapReference singleCombatMapReference) =>
            singleCombatMapReference.MapTerritory == SingleCombatMapReference.Territory.Britannia
                ? AllTerritories.CombatBritannia
                : AllTerritories.CombatDungeon;

        private static AllTerritories GetOverrideTerritory(SmallMapReferences.SingleMapReference singleMapReference) =>
            singleMapReference.Floor == 0 ? AllTerritories.Britannia : AllTerritories.Underworld;

        public List<TileOverride> GetTileOverrides(SingleCombatMapReference singleCombatMapReference)
            => GetTileOverrides(GetOverrideTerritory(singleCombatMapReference),
                singleCombatMapReference.CombatMapNum, 0);

        /// <summary>
        ///     Gets all tile overrides by a single map location (which includes a single floor)
        /// </summary>
        /// <param name="singleMapReference"></param>
        /// <returns>a list of TileOverride object, can be empty, but never null</returns>
        public List<TileOverride> GetTileOverrides(SmallMapReferences.SingleMapReference singleMapReference)
            => GetTileOverrides(GetOverrideTerritory(singleMapReference), singleMapReference.Id,
                singleMapReference.Floor);

        private Dictionary<Point2D, TileOverride> GetTileXYOverrides(AllTerritories territory, int nMapNumber,
            int nFloor)
        {
            Dictionary<Point2D, TileOverride> tileOverrideList = new Dictionary<Point2D, TileOverride>();

            if (!TileOverrideExists(territory, nMapNumber, nFloor)) return null;

            foreach (TileOverride tileOverride in GetTileOverrides(territory, nMapNumber, nFloor))
            {
                Point2D xy = new Point2D(tileOverride.X, tileOverride.Y);
                if (tileOverrideList.ContainsKey(xy))
                    throw new Ultima5ReduxException("You have a duplicate record in TileOverrides: " +
                                                    nMapNumber + " " + xy);
                tileOverrideList.Add(xy, tileOverride);
            }

            return tileOverrideList;
        }

        public Dictionary<Point2D, TileOverride> GetTileXYOverrides(
            SmallMapReferences.SingleMapReference singleMapReference) => GetTileXYOverrides(
            GetOverrideTerritory(singleMapReference), singleMapReference.Id,
            singleMapReference.Floor);

        public Dictionary<Point2D, TileOverride>
            GetTileXYOverrides(SingleCombatMapReference singleCombatMapReference) =>
            GetTileXYOverrides(GetOverrideTerritory(singleCombatMapReference), singleCombatMapReference.CombatMapNum,
                0);

        private enum AllTerritories { Britannia, Underworld, CombatBritannia, CombatDungeon }
    }

    /// <summary>
    ///     A single overriden tile
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)] public class TileOverride
    {
        [JsonProperty] public int MapNumber;

        public bool IsOverworld => MapNumber == 0 && Z == 0;

        public bool IsUnderworld => MapNumber == 0 && Z == -1;

        public bool IsSmallMap => MapNumber != 0;

        [JsonProperty] public int X { get; set; }

        [JsonProperty] public int Y { get; set; }

        [JsonProperty] public int Z { get; set; }

        [JsonProperty] public int SpriteNum { get; set; }

        [JsonProperty] public string SpriteName { get; set; }

        [JsonProperty] public string Comment { get; set; }

        public MapUnitPosition Position => new MapUnitPosition(X, Y, Z);
    }
}