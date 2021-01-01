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
        public int Round { get; private set; }

        public int Turn { get; private set; }
        
        // world references
        private readonly VirtualMap _virtualMap;
        private readonly TileReferences _tileReferences;
        private readonly EnemyReferences _enemyReferences;
        private readonly InventoryReferences _inventoryReferences;
        private readonly Inventory _inventory;

        // player character information

        /// <summary>
        /// Current player or enemy who is active in current round
        /// </summary>
        public PlayerCharacterRecord ActivePlayerCharacterRecord { get; private set; }

        public CombatPlayer ActiveCombatPlayer => GetCurrentCombatUnit() is CombatPlayer player ? player : null;

        public Enemy ActiveEnemy => GetCurrentCombatUnit() is Enemy enemy ? enemy : null;
        
        /// <summary>
        /// All current player characters
        /// </summary>
        private PlayerCharacterRecords _playerCharacterRecords;
        
        /// <summary>
        /// Current combat map units for current combat map
        /// </summary>
        private MapUnits.MapUnits CombatMapUnits { get; }
        
        /// <summary>
        /// Queue that provides order attack for all players and enemies
        /// </summary>
        private readonly Queue<CombatMapUnit> _initiativeQueue = new Queue<CombatMapUnit>();
        /// <summary>
        /// A running tally of combat initiatives. This provides a running tally of initiative to provide
        /// ongoing boosts to high dexterity map units
        /// </summary>
        private readonly Dictionary<CombatMapUnit, int> _combatInitiativeTally = new Dictionary<CombatMapUnit, int>();

        /// <summary>
        /// Lowest player/enemy dexterity encountered
        /// </summary>
        private int _nLowestDexterity;
        /// <summary>
        /// Highest player/enemy dexterity encountered
        /// </summary>
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
            Round = -1;
            Turn = 0;

            foreach (MapUnit mapUnit in CombatMapUnits.CurrentMapUnits)
            {
                if (!IsCombatMapUnit(mapUnit)) continue;
                
                byte nDexterity = GetDexterity((CombatMapUnit)mapUnit);

                // get the highest and lowest dexterity values to be used in ongoing tally
                if (_nLowestDexterity > nDexterity) _nLowestDexterity = nDexterity;
                if (_nHighestDexterity < nDexterity) _nHighestDexterity = nDexterity;

                _combatInitiativeTally.Add((CombatMapUnit)mapUnit, 0);
            }
        }

        /// <summary>
        /// Gets the dexterity regardless of the type of combat unit
        /// </summary>
        /// <param name="mapUnit"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        private byte GetDexterity(CombatMapUnit mapUnit)
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

        /// <summary>
        /// Is the map unit a combat map unit type?
        /// </summary>
        /// <param name="mapUnit"></param>
        /// <returns></returns>
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

        public enum TurnResult { RequireCharacterInput, EnemyMoved, EnemyAttacks }

        private string GetAttackWeaponsString(CombatPlayer combatPlayer)
        {
            List<CombatItem> combatItems = GetAttackWeapons(combatPlayer);

            if (combatItems == null) return "bare hands";

            string combatItemString  = "";
            for (int index = 0; index < combatItems.Count; index++)
            {
                CombatItem item = combatItems[index];
                if (index > 0)
                    combatItemString += ", ";
                combatItemString += item.LongName;
            }

            return combatItemString;
        }
        
        private List<CombatItem> GetAttackWeapons(CombatPlayer combatPlayer)
        {
            string weaponNames;

            DataOvlReference.Equipment rightHandWeapon = combatPlayer.Record.Equipped.RightHand;

            List<CombatItem> weapons = new List<CombatItem>();

            bool bBareHands = false;

            bool isAttackingCombatItem(DataOvlReference.Equipment equipment)
            {
                return equipment != DataOvlReference.Equipment.Nothing &&
                       _inventory.GetItemFromEquipment(equipment) is CombatItem combatItem && combatItem.AttackStat > 0;
            }
            
            if (isAttackingCombatItem(combatPlayer.Record.Equipped.Helmet))
                weapons.Add(_inventory.GetItemFromEquipment(combatPlayer.Record.Equipped.Helmet));

            if (isAttackingCombatItem(combatPlayer.Record.Equipped.LeftHand))
                weapons.Add(_inventory.GetItemFromEquipment(combatPlayer.Record.Equipped.LeftHand));
            else
                bBareHands = true;

            if (isAttackingCombatItem(combatPlayer.Record.Equipped.RightHand))
                weapons.Add(_inventory.GetItemFromEquipment(combatPlayer.Record.Equipped.RightHand));
            else
                bBareHands = true;

            if (weapons.Count != 0) return weapons;
            
            Debug.Assert(bBareHands);
            return null;

        }
        
        public TurnResult ProcessMapUnitTurn(out CombatMapUnit affectedCombatMapUnit, out string outputStr)
        {
            affectedCombatMapUnit = GetCurrentCombatUnit();

            if (affectedCombatMapUnit is CombatPlayer combatPlayer)
            {
                outputStr = combatPlayer.Record.Name + ", armed with " + GetAttackWeaponsString(combatPlayer);  
           
                return TurnResult.RequireCharacterInput;
            }

            // either move the enemy or have them attack someone
            Debug.Assert(affectedCombatMapUnit is Enemy);
            Enemy enemy = (Enemy) affectedCombatMapUnit;

            outputStr = enemy.EnemyReference.MixedCaseSingularName + " moved.";
            
            AdvanceToNextCombatMapUnit();
            return TurnResult.EnemyMoved;
        }

        public void MoveActiveCombatMapUnit(Point2D xy)
        {
            CombatMapUnit currentCombatUnit = GetCurrentCombatUnit();
            currentCombatUnit.MapUnitPosition = new MapUnitPosition(xy.X, xy.Y, 0);
        }

        private CombatMapUnit GetCurrentCombatUnit()
        {
            // if the queue is empty then we will recalculate it before continuing
            if (_initiativeQueue.Count == 0) CalculateInitiativeQueue();

            Debug.Assert(_initiativeQueue.Count > 0);

            CombatMapUnit combatMapUnit = _initiativeQueue.Peek();

            return combatMapUnit;
        }
        
        /// <summary>
        /// Gets the next combat map unit that will be playing
        /// </summary>
        /// <returns></returns>
        public void AdvanceToNextCombatMapUnit()
        {
            Debug.Assert(_initiativeQueue.Count > 0);
            Turn++;
            // we are done with this unit now, so we just toss them out
            _initiativeQueue.Dequeue();

            // if the queue is empty then we will recalculate it before continuing
            if (_initiativeQueue.Count == 0) CalculateInitiativeQueue();

            Debug.Assert(_initiativeQueue.Count > 0);

            ActivePlayerCharacterRecord = _initiativeQueue.Peek() is CombatPlayer player ? player.Record : null;
        }
        
        /// <summary>
        /// Calculates an initiative queue giving the order of all attacks or moves within a single round
        /// </summary>
        private void CalculateInitiativeQueue()
        {
            Debug.Assert(_playerCharacterRecords != null);
            Debug.Assert(_initiativeQueue.Count == 0);
            Round++;
                
            // a mapping of dexterity values to an ordered list of combat map units 
            Dictionary<int, List<CombatMapUnit>> dexterityToCombatUnits = new Dictionary<int, List<CombatMapUnit>>();
            
            // go through each combat map unit and place them in priority order based on their dexterity values 
            foreach (MapUnit mapUnit in CombatMapUnits.CurrentMapUnits)
            {
                if (!IsCombatMapUnit(mapUnit)) continue;

                CombatMapUnit combatMapUnit = (CombatMapUnit) mapUnit;
                
                int nDexterity = GetDexterity(combatMapUnit);
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
        public CombatMap(VirtualMap virtualMap, SingleCombatMapReference singleCombatCombatMapReference, TileReferences tileReferences, EnemyReferences enemyReferences, 
            InventoryReferences inventoryReferences, Inventory inventory) : 
            base(null, null)
        {
            _virtualMap = virtualMap;
            CombatMapUnits = _virtualMap.TheMapUnits;
            _tileReferences = tileReferences;
            TheCombatMapReference = singleCombatCombatMapReference;
            _enemyReferences = enemyReferences;
            _inventoryReferences = inventoryReferences;
            _inventory = inventory;
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
                    playerStartPositions[nPlayer]);
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
    }
}