using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        private readonly VirtualMap _virtualMap;
        private readonly TileReferences _tileReferences;
        private readonly EnemyReferences _enemyReferences;

        // player character information
        private PlayerCharacterRecord _activePlayerCharacterRecord;
        private PlayerCharacterRecords _playerCharacterRecords;
        private MapUnits.MapUnits CombatMapUnits { get; }
        
        private readonly Queue<CombatMapUnit> _initiativeQueue = new Queue<CombatMapUnit>();
        private readonly Dictionary<CombatMapUnit, int> _combatInitiativeTally = new Dictionary<CombatMapUnit, int>();

        private int _nLowestDexterity;
        private int _nHighestDexterity;
        
        /// <summary>
        /// Clears all queues and tallies, and populates with new data
        /// </summary>
        internal void InitializeInitiativeQueue()
        {
            Debug.Assert(_virtualMap.LargeMapOverUnder == Maps.Combat);
            _initiativeQueue.Clear();
            _combatInitiativeTally.Clear();
            _nLowestDexterity = 50;
            _nHighestDexterity = 0;

            foreach (MapUnit mapUnit in CombatMapUnits.CurrentMapUnits)
            {
                if (!IsCombatMapUnit(mapUnit)) continue;
                
                byte nDexterity = GetDexterity(mapUnit);

                // get the highest and lowest dexterity values to be used in ongoing tally
                if (_nLowestDexterity > nDexterity) _nLowestDexterity = nDexterity;
                if (_nHighestDexterity < nDexterity) _nHighestDexterity = nDexterity;

                _combatInitiativeTally.Add((CombatMapUnit)mapUnit, 0);
            }
        }

        private byte GetDexterity(MapUnit mapUnit)
        {
            byte nDexterity;
            switch (mapUnit)
            {
                case CombatPlayer playerUnit:
                    nDexterity = (byte)playerUnit.Record.Stats.Dexterity;
                    break;
                case Enemy enemyUnit:
                    nDexterity = (byte) enemyUnit.EnemyReference.TheDefaultEnemyStats.Dexterity;
                    break;
                default: throw new Ultima5ReduxException("Tried to get dexterity from a non combat unit");
            }

            return nDexterity;
        }

        private bool IsCombatMapUnit(MapUnit mapUnit)
        {
            switch (mapUnit)
            {
                case CombatPlayer playerUnit:
                case Enemy enemyUnit:
                    return true;
            }

            return false;
        }

        internal void CalculateInitiativeQueue()
        {
            Debug.Assert(_playerCharacterRecords != null);
            Debug.Assert(_initiativeQueue.Count == 0);

            // a mapping of dexterity values to an ordered list of combat map units 
            Dictionary<int, List<CombatMapUnit>> dexterityToCombatUnits = new Dictionary<int, List<CombatMapUnit>>();
            
            // go through each combat map unit and place them in priority order based on their dexterity values 
            foreach (MapUnit mapUnit in CombatMapUnits.CurrentMapUnits)
            {
                if (!IsCombatMapUnit(mapUnit)) continue;

                CombatMapUnit combatMapUnit = (CombatMapUnit) mapUnit;
                
                int nDexterity = GetDexterity(mapUnit);
                int nTally = _combatInitiativeTally[combatMapUnit];

                // initiative is determined by the map units dexterity + the accumulated dexterity thus far
                int nInitiative = nDexterity + nTally;

                void addToDexterityToCombatUnits(int nInitiativeIndex, CombatMapUnit combatMapUnitToAdd)
                {
                    if (!dexterityToCombatUnits.ContainsKey(nInitiativeIndex))
                        dexterityToCombatUnits.Add(nInitiativeIndex, new List<CombatMapUnit>());
                    
                    dexterityToCombatUnits[nInitiativeIndex].Add(combatMapUnitToAdd);
                }
                
                // our stored initiative tally cannot exceed the highest dexterity 
                if (nInitiative > _nLowestDexterity)
                {
                    _combatInitiativeTally[combatMapUnit] = nInitiative % _nLowestDexterity;

                    // if you have exceeded the dexterity count and get a free hit this round
                    addToDexterityToCombatUnits(nInitiative - _nLowestDexterity, combatMapUnit);
                }
                else
                {
                    _combatInitiativeTally[combatMapUnit] = nInitiative;
                    // add the combat map unit to working dexterity map based on the modded (smaller) initiative value
                }

                // this hit will use the larger initiative value giving you first in line access to attack
                addToDexterityToCombatUnits(nInitiative, combatMapUnit);
            }

            // now that we have the order of all attacks, let's put them in a FIFO queue
            foreach (int nInitiative in dexterityToCombatUnits.Keys.OrderByDescending(initiative => initiative))
            {
                foreach (CombatMapUnit combatMapUnit in dexterityToCombatUnits[nInitiative])
                {
                    _initiativeQueue.Enqueue(combatMapUnit);
                }
            }
        }
        

        public CombatMap(VirtualMap virtualMap, SingleCombatMapReference singleCombatCombatMapReference, TileReferences tileReferences, EnemyReferences enemyReferences) : 
            base(null, null)
        {
            _virtualMap = virtualMap;
            CombatMapUnits = _virtualMap.TheMapUnits;
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
        
        internal void CreateParty(SingleCombatMapReference.EntryDirection entryDirection,
            PlayerCharacterRecords activeRecords)
        {
            _playerCharacterRecords = activeRecords;
            
            // clear any previous combat map units
            CombatMapUnits.InitializeCombatMapReferences();
            
            List<Point2D> playerStartPositions =
                TheCombatMapReference.GetPlayerStartPositions(entryDirection);
            
            // cycle through each player and make a map unit
            for (int nPlayer = 0; nPlayer < activeRecords.GetNumberOfActiveCharacters(); nPlayer++)
            {
                PlayerCharacterRecord record = activeRecords.Records[nPlayer];

                CombatPlayer combatPlayer = new CombatPlayer(record, _tileReferences, 
                    playerStartPositions[nPlayer]);
                CombatMapUnits.CurrentMapUnits[nPlayer] = combatPlayer;
            }
        }

        private void CreateEnemy(int nEnemyIndex, 
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
                    CombatMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
                        _enemyReferences.GetEnemyReference(nEnemySprite), out int _);
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.EncounterBased:
                    Debug.Assert(!(nEnemyPosition.X == 0 && nEnemyPosition.Y == 0));
                    CombatMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
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
        /// <param name="singleCombatMapReference"></param>
        /// <param name="entryDirection"></param>
        /// <param name="primaryEnemyReference"></param>
        /// <param name="nPrimaryEnemies"></param>
        /// <param name="secondaryEnemyReference"></param>
        /// <param name="nSecondaryEnemies"></param>
        /// <param name="avatarRecord"></param>
        internal void CreateEnemies( SingleCombatMapReference singleCombatMapReference,
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
                    CreateEnemy(nEnemyIndex, singleCombatMapReference, null);
                }

                return;
            }

            Queue<int> monsterIndex = Utils.CreateRandomizedIntegerQueue(SingleCombatMapReference.NUM_ENEMIES);
            
            for (int nIndex = 0; nIndex < nPrimaryEnemies; nIndex++, nEnemyIndex++)
            {
                CreateEnemy(monsterIndex.Dequeue(), singleCombatMapReference, primaryEnemyReference);
            }
            for (int nIndex = 0; nIndex < nSecondaryEnemies; nIndex++, nEnemyIndex++)
            {
                CreateEnemy(monsterIndex.Dequeue(), singleCombatMapReference, secondaryEnemyReference);
            }
        }
    }
}