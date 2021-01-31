using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.Monsters;
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

        private InitiativeQueue _initiativeQueue; //= new InitiativeQueue();

        public int Turn => _initiativeQueue.Turn;
        public int Round => _initiativeQueue.Round;

        // player character information

        /// <summary>
        /// Current player or enemy who is active in current round
        /// </summary>
        public PlayerCharacterRecord ActivePlayerCharacterRecord { get; private set; }

        public CombatPlayer ActiveCombatPlayer => _initiativeQueue.GetCurrentCombatUnit() is CombatPlayer player ? player : null;

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

            // either move the enemy or have them attack someone
            Debug.Assert(affectedCombatMapUnit is Enemy);
            Enemy enemy = (Enemy) affectedCombatMapUnit;

            outputStr = enemy.EnemyReference.MixedCaseSingularName + " moved.";
            
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

      

        public void KillCombatMapUnit(CombatMapUnit combatMapUnit)
        {
            combatMapUnit.Stats.CurrentHp = 0;
        }

        public void AdvanceToNextCombatMapUnit()
        {
            CombatMapUnit combatMapUnit = _initiativeQueue.AdvanceToNextCombatMapUnit();
            
            ActivePlayerCharacterRecord = combatMapUnit is CombatPlayer player ? player.Record : null;
        }
        
        public List<CombatMapUnit> GetTopNCombatMapUnits(int nUnits)
        {
            // if there aren't the minimum number of turns, then we force it to add additional turns to at least
            // the stated number of units
            if (_initiativeQueue.TotalTurnsInQueue < nUnits)
                _initiativeQueue.CalculateNextInitiativeQueue();
            
            return _initiativeQueue.GetTopNCombatMapUnits(nUnits);
        }

        internal void InitializeInitiativeQueue()
        {
            _initiativeQueue = new InitiativeQueue(CombatMapUnits, _playerCharacterRecords);
            _initiativeQueue.InitializeInitiativeQueue();
        }

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
            base(null, null)
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

        public override int NumOfXTiles => SingleCombatMapReference.XTILES;
        public override int NumOfYTiles => SingleCombatMapReference.YTILES;

        public override byte[][] TheMap {
            get => TheCombatMapReference.TheMap;
            protected set { }
        }
        
        protected override float GetAStarWeight(TileReferences spriteTileReferences, Point2D xy)
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

            // for regular combat maps, we introduce some randomness 
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

        // public CombatMapUnit GetCombatUnit(Point2D unitPosition)
        // {
        //     CombatMapUnits.GetMapUnitByLocation(Maps.Combat, unitPosition, 0);
        // }
    }
}