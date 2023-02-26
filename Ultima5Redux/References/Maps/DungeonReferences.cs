using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ultima5Redux.References.Maps
{
    public class DungeonReferences
    {
        private const int N_FIRST_DUNGEON_LOCATION_INT = (int)SmallMapReferences.SingleMapReference.Location.Deceit;
        private const int N_LAST_DUNGEON_LOCATION_INT = (int)SmallMapReferences.SingleMapReference.Location.Doom;

        private readonly DungeonMapReference[] _dungeons =
            new DungeonMapReference[DungeonMapReference.N_DUNGEONS];

        public IReadOnlyList<DungeonMapReference> DungeonMapReferences => _dungeons;

        public DungeonReferences(string legacyDataDatFilePath)
        {
            byte[] dungeonDatContents;
            string dungeonDatFilePath = Path.Combine(legacyDataDatFilePath, FileConstants.DUNGEON_DAT);
            try
            {
                dungeonDatContents = File.ReadAllBytes(dungeonDatFilePath);
            }
            catch (Exception e)
            {
                throw new Ultima5ReduxException("Error opening and reading dungeon.dat\n" + e);
            }

            List<byte> dungeonDataListContents = dungeonDatContents.ToList();

            for (int nDungeon = 0; nDungeon < DungeonMapReference.N_DUNGEONS; nDungeon++)
            {
                int nOffset = nDungeon * DungeonMapReference.N_BYTES_PER_DUNGEON_FLOOR *
                              DungeonMapReference.N_DUNGEON_FLOORS_PER_MAP;
                List<byte> rawDungeon =
                    dungeonDataListContents.GetRange(nOffset, DungeonMapReference.N_BYTES_PER_DUNGEON);
                var singleDungeonMapReference = new DungeonMapReference(
                    (SmallMapReferences.SingleMapReference.Location)N_FIRST_DUNGEON_LOCATION_INT + nDungeon,
                    rawDungeon);
                _dungeons[nDungeon] = singleDungeonMapReference;
            }
        }

        public DungeonMapReference GetDungeon(SmallMapReferences.SingleMapReference.Location location)
        {
            if ((int)location < N_FIRST_DUNGEON_LOCATION_INT || (int)location > N_LAST_DUNGEON_LOCATION_INT)
            {
                throw new Ultima5ReduxException(
                    $"Requested dungeon with location {location}, which is out of range and not a dungeon");
            }

            return _dungeons[(int)location - N_FIRST_DUNGEON_LOCATION_INT];
        }
    }
}