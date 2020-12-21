using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.Monsters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.Maps
{
    public class CombatMap : Map
    {
        private readonly TileReferences _tileReferences;
        public SingleCombatMapReference TheMapReference { get; }

        private EnemyReferences _enemyReferences;

        public CombatMap(SingleCombatMapReference singleCombatMapReference, TileReferences tileReferences, EnemyReferences enemyReferences) : 
            base(null, null)
        {
            _tileReferences = tileReferences;
            TheMapReference = singleCombatMapReference;
            _enemyReferences = enemyReferences;
        }

        public override int NumOfXTiles => SingleCombatMapReference.XTILES;
        public override int NumOfYTiles => SingleCombatMapReference.YTILES;

        public override byte[][] TheMap {
            get => TheMapReference.TheMap;
            protected set
            {
                
            }
        }
        
        protected override float GetAStarWeight(TileReferences spriteTileReferences, Point2D xy)
        {
            return 1.0f;
        }
        
        internal void CreateParty(VirtualMap currentVirtualMap, SingleCombatMapReference.EntryDirection entryDirection,
            PlayerCharacterRecords activeRecords)
        {
            // clear any previous combat map units
            currentVirtualMap.TheMapUnits.InitializeCombatMapReferences();
            List<Point2D> playerStartPositions =
                TheMapReference.GetPlayerStartPositions(entryDirection);
            
            // cycle through each player and make a map unit
            for (int nPlayer = 0; nPlayer < activeRecords.GetNumberOfActiveCharacters(); nPlayer++)
            {
                PlayerCharacterRecord record = activeRecords.Records[nPlayer];

                CombatPlayer combatPlayer = new CombatPlayer(record, _tileReferences, 
                    playerStartPositions[nPlayer]);
                currentVirtualMap.TheMapUnits.CurrentMapUnits[nPlayer] = combatPlayer;
            }
        }

        internal void CreateEnemies(VirtualMap currentVirtualMap,
            SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection,
            EnemyReference primaryEnemyReference, EnemyReference secondaryEnemyReference,
            PlayerCharacterRecord avatarRecord)
        {
            int nPrimaryEnemy = 16;
            int nSecondaryEnemy = 2;
            
            for (int nIndex = 0; nIndex < nPrimaryEnemy; nIndex++)
            {
                SingleCombatMapReference.CombatMapSpriteType combatMapSpriteType = 
                    singleCombatMapReference.GetAdjustedEnemySprite(nIndex, out int nEnemySprite);
                Point2D nEnemyPosition = singleCombatMapReference.GetEnemyPosition(nIndex);

                switch (combatMapSpriteType)
                {
                    case SingleCombatMapReference.CombatMapSpriteType.Nothing:
                        Debug.Assert(nEnemyPosition.X == 0 && nEnemyPosition.Y ==0);
                        break;
                    case SingleCombatMapReference.CombatMapSpriteType.Thing:
                        Debug.WriteLine("It's a chest or maybe a dead body!");
                        break;
                    case SingleCombatMapReference.CombatMapSpriteType.AutoSelected:
                        currentVirtualMap.TheMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nIndex),
                            _enemyReferences.GetEnemyReference(nEnemySprite), out int _);
                        break;
                    case SingleCombatMapReference.CombatMapSpriteType.EncounterBased:
                        Debug.Assert(!(nEnemyPosition.X == 0 && nEnemyPosition.Y == 0));
                        currentVirtualMap.TheMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nIndex),
                            primaryEnemyReference, out int _);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
        }
    }
}