using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.MapUnits;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.Maps
{
    public class CombatMap : Map
    {
        private readonly SingleCombatMapReference _singleCombatMapReference;

        public SingleCombatMapReference TheMapReference => _singleCombatMapReference;

        public CombatMap(SingleCombatMapReference singleCombatMapReference) : base(null, null)
        {
            _singleCombatMapReference = singleCombatMapReference;
        }

        public override int NumOfXTiles => SingleCombatMapReference.XTILES;
        public override int NumOfYTiles => SingleCombatMapReference.YTILES;

        public override byte[][] TheMap {
            get => _singleCombatMapReference.TheMap;
            protected set
            {
                
            }
        }
        
        protected override float GetAStarWeight(TileReferences spriteTileReferences, Point2D xy)
        {
            return 1.0f;
        }
        
        internal void CreateParty(VirtualMap currentVirtualMap, SingleCombatMapReference.EntryDirection entryDirection,
            PlayerCharacterRecords activeRecords, TileReferences tileReferences)
        {
            //Debug.Assert(UltimaGlobal.Ultima5World.State.TheVirtualMap.CurrentMap is CombatMap);
            //CombatMap combatMap = (CombatMap)UltimaGlobal.Ultima5World.State.TheVirtualMap.CurrentMap;
            // clear any previous combat map units
            currentVirtualMap.TheMapUnits.InitializeCombatMapReferences();
            List<Point2D> playerStartPositions =
                TheMapReference.GetPlayerStartPositions(entryDirection);
            
            // cycle through each player and make a map unit
            //List<PlayerCharacterRecord> list = activeRecords;//UltimaGlobal.Ultima5World.State.CharacterRecords.GetActiveCharacterRecords();
            for (int nPlayer = 0; nPlayer < activeRecords.GetNumberOfActiveCharacters(); nPlayer++)
            {
                PlayerCharacterRecord record = activeRecords.Records[nPlayer];

                CombatPlayer combatPlayer = new CombatPlayer(record, tileReferences, 
                    playerStartPositions[nPlayer]);
                currentVirtualMap.TheMapUnits.CurrentMapUnits[nPlayer] = combatPlayer;
            }
        }
    }
}