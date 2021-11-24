using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Ultima5Redux.Properties;

namespace Ultima5Redux.References.Maps
{
    public class TileOverrideReferences
    {
        private enum AllTerritories { Britannia, Underworld, CombatBritannia, CombatDungeon }

        private readonly Dictionary<AllTerritories, List<TileOverrideReference>> _tileOverrides;

        /// <summary>
        ///     Constructor loads resource and builds tables
        /// </summary>
        public TileOverrideReferences()
        {
            string textAsset = Resources.TileOverrides;
            _tileOverrides =
                JsonConvert.DeserializeObject<Dictionary<AllTerritories, List<TileOverrideReference>>>(textAsset);
        }

        private static AllTerritories GetOverrideTerritory(SingleCombatMapReference singleCombatMapReference) =>
            singleCombatMapReference.MapTerritory == SingleCombatMapReference.Territory.Britannia
                ? AllTerritories.CombatBritannia
                : AllTerritories.CombatDungeon;

        private static AllTerritories GetOverrideTerritory(SmallMapReferences.SingleMapReference singleMapReference) =>
            singleMapReference.Floor == 0 ? AllTerritories.Britannia : AllTerritories.Underworld;

        private List<TileOverrideReference> GetTileOverrides(AllTerritories territory, int nMapNumber, int nFloor) =>
            !TileOverrideExists(territory, nMapNumber, nFloor)
                ? new List<TileOverrideReference>()
                : _tileOverrides[territory].FindAll(s => s.MapNumber == nMapNumber)
                    .Where(tile => nFloor == tile.Position.Floor).ToList();

        public List<TileOverrideReference> GetTileOverrides(SingleCombatMapReference singleCombatMapReference) =>
            GetTileOverrides(GetOverrideTerritory(singleCombatMapReference), singleCombatMapReference.CombatMapNum, 0);

        /// <summary>
        ///     Gets all tile overrides by a single map location (which includes a single floor)
        /// </summary>
        /// <param name="singleMapReference"></param>
        /// <returns>a list of TileOverride object, can be empty, but never null</returns>
        public List<TileOverrideReference> GetTileOverrides(SmallMapReferences.SingleMapReference singleMapReference) =>
            GetTileOverrides(GetOverrideTerritory(singleMapReference), singleMapReference.Id, singleMapReference.Floor);

        private Dictionary<Point2D, TileOverrideReference> GetTileXYOverrides(AllTerritories territory, int nMapNumber,
            int nFloor)
        {
            Dictionary<Point2D, TileOverrideReference> tileOverrideList =
                new Dictionary<Point2D, TileOverrideReference>();

            if (!TileOverrideExists(territory, nMapNumber, nFloor)) return null;

            foreach (TileOverrideReference tileOverride in GetTileOverrides(territory, nMapNumber, nFloor))
            {
                Point2D xy = new Point2D(tileOverride.X, tileOverride.Y);
                if (tileOverrideList.ContainsKey(xy))
                    throw new Ultima5ReduxException("You have a duplicate record in TileOverrides: " + nMapNumber +
                                                    " " + xy);
                tileOverrideList.Add(xy, tileOverride);
            }

            return tileOverrideList;
        }

        public Dictionary<Point2D, TileOverrideReference> GetTileXYOverrides(
            SmallMapReferences.SingleMapReference singleMapReference) => GetTileXYOverrides(
            GetOverrideTerritory(singleMapReference), singleMapReference.Id, singleMapReference.Floor);

        public Dictionary<Point2D, TileOverrideReference> GetTileXYOverrides(
            SingleCombatMapReference singleCombatMapReference) =>
            GetTileXYOverrides(GetOverrideTerritory(singleCombatMapReference), singleCombatMapReference.CombatMapNum,
                0);

        private bool TileOverrideExists(AllTerritories territory, int nMapNumber, int nFloor)
        {
            if (!_tileOverrides.ContainsKey(territory)) return false;
            if (!_tileOverrides[territory].Exists(s => s.MapNumber == nMapNumber)) return false;
            return (_tileOverrides[territory].FindAll(s => s.MapNumber == nMapNumber)
                .FirstOrDefault(tileOverride => nFloor == tileOverride.Position.Floor) != null);
        }
    }
}