using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Ultima5Redux.Properties;

namespace Ultima5Redux.References.Maps
{
    public class TileOverrideReferences
    {
        private enum AllTerritories { Britannia, Underworld, CombatBritannia, CombatDungeon }

        private readonly Dictionary<AllTerritories,
                Dictionary<int, Dictionary<int, Dictionary<Point2D, TileOverrideReference>>>>
            _tileOverrideMap = new();

        /// <summary>
        ///     Constructor loads resource and builds tables
        /// </summary>
        public TileOverrideReferences()
        {
            string textAsset = Resources.TileOverrides;
            Dictionary<AllTerritories, List<TileOverrideReference>> tileOverrides =
                JsonConvert.DeserializeObject<Dictionary<AllTerritories, List<TileOverrideReference>>>(textAsset);

            if (tileOverrides == null)
                throw new Ultima5ReduxException("Tile overrides were mysteriously empty - check yo files!");

            // bajh: this seems like a LOT of work for little gain, but it ran very slow and very often using
            // LINQ with Lists
            foreach (AllTerritories territory in tileOverrides.Keys)
            {
                bool bFirst = true;
                foreach (TileOverrideReference tileOverrideReference in tileOverrides[territory])
                {
                    // make sure the territory is initialized
                    if (bFirst)
                    {
                        bFirst = false;
                        _tileOverrideMap.Add(territory,
                            new Dictionary<int, Dictionary<int, Dictionary<Point2D, TileOverrideReference>>>());
                    }

                    if (!_tileOverrideMap[territory].ContainsKey(tileOverrideReference.MapNumber))
                        _tileOverrideMap[territory].Add(tileOverrideReference.MapNumber,
                            new Dictionary<int, Dictionary<Point2D, TileOverrideReference>>());

                    // if it doesn't include the floor, then we add it
                    if (!_tileOverrideMap[territory][tileOverrideReference.MapNumber]
                            .ContainsKey(tileOverrideReference.Z))
                        _tileOverrideMap[territory][tileOverrideReference.MapNumber].Add(tileOverrideReference.Z,
                            new Dictionary<Point2D, TileOverrideReference>());

                    Point2D position = new(tileOverrideReference.X, tileOverrideReference.Y);
                    _tileOverrideMap[territory][tileOverrideReference.MapNumber][tileOverrideReference.Z]
                        .Add(position, tileOverrideReference);
                }
            }
        }

        private static AllTerritories GetOverrideTerritory(SingleCombatMapReference singleCombatMapReference) =>
            singleCombatMapReference.MapTerritory == SingleCombatMapReference.Territory.Britannia
                ? AllTerritories.CombatBritannia
                : AllTerritories.CombatDungeon;

        private static AllTerritories GetOverrideTerritory(SmallMapReferences.SingleMapReference singleMapReference)
        {
            if (singleMapReference.Floor == -1 && singleMapReference.MapLocation ==
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
                return AllTerritories.Underworld;
            return AllTerritories.Britannia;
        }

        private List<TileOverrideReference> GetTileOverrides(AllTerritories territory, int nMapNumber, int nFloor)
        {
            if (!TileOverrideExists(territory, nMapNumber, nFloor)) return new List<TileOverrideReference>();
            return _tileOverrideMap[territory][nMapNumber][nFloor].Values.ToList();
        }

        /// <summary>
        ///     Gets a full collection of all tile overrides for a particular territory and map
        /// </summary>
        /// <param name="territory"></param>
        /// <param name="nMapNumber"></param>
        /// <param name="nFloor"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        private Dictionary<Point2D, TileOverrideReference> GetTileXYOverrides(AllTerritories territory, int nMapNumber,
            int nFloor)
        {
            if (TileOverrideExists(territory, nMapNumber, nFloor))
                return _tileOverrideMap[territory][nMapNumber][nFloor];
            return null;
        }

        private bool TileOverrideExists(AllTerritories territory, int nMapNumber, int nFloor)
        {
            if (!_tileOverrideMap.ContainsKey(territory)) return false;
            if (!_tileOverrideMap[territory].ContainsKey(nMapNumber)) return false;
            return _tileOverrideMap[territory][nMapNumber].ContainsKey(nFloor);
        }

        public List<TileOverrideReference> GetTileOverrides(SingleCombatMapReference singleCombatMapReference) =>
            GetTileOverrides(GetOverrideTerritory(singleCombatMapReference), singleCombatMapReference.CombatMapNum, 0);

        /// <summary>
        ///     Gets all tile overrides by a single map location (which includes a single floor)
        /// </summary>
        /// <param name="singleMapReference"></param>
        /// <returns>a list of TileOverride object, can be empty, but never null</returns>
        public List<TileOverrideReference> GetTileOverrides(SmallMapReferences.SingleMapReference singleMapReference) =>
            GetTileOverrides(GetOverrideTerritory(singleMapReference), singleMapReference.Id, singleMapReference.Floor);

        public Dictionary<Point2D, TileOverrideReference> GetTileXYOverrides(
            SmallMapReferences.SingleMapReference singleMapReference) => GetTileXYOverrides(
            GetOverrideTerritory(singleMapReference), singleMapReference.Id, singleMapReference.Floor);

        public Dictionary<Point2D, TileOverrideReference> GetTileXYOverrides(
            SingleCombatMapReference singleCombatMapReference) =>
            GetTileXYOverrides(GetOverrideTerritory(singleCombatMapReference), singleCombatMapReference.CombatMapNum,
                0);
    }
}