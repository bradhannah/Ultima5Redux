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
        public SingleCombatMapReference TheCombatMapReference { get; }

        // world references
        private readonly TileReferences _tileReferences;
        private readonly EnemyReferences _enemyReferences;

        // player character information
        private PlayerCharacterRecord _activePlayerCharacterRecord;
        private PlayerCharacterRecords _playerCharacterRecords;

        private Queue<CombatMapUnit> _initiativeQueue = new Queue<CombatMapUnit>();

        private void CalculateInitiativeQueue()
        {
            Debug.Assert(_playerCharacterRecords != null);
            Debug.Assert(_initiativeQueue.Count == 0);
            
            //_enemyReferences.AllEnemyReferences[0].
        }
        

        public CombatMap(SingleCombatMapReference singleCombatCombatMapReference, TileReferences tileReferences, EnemyReferences enemyReferences) : 
            base(null, null)
        {
            _tileReferences = tileReferences;
            TheCombatMapReference = singleCombatCombatMapReference;
            _enemyReferences = enemyReferences;
        }

        public override int NumOfXTiles => SingleCombatMapReference.XTILES;
        public override int NumOfYTiles => SingleCombatMapReference.YTILES;

        public override byte[][] TheMap {
            get => TheCombatMapReference.TheMap;
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
            _playerCharacterRecords = activeRecords;
            
            // clear any previous combat map units
            currentVirtualMap.TheMapUnits.InitializeCombatMapReferences();
            List<Point2D> playerStartPositions =
                TheCombatMapReference.GetPlayerStartPositions(entryDirection);
            
            // cycle through each player and make a map unit
            for (int nPlayer = 0; nPlayer < activeRecords.GetNumberOfActiveCharacters(); nPlayer++)
            {
                PlayerCharacterRecord record = activeRecords.Records[nPlayer];

                CombatPlayer combatPlayer = new CombatPlayer(record, _tileReferences, 
                    playerStartPositions[nPlayer]);
                currentVirtualMap.TheMapUnits.CurrentMapUnits[nPlayer] = combatPlayer;
            }
        }

        private void CreateEnemy(int nEnemyIndex, VirtualMap currentVirtualMap,
            SingleCombatMapReference singleCombatMapReference,
            EnemyReference enemyReference)
        {
            SingleCombatMapReference.CombatMapSpriteType combatMapSpriteType = 
                singleCombatMapReference.GetAdjustedEnemySprite(nEnemyIndex, out int nEnemySprite);
            Point2D nEnemyPosition = singleCombatMapReference.GetEnemyPosition(nEnemyIndex);

            switch (combatMapSpriteType)
            {
                case SingleCombatMapReference.CombatMapSpriteType.Nothing:
                    Debug.Assert(nEnemyPosition.X == 0 && nEnemyPosition.Y ==0);
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.Thing:
                    Debug.WriteLine("It's a chest or maybe a dead body!");
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.AutoSelected:
                    currentVirtualMap.TheMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
                        _enemyReferences.GetEnemyReference(nEnemySprite), out int _);
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.EncounterBased:
                    Debug.Assert(!(nEnemyPosition.X == 0 && nEnemyPosition.Y == 0));
                    currentVirtualMap.TheMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
                        enemyReference, out int _);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// Creates enemies in the combat map. If the map contains hard coded enemies then it will ignore the
        /// specified enemies
        /// </summary>
        /// <param name="currentVirtualMap"></param>
        /// <param name="singleCombatMapReference"></param>
        /// <param name="entryDirection"></param>
        /// <param name="primaryEnemyReference"></param>
        /// <param name="nPrimaryEnemies"></param>
        /// <param name="secondaryEnemyReference"></param>
        /// <param name="nSecondaryEnemies"></param>
        /// <param name="avatarRecord"></param>
        internal void CreateEnemies(VirtualMap currentVirtualMap,
            SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection,
            EnemyReference primaryEnemyReference, int nPrimaryEnemies, 
            EnemyReference secondaryEnemyReference, int nSecondaryEnemies,
            PlayerCharacterRecord avatarRecord)
        {
            int nEnemyIndex = 0;

            // dungeons do not have encountered based enemies (but where are the dragons???)
            if (singleCombatMapReference.MapTerritory == SingleCombatMapReference.Territory.Dungeon)
            {
                for (nEnemyIndex = 0; nEnemyIndex < SingleCombatMapReference.NUM_ENEMIES; nEnemyIndex++)
                {
                    CreateEnemy(nEnemyIndex, currentVirtualMap, singleCombatMapReference, null);
                }

                return;
            }

            Queue<int> monsterIndex = Utils.CreateRandomizedIntegerQueue(SingleCombatMapReference.NUM_ENEMIES);
            
            for (int nIndex = 0; nIndex < nPrimaryEnemies; nIndex++, nEnemyIndex++)
            {
                CreateEnemy(monsterIndex.Dequeue(), currentVirtualMap, singleCombatMapReference, primaryEnemyReference);
            }
            for (int nIndex = 0; nIndex < nSecondaryEnemies; nIndex++, nEnemyIndex++)
            {
                CreateEnemy(monsterIndex.Dequeue(), currentVirtualMap, singleCombatMapReference, secondaryEnemyReference);
            }
        }
    }
}