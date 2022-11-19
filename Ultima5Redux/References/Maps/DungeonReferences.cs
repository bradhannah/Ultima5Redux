using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References
{
    public class DungeonReferences
    {
        private readonly SingleDungeonMapReference[] _dungeons =
            new SingleDungeonMapReference[SingleDungeonMapReference.N_DUNGEONS];

        private const int N_FIRST_DUNGEON_LOCATION_INT = (int)SmallMapReferences.SingleMapReference.Location.Deceit;
        private const int N_LAST_DUNGEON_LOCATION_INT = (int)SmallMapReferences.SingleMapReference.Location.Doom;

        public SingleDungeonMapReference GetDungeon(SmallMapReferences.SingleMapReference.Location location)
        {
            if ((int)location < N_FIRST_DUNGEON_LOCATION_INT || (int)location > N_LAST_DUNGEON_LOCATION_INT)
            {
                throw new Ultima5ReduxException(
                    $"Requested dungeon with location {location}, which is out of range and not a dungeon");
            }

            return _dungeons[(int)location - N_FIRST_DUNGEON_LOCATION_INT];
        }

        public DungeonReferences(string legacyDataDatFilePath)
        {
            byte[] dungeonDatContents;
            string dungeonDatFilePath = Path.Combine(legacyDataDatFilePath, FileConstants.DUNGEON_DAT);
            try
            {
                dungeonDatContents = File.ReadAllBytes(dungeonDatFilePath);
            } catch (Exception e)
            {
                throw new Ultima5ReduxException("Error opening and reading dungeon.dat\n" + e);
            }

            List<byte> dungeonDataListContents = dungeonDatContents.ToList();

            for (int nDungeon = 0; nDungeon < SingleDungeonMapReference.N_DUNGEONS; nDungeon++)
            {
                int nOffset = nDungeon * SingleDungeonMapReference.N_BYTES_PER_DUNGEON;
                List<byte> rawDungeon =
                    dungeonDataListContents.GetRange(nOffset, SingleDungeonMapReference.N_BYTES_PER_DUNGEON);
                var singleDungeonMapReference =
                    new SingleDungeonMapReference(rawDungeon.ToArray());
                _dungeons[nDungeon] = singleDungeonMapReference;
            }
        }
    }
}