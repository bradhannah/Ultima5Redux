using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.Monsters;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.Maps
{
    public class CombatMap : Map
    {
        public SingleCombatMapReference TheCombatMapReference { get; }
        
        // world references
        private readonly VirtualMap _virtualMap;
        private readonly TileReferences _tileReferences;
        private readonly EnemyReferences _enemyReferences;
        private readonly InventoryReferences _inventoryReferences;
        private readonly Inventory _inventory;
        private readonly DataOvlReference _dataOvlReference;
        
        private InitiativeQueue _initiativeQueue; 

        public override int NumOfXTiles => SingleCombatMapReference.XTILES;
        public override int NumOfYTiles => SingleCombatMapReference.YTILES;

        public override byte[][] TheMap {
            get => TheCombatMapReference.TheMap;
            protected set { }
        }

        public int Turn => _initiativeQueue.Turn;
        public int Round => _initiativeQueue.Round;

        public int NumberOfEnemies => CombatMapUnits.CurrentMapUnits.OfType<Enemy>().Count();
        public int NumberOfVisiblePlayers => CombatMapUnits.CurrentMapUnits.OfType<CombatPlayer>().Count(combatPlayer => combatPlayer.IsActive);

        public bool AreEnemiesLeft => NumberOfEnemies > 0;

        public bool InEscapeMode { get; set; } = false;

        protected override bool IsRepeatingMap => false;
        
        public override bool ShowOuterSmallMapTiles => false;

        // player character information

        /// <summary>
        /// Current player or enemy who is active in current round
        /// </summary>
        public PlayerCharacterRecord CurrentPlayerCharacterRecord { get; private set; }

        public CombatPlayer CurrentCombatPlayer => _initiativeQueue.GetCurrentCombatUnit() is CombatPlayer player ? player : null;

        public Enemy ActiveEnemy => _initiativeQueue.GetCurrentCombatUnit() is Enemy enemy ? enemy : null;
        
        /// <summary> 
        /// All current player characters
        /// </summary>
        private PlayerCharacterRecords _playerCharacterRecords;
        
        /// <summary>
        /// Current combat map units for current combat map
        /// </summary>
        private MapUnits.MapUnits CombatMapUnits { get; }

        public enum TurnResult { RequireCharacterInput, EnemyMoved, EnemyAttacks }

        public List<CombatPlayer> AllCombatPlayers => CombatMapUnits.CurrentMapUnits.OfType<CombatPlayer>().ToList();
        
        /// <summary>
        /// Creates CombatMap.
        /// Note: Does not initialize the combat map units.
        /// </summary>
        /// <param name="virtualMap"></param>
        /// <param name="singleCombatCombatMapReference"></param>
        /// <param name="tileReferences"></param>
        /// <param name="enemyReferences"></param>
        /// <param name="inventoryReferences"></param>
        /// <param name="inventory"></param>
        /// <param name="dataOvlReference"></param>
        public CombatMap(VirtualMap virtualMap, SingleCombatMapReference singleCombatCombatMapReference, TileReferences tileReferences, EnemyReferences enemyReferences, 
            InventoryReferences inventoryReferences, Inventory inventory, DataOvlReference dataOvlReference) : 
            base(null, null, tileReferences)
        {
            _virtualMap = virtualMap;
            CombatMapUnits = _virtualMap.TheMapUnits;
            _tileReferences = tileReferences;
            TheCombatMapReference = singleCombatCombatMapReference;
            _enemyReferences = enemyReferences;
            _inventoryReferences = inventoryReferences;
            _inventory = inventory;
            _dataOvlReference = dataOvlReference;
        }
        
        /// <summary>
        /// Attempts to processes the turn of the current combat unit - either CombatPlayer or Enemy.
        /// Can result in advancing to next turn, or indicate user input required
        /// </summary>
        /// <param name="affectedCombatMapUnit"></param>
        /// <param name="outputStr"></param>
        /// <returns></returns>
        public TurnResult ProcessMapUnitTurn(out CombatMapUnit affectedCombatMapUnit, out string outputStr)
        {
            affectedCombatMapUnit = _initiativeQueue.GetCurrentCombatUnit();

            if (affectedCombatMapUnit is CombatPlayer combatPlayer)
            {
                outputStr = combatPlayer.Record.Name + ", armed with " + combatPlayer.GetAttackWeaponsString();  
           
                return TurnResult.RequireCharacterInput;
            }

            // Everything after is ENEMY logic!
            // either move the ENEMY or have them attack someone
            Debug.Assert(affectedCombatMapUnit is Enemy);
            Enemy enemy = affectedCombatMapUnit as Enemy;

            // if enemy is within range of someone, 
            CombatMapUnit bestCombatPlayer = enemy?.PreviousAttackTarget ?? GetClosestCombatPlayerInRange(enemy);
            
            // we determine if the best combat player is close enough to attack or not
            bool bIsAttackable = bestCombatPlayer?.IsAttackable ?? false;
            bool bIsReachable = bestCombatPlayer != null && enemy.CanReachForAttack(bestCombatPlayer);
            
            Debug.Assert(bestCombatPlayer?.IsAttackable ?? true);
            
            if (bIsAttackable && bIsReachable)
            {
                enemy.Attack(bestCombatPlayer, enemy.EnemyReference.TheDefaultEnemyStats.Damage,
                    out outputStr);
                AdvanceToNextCombatMapUnit();
                return TurnResult.EnemyAttacks;
            }

            outputStr = enemy.EnemyReference.MixedCaseSingularName + " moved.";
            
            enemy.MoveToClosestAttackableCombatMapUnit(enemy);
            
            AdvanceToNextCombatMapUnit();
            return TurnResult.EnemyMoved;
        }
        
        /// <summary>
        /// Moves the active combat unit to a new map location
        /// No additional logic is computed.
        /// </summary>
        /// <param name="xy"></param>
        public void MoveActiveCombatMapUnit(Point2D xy)
        {
            CombatMapUnit currentCombatUnit = _initiativeQueue.GetCurrentCombatUnit();
            currentCombatUnit.MapUnitPosition = new MapUnitPosition(xy.X, xy.Y, 0);
        }

        public void KillCombatMapUnit(CombatMapUnit combatMapUnit) => combatMapUnit.Stats.CurrentHp = 0;

        public CombatPlayer GetCombatPlayer(PlayerCharacterRecord record) => 
            CombatMapUnits.CurrentMapUnits.OfType<CombatPlayer>().FirstOrDefault(player => player.Record == record);

        public void AdvanceToNextCombatMapUnit()
        {
            CombatMapUnit combatMapUnit = _initiativeQueue.AdvanceToNextCombatMapUnit();
            
            CurrentPlayerCharacterRecord = combatMapUnit is CombatPlayer player ? player.Record : null;
        } 
        
        public List<CombatMapUnit> GetTopNCombatMapUnits(int nUnits)
        {
            // if there aren't the minimum number of turns, then we force it to add additional turns to at least
            // the stated number of units
            if (_initiativeQueue.TotalTurnsInQueue < nUnits)
                _initiativeQueue.CalculateNextInitiativeQueue();
            
            return _initiativeQueue.GetTopNCombatMapUnits(nUnits);
        }

        private int GetCombatMapUnitIndex(CombatMapUnit combatMapUnit)
        {
            if (combatMapUnit == null) return -1;
            for (int i = 0; i < CombatMapUnits.CurrentMapUnits.Count(); i++)
            {
                if (CombatMapUnits.CurrentMapUnits[i] == combatMapUnit) return i;
            }

            return -1;
        }

        public void SetActivePlayerCharacter(PlayerCharacterRecord record) => _initiativeQueue.SetActivePlayerCharacter(record);

        public Enemy GetFirstEnemy(CombatItem combatItem) => GetNextEnemy(null, combatItem);

        private CombatPlayer GetClosestCombatPlayerInRange(Enemy enemy)
        {
            int nMapUnits = CombatMapUnits.CurrentMapUnits.Count();

            double dBestDistanceToAttack = 150f;
            CombatPlayer bestCombatPlayer = null;
            
            for (int nIndex = 0; nIndex < nMapUnits; nIndex++)
            {
                if (!(CombatMapUnits.CurrentMapUnits[nIndex] is CombatPlayer combatPlayer)) continue;
                if (!enemy.CanReachForAttack(combatPlayer, enemy.EnemyReference.AttackRange)) continue;
                
                double dDistance = enemy.MapUnitPosition.XY.DistanceBetween(combatPlayer.MapUnitPosition.XY);
                if (!(dDistance < dBestDistanceToAttack)) continue;

                dBestDistanceToAttack = dDistance;
                bestCombatPlayer = combatPlayer;
            }
            
            return bestCombatPlayer;
        }
        
        public Enemy GetClosestEnemyInRange(CombatItem combatItem)
        {
            int nMapUnits = CombatMapUnits.CurrentMapUnits.Count();

            double dBestDistanceToAttack = 150f;
            Enemy bestEnemy = null;
            
            for (int nIndex = 0; nIndex < nMapUnits; nIndex++)
            {
                if (!(CombatMapUnits.CurrentMapUnits[nIndex] is Enemy enemy)) continue;
                if (!CurrentCombatPlayer.CanReachForAttack(enemy, combatItem)) continue;
                
                double dDistance = enemy.MapUnitPosition.XY.DistanceBetween(CurrentCombatPlayer.MapUnitPosition.XY);
                if (!(dDistance < dBestDistanceToAttack)) continue;

                dBestDistanceToAttack = dDistance;
                bestEnemy = enemy;
            }
            
            return bestEnemy;
        }
        
        public Enemy GetNextEnemy(Enemy currentEnemy, CombatItem combatItem)
        {
            int nOffset = GetCombatMapUnitIndex(currentEnemy);
            // -1 indicates it wasn't found, so could be dead or null. We set it to the beginning
            if (nOffset == -1) nOffset = 0;
            
            int nMapUnits = CombatMapUnits.CurrentMapUnits.Count();

            for (int i = 0; i < nMapUnits; i++)
            {
                // we start at the next position, and wrap around ensuring we have hit all possible enemies
                int nIndex = (i + nOffset + 1) % nMapUnits;
                if (!(CombatMapUnits.CurrentMapUnits[nIndex] is Enemy enemy)) continue;
                
                if (CurrentCombatPlayer.CanReachForAttack(enemy, combatItem)) return enemy;
            }
            return null;
        }

        internal void InitializeInitiativeQueue()
        {
            _initiativeQueue = new InitiativeQueue(CombatMapUnits, _playerCharacterRecords);
            _initiativeQueue.InitializeInitiativeQueue();
        }



        protected override float GetAStarWeight(Point2D xy)
        {
            return 1.0f;
        }
        
        /// <summary>
        /// Creates a party in the context of the combat map
        /// </summary>
        /// <param name="entryDirection">which direction did they enter from?</param>
        /// <param name="activeRecords">all character records</param>
        internal void CreateParty(SingleCombatMapReference.EntryDirection entryDirection, PlayerCharacterRecords activeRecords)
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
                    playerStartPositions[nPlayer], _dataOvlReference, _inventory);
                CombatMapUnits.CurrentMapUnits[nPlayer] = combatPlayer;
            }
        }

        /// <summary>
        /// Creates a single enemy in the context of the combat map.
        /// </summary>
        /// <param name="nEnemyIndex">0 based index that reflects the combat maps enemy index list</param>
        /// <param name="singleCombatMapReference">reference of the combat map</param>
        /// <param name="enemyReference">reference to enemy to be added (ignored for auto selected enemies)</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void CreateEnemy(int nEnemyIndex, SingleCombatMapReference singleCombatMapReference,
            EnemyReference enemyReference, NonPlayerCharacterReference npcRef)
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
                        _enemyReferences.GetEnemyReference(nEnemySprite), out int _, npcRef);
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.EncounterBased:
                    Debug.Assert(!(nEnemyPosition.X == 0 && nEnemyPosition.Y == 0));
                    CombatMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
                        enemyReference, out int _, npcRef);
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
        /// <param name="npcRef"></param>
        internal void CreateEnemies( SingleCombatMapReference singleCombatMapReference,
            SingleCombatMapReference.EntryDirection entryDirection,
            EnemyReference primaryEnemyReference, int nPrimaryEnemies, 
            EnemyReference secondaryEnemyReference, int nSecondaryEnemies,
            PlayerCharacterRecord avatarRecord, NonPlayerCharacterReference npcRef)
        {
            int nEnemyIndex = 0;

            // dungeons do not have encountered based enemies (but where are the dragons???)
            if (singleCombatMapReference.MapTerritory == SingleCombatMapReference.Territory.Dungeon)
            {
                for (nEnemyIndex = 0; nEnemyIndex < SingleCombatMapReference.NUM_ENEMIES; nEnemyIndex++)
                {
                    CreateEnemy(nEnemyIndex, singleCombatMapReference, primaryEnemyReference, npcRef);
                }

                return;
            }
            
            if (npcRef != null) Debug.Assert(nPrimaryEnemies == 1 && nSecondaryEnemies == 0 );

            // if there is only a single enemy then we always give them first position (such as NPC fights)
            if (nPrimaryEnemies == 1 && nSecondaryEnemies == 0)
            {
                CreateEnemy(0, singleCombatMapReference, primaryEnemyReference, npcRef);
                return;
            }

            // for regular combat maps, we introduce some randomness 
            Queue<int> monsterIndex = Utils.CreateRandomizedIntegerQueue(SingleCombatMapReference.NUM_ENEMIES);
            
            for (int nIndex = 0; nIndex < nPrimaryEnemies; nIndex++, nEnemyIndex++)
            {
                CreateEnemy(monsterIndex.Dequeue(), singleCombatMapReference, primaryEnemyReference, null);
            }
            for (int nIndex = 0; nIndex < nSecondaryEnemies; nIndex++, nEnemyIndex++)
            {
                CreateEnemy(monsterIndex.Dequeue(), singleCombatMapReference, secondaryEnemyReference, null);
            }
        }

        public CombatMapUnit GetCombatUnit(Point2D unitPosition)
        {
            MapUnit mapUnit = _virtualMap.GetTopVisibleMapUnit(unitPosition, true);

            if (mapUnit is CombatMapUnit unit) return unit;

            return null;
        }

        /// <summary>
        /// Makes the next available character escape
        /// </summary>
        /// <param name="escapedPlayer">the player who escaped, or null if none left</param>
        /// <returns>true if a player escaped, false if none were found</returns>
        public bool NextCharacterEscape(out CombatPlayer escapedPlayer)
        {
            foreach (MapUnit mapUnit in CombatMapUnits.CurrentMapUnits)
            {
                if (!(mapUnit is CombatPlayer)) continue;
                
                CombatPlayer combatPlayer = (CombatPlayer) mapUnit;
                if (combatPlayer.HasEscaped) continue;
                    
                MakePlayerEscape(combatPlayer);
                escapedPlayer = combatPlayer;
                return true;
            }

            escapedPlayer = null;
            return false;
        }

        public void MakePlayerEscape(CombatPlayer combatPlayer)
        {
            Debug.Assert(!combatPlayer.HasEscaped);
                    
            combatPlayer.HasEscaped = true;
        }

    }
    
}