using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.External;
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

        public int NumberOfEnemies => CombatMapUnits.CurrentMapUnits.OfType<Enemy>().Count(enemy => enemy.IsActive);
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

        public CombatMapUnit CurrentCombatMapUnit => _initiativeQueue.GetCurrentCombatUnit();
        
        public Enemy ActiveEnemy => _initiativeQueue.GetCurrentCombatUnit() is Enemy enemy ? enemy : null;

        public PlayerCharacterRecord SelectedCombatPlayerRecord => _initiativeQueue.ActivePlayerCharacterRecord;
        
        /// <summary> 
        /// All current player characters
        /// </summary>
        private PlayerCharacterRecords _playerCharacterRecords;
        
        /// <summary>
        /// Current combat map units for current combat map
        /// </summary>
        private MapUnits.MapUnits CombatMapUnits { get; }

        public enum TurnResult { RequireCharacterInput, EnemyMoved, EnemyAttacks, EnemyWandered, 
            EnemyEscaped, EnemyMissed, EnemyGrazed, EnemyMissedButHit, CombatPlayerMissed, CombatPlayerMissedButHit,
            CombatPlayerHit, CombatPlayerGrazed, CombatPlayerBlocked, NoAction 
        }

        public enum CombatMapUnitEnum { All, CombatPlayer, Enemy };

        public List<CombatMapUnit> GetActiveCombatMapUnitsByType(CombatMapUnitEnum combatMapUnitEnum)
        {
            switch (combatMapUnitEnum)
            {
                case CombatMapUnitEnum.All:
                    return AllVisibleAttackableCombatMapUnits;
                case CombatMapUnitEnum.CombatPlayer:
                    return AllCombatPlayersGeneric.Where(combatPlayer => combatPlayer.IsActive).ToList();
                case CombatMapUnitEnum.Enemy:
                    return AllEnemiesGeneric.Where(enemy => enemy.IsActive).ToList();;
                default:
                    throw new ArgumentOutOfRangeException(nameof(combatMapUnitEnum), combatMapUnitEnum, null);
            }
        }

        public List<CombatPlayer> AllCombatPlayers => CombatMapUnits.CurrentMapUnits.OfType<CombatPlayer>().ToList();
        private List<CombatMapUnit> AllCombatPlayersGeneric => AllCombatPlayers.Cast<CombatMapUnit>().ToList();
        private List<CombatMapUnit> AllEnemiesGeneric => AllEnemies.Cast<CombatMapUnit>().ToList();
        public List<Enemy> AllEnemies => CombatMapUnits.CurrentMapUnits.OfType<Enemy>().ToList();

        public List<CombatMapUnit> AllVisibleAttackableCombatMapUnits =>
            CombatMapUnits.CurrentMapUnits.Where(combatMapUnit => combatMapUnit.IsAttackable && combatMapUnit.IsActive).OfType<CombatMapUnit>().ToList();
        
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

            InitializeAStarMap(WalkableType.CombatLand);
            InitializeAStarMap(WalkableType.CombatWater);
        }

        public string GetCombatPlayerOutputText()
        {
            CombatPlayer combatPlayer = GetCurrentCombatPlayer();
            return combatPlayer.Record.Name + ", armed with " + combatPlayer.GetAttackWeaponsString();
        }

        private CombatPlayer GetCurrentCombatPlayer()
        {
            CombatMapUnit activeCombatMapUnit = _initiativeQueue.GetCurrentCombatUnit();
            
            Debug.Assert(activeCombatMapUnit is CombatPlayer);
            CombatPlayer combatPlayer = activeCombatMapUnit as CombatPlayer;

            return combatPlayer;
        }
        
        public enum SelectionAction {None, Magic, Attack}

        private Queue<CombatItem> _currentCombatItemQueue;

        public bool AreCombatItemsInQueue => _currentCombatItemQueue != null && _currentCombatItemQueue.Count > 0;
        public int NumberOfCombatItemInQueue => _currentCombatItemQueue.Count;
        public CombatItem PeekCurrentCombatItem() => _currentCombatItemQueue.Peek();
        public CombatItem DequeueCurrentCombatItem() => _currentCombatItemQueue.Dequeue();

        private void ClearCurrentCombatItemQueue()
        {
            if (_currentCombatItemQueue == null)
                _currentCombatItemQueue = new Queue<CombatItem>();
            else
                _currentCombatItemQueue.Clear();
        }

        public void BuildCombatItemQueue(List<CombatItem> combatItems) => _currentCombatItemQueue = new Queue<CombatItem>(combatItems);

        private bool IsRangedPathBlocked(Point2D attackingPoint, Point2D opponentMapUnit, out Point2D firstBlockPoint)
        {
            // get the points between the player and opponent
            List<Point2D> points = attackingPoint.Raytrace(opponentMapUnit);
            // for (int i = 1; i < points.Count - 2; i++)
            for (int i = 0; i < points.Count - 1; i++)
            {
                Point2D point = points[i];
                TileReference tileRef = GetTileReference(point);
                if (tileRef.IsLandEnemyPassable || tileRef.IsWaterEnemyPassable) continue;
                
                // we can't penetrate this thing and need to give up
                firstBlockPoint = point;
                return true;
            }

            firstBlockPoint = null;
            return false;
        }
        
        
        public TurnResult ProcessCombatPlayerTurn(SelectionAction selectedAction, Point2D actionPosition, 
            out CombatMapUnit activeCombatMapUnit, out CombatMapUnit targetedCombatMapUnit, 
            out string preAttackOutputStr, out string postAttackOutputStr, out Point2D missedPoint, 
            out CombatMapUnit.HitState targetedHitState)
        {
            targetedCombatMapUnit = null;
            missedPoint = null;

            CombatPlayer combatPlayer = GetCurrentCombatPlayer();

            preAttackOutputStr = "";
            postAttackOutputStr = "";
            activeCombatMapUnit = null;
            
            targetedHitState = CombatMapUnit.HitState.None;

            switch (selectedAction)
            {
                case SelectionAction.None:
                    return TurnResult.NoAction;
                case SelectionAction.Magic:
                    return TurnResult.NoAction;
                case SelectionAction.Attack:
                    preAttackOutputStr = "Attack ";
                    MapUnit opponentMapUnit = GetCombatUnit(actionPosition);

                    // you have tried to attack yourself!?
                    if (opponentMapUnit == combatPlayer)
                    {
                        preAttackOutputStr = preAttackOutputStr.TrimEnd() + "... yourself? ";
                        preAttackOutputStr += "\nYou think better of it and skip your turn.";
                        return TurnResult.NoAction;
                    }
                    
                    CombatItem weapon;
                    
                    // the top most unit is NOT a combat unit, so they hit nothing!
                    if (!(opponentMapUnit is CombatMapUnit opponentCombatMapUnit)) 
                    {
                        preAttackOutputStr += "nothing with ";
                        if (_currentCombatItemQueue == null)
                        {
                            // bare hands
                            preAttackOutputStr += " with bare hands!";
                        }
                        else
                        {
                            weapon = _currentCombatItemQueue.Dequeue();
                            preAttackOutputStr += weapon.LongName + "!";
                        }

                        if (_currentCombatItemQueue == null || _currentCombatItemQueue.Count == 0)
                        {
                            AdvanceToNextCombatMapUnit();
                        }

                        missedPoint = actionPosition;
                        targetedHitState = CombatMapUnit.HitState.Missed; 
                        return TurnResult.CombatPlayerMissed; //true;
                    }
                    
                    // if the top most unit is a combat map unit, then let's fight!
                    preAttackOutputStr += opponentCombatMapUnit.Name;

                    weapon = _currentCombatItemQueue.Dequeue();

                    preAttackOutputStr += " with " + weapon.LongName + "!";

                    // let's first make sure that any range weapons do not hit a wall first!
                    if (weapon.Range > 1)
                    {
                        bool bIsBlocked = IsRangedPathBlocked(combatPlayer.MapUnitPosition.XY,
                            opponentMapUnit.MapUnitPosition.XY,
                            out missedPoint);
                        if (bIsBlocked)
                        {
                            postAttackOutputStr =
                                combatPlayer.FriendlyName + _dataOvlReference.StringReferences
                                                              .GetString(DataOvlReference.BattleStrings._MISSED_BANG_N)
                                                              .TrimEnd().Replace("!", " ")
                                                          + opponentMapUnit.FriendlyName + " because it was blocked!";

                            AdvanceToNextCombatMapUnit();
                            targetedHitState = CombatMapUnit.HitState.Missed;
                            return TurnResult.CombatPlayerBlocked;
                        }
                    }
                    
                    // do the attack logic
                    targetedHitState = combatPlayer.Attack(opponentCombatMapUnit, weapon, out string stateOutput);
                    postAttackOutputStr = stateOutput;
                    
                    // if the player attacks, but misses with a range weapon the we need see if they
                    // accidentally hit someone else
                    bool bMissedButHit = false;
                    if (targetedHitState == CombatMapUnit.HitState.Missed && weapon.Range > 1)
                    {
                        TurnResult turnResult = HandleRangedMissed(combatPlayer, 
                            opponentCombatMapUnit.MapUnitPosition.XY, out targetedCombatMapUnit,
                            weapon.AttackStat, out missedPoint, out string addStr);
                        postAttackOutputStr += addStr;

                        if (turnResult == TurnResult.EnemyMissedButHit)
                        {
                            bMissedButHit = true;
                        }
                    }
                    else
                    {
                        // we know they attacked this particular opponent at this point
                        targetedCombatMapUnit = opponentCombatMapUnit;
                        missedPoint = targetedCombatMapUnit.MapUnitPosition.XY;
                    }
                    
                    HandleHitState(targetedHitState, targetedCombatMapUnit);

                    if (_currentCombatItemQueue == null || _currentCombatItemQueue.Count == 0)
                    {
                        AdvanceToNextCombatMapUnit();
                    }

                    if (bMissedButHit) return TurnResult.CombatPlayerMissedButHit;

                    if (targetedHitState == CombatMapUnit.HitState.Grazed) return TurnResult.CombatPlayerGrazed;
                    
                    return targetedHitState != CombatMapUnit.HitState.Missed
                        ? TurnResult.CombatPlayerHit
                        : TurnResult.CombatPlayerMissed;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleHitState(CombatMapUnit.HitState hitState, CombatMapUnit affectedCombatMapUnit)
        {
            switch (hitState)
            {
                case CombatMapUnit.HitState.Grazed:
                    break;
                case CombatMapUnit.HitState.Missed:
                    break;
                case CombatMapUnit.HitState.BarelyWounded:
                    break;
                case CombatMapUnit.HitState.LightlyWounded:
                    break;
                case CombatMapUnit.HitState.HeavilyWounded:
                    break;
                case CombatMapUnit.HitState.CriticallyWounded:
                    break;
                case CombatMapUnit.HitState.Fleeing:
                    if (affectedCombatMapUnit is Enemy fleeingEnemy)
                    {
                    }
                    break;
                case CombatMapUnit.HitState.Dead:
                    if (affectedCombatMapUnit is Enemy deadEnemy)
                    {
                        // if the enemy was an NPC then we kill them!
                        if (deadEnemy.NPCRef != null)
                        {
                            deadEnemy.NPCRef.IsDead = true;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hitState), hitState, null);
            }
        }

        /// <summary>
        /// Attempts to processes the turn of the current combat unit - either CombatPlayer or Enemy.
        /// Can result in advancing to next turn, or indicate user input required
        /// </summary>
        /// <param name="activeCombatMapUnit">the combat unit that is taking the action</param>
        /// <param name="targetedCombatMapUnit">an optional unit that is being affected by the active combat unit</param>
        /// <param name="preAttackOutputStr"></param>
        /// <param name="postAttackOutputStr"></param>
        /// <param name="missedPoint">if the target is empty or missed then this gives the point that the attack landed</param>
        /// <returns></returns>
        public TurnResult ProcessEnemyTurn(out CombatMapUnit activeCombatMapUnit, out CombatMapUnit targetedCombatMapUnit, 
            out string preAttackOutputStr, out string postAttackOutputStr, out Point2D missedPoint)
        {
            activeCombatMapUnit = _initiativeQueue.GetCurrentCombatUnit();
            targetedCombatMapUnit = null;
            missedPoint = null;
            postAttackOutputStr = "";
            preAttackOutputStr = "";

            // Everything after is ENEMY logic!
            // either move the ENEMY or have them attack someone
            Debug.Assert(activeCombatMapUnit is Enemy);
            Enemy enemy = activeCombatMapUnit as Enemy;

            // if the enemy is charmed then the player get's to control them instead!
            if (enemy.IsCharmed)
            {
                // the player get's to control the enemy!
                preAttackOutputStr = enemy.FriendlyName + ":";  
           
                return TurnResult.RequireCharacterInput;
            }

            // the enemy is badly wounded and is going to try to escape
            if (enemy.IsFleeing)
            {
                // if the enemy is on an outer tile, then they are free exit and end their turn
                if (enemy.MapUnitPosition.X == 0 || enemy.MapUnitPosition.X == NumOfXTiles - 1 ||
                    enemy.MapUnitPosition.Y == 0 || enemy.MapUnitPosition.Y == NumOfYTiles - 1)
                {
                    enemy.Stats.CurrentHp = 0;
                    preAttackOutputStr = enemy.EnemyReference.MixedCaseSingularName + " escaped!";
                    AdvanceToNextCombatMapUnit();
                    return TurnResult.EnemyEscaped;
                }
                
                TileReference tileReference = enemy.FleeingPath != null ? GetTileReference(enemy.FleeingPath.Peek().Position) : null;
                CombatMapUnit combatMapUnit =
                    enemy.FleeingPath != null ? GetCombatUnit(enemy.FleeingPath.Peek().Position) : null;
                // bool bIsTileWalkable = tileReference != null && combatMapUnit == null && IsTileWalkable(tileReference); 
                bool bIsTileWalkable =
                    tileReference != null && (combatMapUnit == null && enemy.EnemyReference.IsWaterEnemy
                        ? tileReference.IsWaterEnemyPassable : tileReference.IsLandEnemyPassable);//IsTileWalkable(tileReference); 
                
                
                // does the monster not yet have a flee path OR
                // does the enemy have a flee path already established that is now block OR
                if (enemy.FleeingPath == null || !bIsTileWalkable)
                {
                    enemy.FleeingPath = GetEscapeRoute(enemy.MapUnitPosition.XY, 
                        enemy.EnemyReference.IsWaterEnemy ? WalkableType.CombatWater : WalkableType.CombatLand);
                    // if the enemy is unable to calculate an exit path
                    if (enemy.FleeingPath == null)
                    {
                        // if I decide to do something, then I will do it here
                    }
                }

                // if there is a path then follow it, otherwise fall through and attack like normal
                if (enemy.FleeingPath?.Count > 0)
                {
                    Point2D nextStep = enemy.FleeingPath.Pop().Position;
                    MoveActiveCombatMapUnit(nextStep);
                    preAttackOutputStr = enemy.EnemyReference.MixedCaseSingularName + " fleeing!";
                    AdvanceToNextCombatMapUnit();
                    return TurnResult.EnemyMoved;
                }
            }

            // if enemy is within range of someone then they will have a bestCombatPlayer to attack
            // if their old target is now out of range, they won't hesitate to attack someone who is
            bool bPreviousTargetInRange = enemy.PreviousAttackTarget != null && enemy.PreviousAttackTarget.Stats.CurrentHp > 0 && 
                                          (enemy.CanReachForMeleeAttack(enemy.PreviousAttackTarget) 
                                            || !IsRangedPathBlocked(enemy.MapUnitPosition.XY, enemy.PreviousAttackTarget.MapUnitPosition.XY, out _));
            CombatMapUnit bestCombatPlayer = bPreviousTargetInRange ? enemy.PreviousAttackTarget : GetClosestCombatPlayerInRange(enemy);
            
            // we determine if the best combat player is close enough to attack or not
            bool bIsAttackable = bestCombatPlayer?.IsAttackable ?? false;
            bool bIsReachable = bestCombatPlayer != null && enemy.CanReachForMeleeAttack(bestCombatPlayer);
            
            Debug.Assert(bestCombatPlayer?.IsAttackable ?? true);
            
            // if the best combat player is attackable and reachable, then we do just that!
            if (bIsAttackable && bIsReachable)
            {
                CombatMapUnit.HitState hitState = enemy.Attack(bestCombatPlayer, enemy.EnemyReference.TheDefaultEnemyStats.Damage,
                    out postAttackOutputStr);
                switch (hitState)
                {
                    case CombatMapUnit.HitState.Missed:
                        // oh oh - the enemy missed
                        if (enemy.EnemyReference.TheMissileType == CombatItem.MissileType.None)
                        {
                            targetedCombatMapUnit = bestCombatPlayer;
                            break;
                        }

                        Debug.Assert(enemy.EnemyReference.AttackRange > 1,
                            "Cannot have a ranged weapon if no missile type set");

                        TurnResult turnResult = HandleRangedMissed(enemy, 
                            bestCombatPlayer.MapUnitPosition.XY, out targetedCombatMapUnit,
                            enemy.EnemyReference.TheDefaultEnemyStats.Damage, out missedPoint, out string addStr);
                        
                        postAttackOutputStr += addStr;
                        return turnResult;
                    case CombatMapUnit.HitState.Grazed:
                    case CombatMapUnit.HitState.BarelyWounded:
                    case CombatMapUnit.HitState.LightlyWounded:
                    case CombatMapUnit.HitState.HeavilyWounded:
                    case CombatMapUnit.HitState.CriticallyWounded:
                    case CombatMapUnit.HitState.Dead:
                        targetedCombatMapUnit = bestCombatPlayer;
                        break;
                    case CombatMapUnit.HitState.Fleeing:
                        targetedCombatMapUnit = bestCombatPlayer;
                        //enemy.IsFleeing = true;
                        break;
                    case CombatMapUnit.HitState.None:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                AdvanceToNextCombatMapUnit();
                if (hitState == CombatMapUnit.HitState.Grazed)
                    return TurnResult.EnemyGrazed;
                return TurnResult.EnemyAttacks;
            }

            CombatMapUnit pursuedCombatMapUnit = MoveToClosestAttackableCombatPlayer(enemy);

            if (pursuedCombatMapUnit != null)
            {
                // we have exhausted all potential attacking possibilities, so instead we will just move 
                preAttackOutputStr = enemy.EnemyReference.MixedCaseSingularName + " moved.";
            }
            else
            {
                preAttackOutputStr = enemy.EnemyReference.MixedCaseSingularName + " is unable to move or attack.";
            }

            AdvanceToNextCombatMapUnit();
            
            return TurnResult.EnemyMoved;
        }
        
        private void RefreshCurrentCombatPlayer()
        {
            if (CurrentCombatPlayer is null)
            {
                ClearCurrentCombatItemQueue();
                return;
            }

            List <CombatItem> combatItems =
                CurrentCombatPlayer.GetAttackWeapons();
            BuildCombatItemQueue(combatItems);
        }


        private TurnResult HandleRangedMissed(CombatMapUnit attackingCombatMapUnit, Point2D attackPosition, 
            out CombatMapUnit targetedCombatMapUnit, int nAttackMax, out Point2D missedPoint, out string outputStr)
        {
            // they are ranged, and missed which means we need to pick a new tile to attack
            // get a list of all surround tiles surrounding the player that they are attacking
            outputStr = "";
            missedPoint = null;

            Point2D newAttackPosition = GetRandomSurroundingPointThatIsnt(attackPosition,
                attackPosition);
            Debug.Assert(newAttackPosition != null);
            
            targetedCombatMapUnit = GetCombatUnit(newAttackPosition);

            // We will check the raycast and determine if the missed shot in fact actually gets blocked - 
            // doing it in here will ensure that no damage is computed against an enemy if they are targeted
            if (IsRangedPathBlocked(attackPosition, newAttackPosition, out missedPoint))
            {
                AdvanceToNextCombatMapUnit();
                return TurnResult.EnemyMissed;
            }

            if (targetedCombatMapUnit == null)
            {
                missedPoint = newAttackPosition;
                AdvanceToNextCombatMapUnit();
                return TurnResult.EnemyMissed;
            }

            outputStr += "\nBut they accidentally hit another!"; 
            // we attack the thing we accidentally hit
            CombatMapUnit.HitState hitState = attackingCombatMapUnit.Attack(targetedCombatMapUnit,
                nAttackMax, out string missedAttackOutputStr, true);
            
            outputStr += "\n" + missedAttackOutputStr;
            AdvanceToNextCombatMapUnit();
            return TurnResult.EnemyMissedButHit;
        }

        private Point2D GetRandomSurroundingPointThatIsnt(Point2D surroundThisPoint, Point2D notThisPoint)
        {
            List<Point2D> surroundingCombatPlayerPoints =
                surroundThisPoint.GetConstrainedSurroundingPoints(1, NumOfXTiles - 1,
                    NumOfYTiles - 1);
            Point2D randomSurroundingPoint;
            Random random = new Random();
            for (;;)
            {
                int nIndex = random.Next() % surroundingCombatPlayerPoints.Count;
                randomSurroundingPoint = surroundingCombatPlayerPoints[nIndex];
                if (randomSurroundingPoint != notThisPoint)
                    break;
                surroundingCombatPlayerPoints.RemoveAt(nIndex);
            }

            return randomSurroundingPoint;
        }
        
        /// <summary>
        /// Moves the active combat unit to a new map location
        /// </summary>
        /// <param name="xy"></param>
        public void MoveActiveCombatMapUnit(Point2D xy)
        {
            CombatMapUnit currentCombatUnit = _initiativeQueue.GetCurrentCombatUnit();
            
            // reset the a star walking rules
            RecalculateWalkableTile(currentCombatUnit.MapUnitPosition.XY, WalkableType.CombatLand);
            RecalculateWalkableTile(currentCombatUnit.MapUnitPosition.XY, WalkableType.CombatWater);
            
            currentCombatUnit.MapUnitPosition = new MapUnitPosition(xy.X, xy.Y, 0);

            WalkableType walkableType = GetWalkableTypeByMapUnit(currentCombatUnit);
            SetWalkableTile(xy, false, walkableType);
        }
        
        public void KillCombatMapUnit(CombatMapUnit combatMapUnit)
        {
            combatMapUnit.Stats.CurrentHp = 0;
            if (combatMapUnit is Enemy enemy)
            {
                RecalculateWalkableTile(combatMapUnit.MapUnitPosition.XY, enemy.EnemyReference.IsWaterEnemy ?
                    WalkableType.CombatWater : WalkableType.CombatLand);
            }
            else
            {
                RecalculateVisibleTiles(combatMapUnit.MapUnitPosition.XY);
            }
        }

        public CombatPlayer GetCombatPlayer(PlayerCharacterRecord record) => 
            CombatMapUnits.CurrentMapUnits.OfType<CombatPlayer>().FirstOrDefault(player => player.Record == record);

        public void AdvanceToNextCombatMapUnit()
        {
            CombatMapUnit combatMapUnit = _initiativeQueue.AdvanceToNextCombatMapUnit();
            
            CurrentPlayerCharacterRecord = combatMapUnit is CombatPlayer player ? player.Record : null;

            _bPlayerHasChanged = true;

            RefreshCurrentCombatPlayer();
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

        private bool _bPlayerHasChanged = true;
        public bool GetHasPlayerChangedSinceLastCheckAndReset()
        {
            if (!_bPlayerHasChanged) return false;
            _bPlayerHasChanged = false;
            return true;
        }

        public void SetActivePlayerCharacter(PlayerCharacterRecord record)
        {
            // if (CurrentCombatPlayer.Record != record)
            // {
            //     
            // }

            _initiativeQueue.SetActivePlayerCharacter(record);
            _bPlayerHasChanged = true;
            RefreshCurrentCombatPlayer();
        }

        public Enemy GetFirstEnemy(CombatItem combatItem) => GetNextEnemy(null, combatItem);

        public CombatPlayer MoveToClosestAttackableCombatPlayer(CombatMapUnit activeCombatUnit) =>
            MoveToClosestAttackableCombatMapUnit(activeCombatUnit, CombatMapUnitEnum.CombatPlayer) as CombatPlayer;

        public Enemy MoveToClosestAttackableEnemy(out string outputStr) => MoveToClosestAttackableEnemy(CurrentCombatPlayer, out outputStr);

        public Enemy MoveToClosestAttackableEnemy(CombatMapUnit activeMapUnit, out string outputStr)
        {
            Enemy enemy = MoveToClosestAttackableCombatMapUnit(activeMapUnit, CombatMapUnitEnum.Enemy) as Enemy;
            if (enemy == null)
            {
                outputStr = "Unable to target or advance on enemy.";
            }
            else
            {
                outputStr = CurrentCombatPlayer.FriendlyName + " advances on " + enemy.FriendlyName;
            }
            AdvanceToNextCombatMapUnit();
            return enemy;
        }

        /// <summary>
        /// Moves the combat map unit to the CombatPlayer for whom they can reach in the fewest number of steps
        /// </summary>
        /// <param name="activeCombatUnit">the combat map unit that wants to attack a combat player</param>
        /// <param name="preferredAttackTarget">the type of target you will target</param>
        /// <returns>The combat player that they are heading towards</returns>
        public CombatMapUnit MoveToClosestAttackableCombatMapUnit(CombatMapUnit activeCombatUnit, CombatMapUnitEnum preferredAttackTarget)
        {
            const int NoPath = 0xFFFF;
            bool bCharmed = activeCombatUnit.IsCharmed;

            int nMinMoves = 0xFFFF;
            Stack<Node> preferredRoute = null;
            CombatMapUnit preferredAttackVictim = null;

            AStar aStar = GetAStarByMapUnit(activeCombatUnit);

            List<Point2D> potentialTargetsPoints = new List<Point2D>();
            
            foreach (CombatMapUnit combatMapUnit in GetActiveCombatMapUnitsByType(preferredAttackTarget))
            {
                Point2D combatMapUnitXY = combatMapUnit.MapUnitPosition.XY;
                
                potentialTargetsPoints.Add(combatMapUnitXY);
                
                // get the shortest path to the unit - we ignore the range value because by calling this method we are insisting
                // that they move
                Stack<Node> theWay = aStar.FindBestPathForSurroundingTiles(activeCombatUnit.MapUnitPosition.XY,
                    combatMapUnitXY, 1);//activeCombatUnit.ClosestAttackRange);
                int nMoves = theWay?.Count ?? NoPath;
                if (nMoves < nMinMoves)
                {
                    nMinMoves = nMoves;
                    preferredRoute = theWay;
                    preferredAttackVictim = combatMapUnit;
                }
            }

            Point2D activeCombatUnitXY = activeCombatUnit.MapUnitPosition.XY;
            
            if (nMinMoves == NoPath)
            {
                // if there is no path, then lets do some dirty checks to see if we can at least move closer
                
                // get the surrounding points around current active unit
                List <Point2D> surroundingPoints = 
                    activeCombatUnitXY.GetConstrainedFourDirectionSurroundingPoints(NumOfXTiles - 1, NumOfYTiles - 1);

                double fShortestPath = 999f;
                Point2D bestOpponentPoint = null;
                // cycle through all potential targets and determine and pick the closest available target
                foreach (Point2D point in potentialTargetsPoints)
                {
                    double fDistance = point.DistanceBetween(activeCombatUnitXY);
                    if (fDistance < fShortestPath)
                    {
                        fShortestPath = fDistance;
                        bestOpponentPoint = point.Copy();
                    }
                }

                // there is not best point, so give up - also, this shouldn't really happen, but not 
                // worth crashing over if it does
                if (bestOpponentPoint == null)
                    return null;

                // we start with the shortest path being the current tiles distance to the best enemies tile
                fShortestPath = activeCombatUnitXY.DistanceBetween(bestOpponentPoint);
                
                Point2D nextBestMovePoint = null;
                 List<Point2D> wanderablePoints = new List<Point2D>();
                // go through of each surrounding points and find the shortest path based to an opponent
                // on the free tiles
                foreach (Point2D point in surroundingPoints)
                {
                    // if it isn't walkable then we skip it
                    if (!GetAStarByMapUnit(activeCombatUnit).GetWalkable(point)) continue;
                    // keep track of the points we could wander to if we don't find a good path
                    wanderablePoints.Add(point);
                    double fDistance = point.DistanceBetween(bestOpponentPoint);
                    if (fDistance < fShortestPath)
                    {
                        fShortestPath = fDistance;
                        nextBestMovePoint = point;
                    }
                }

                if (nextBestMovePoint == null)
                {
                    if (wanderablePoints.Count == 0) return null;
                    Random ran = new Random();

                    // only a 50% chance they will wander
                    if (ran.Next() % 2 == 0) return null;
                    
                    // wander logic - we are already the closest to the selected enemy
                    int nChoices = wanderablePoints.Count;
                    int nRandomChoice = ran.Next() % nChoices;
                    nextBestMovePoint = wanderablePoints[nRandomChoice];
                }

                // we think we found the next best path
                preferredRoute = new Stack<Node>();
                preferredRoute.Push(aStar.GetNode(nextBestMovePoint));
            }

            Debug.Assert(preferredRoute != null, nameof(preferredRoute) + " != null");
            
            Point2D nextPosition = preferredRoute.Pop().Position;
            
            MoveActiveCombatMapUnit(nextPosition);

            return preferredAttackVictim;
        }
        
        private CombatPlayer GetClosestCombatPlayerInRange(Enemy enemy)
        {
            int nMapUnits = CombatMapUnits.CurrentMapUnits.Count();

            double dBestDistanceToAttack = 150f;
            CombatPlayer bestCombatPlayer = null;
            
            for (int nIndex = 0; nIndex < nMapUnits; nIndex++)
            {
                if (!(CombatMapUnits.CurrentMapUnits[nIndex] is CombatPlayer combatPlayer)) continue;
                if (enemy.EnemyReference.AttackRange == 1 && !enemy.CanReachForMeleeAttack(combatPlayer, enemy.EnemyReference.AttackRange)) continue;
                if (enemy.EnemyReference.AttackRange > 1)
                {
                    if (IsRangedPathBlocked(enemy.MapUnitPosition.XY,  combatPlayer.MapUnitPosition.XY, out _)) continue;
                }
                if (!CombatMapUnits.CurrentMapUnits[nIndex].IsActive) continue;

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
                // if (!CurrentCombatPlayer.CanReachForMeleeAttack(enemy, combatItem)) continue;
                if (combatItem.Range == 1 && !CurrentCombatPlayer.CanReachForMeleeAttack(enemy, combatItem.Range)) continue;
                if (combatItem.Range > 1)
                {
                    if (IsRangedPathBlocked(CurrentCombatPlayer.MapUnitPosition.XY,  enemy.MapUnitPosition.XY, out _)) continue;
                }
                
                if (!CombatMapUnits.CurrentMapUnits[nIndex].IsActive) continue;
                
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
                if (!enemy.IsActive) continue;
                if (CurrentCombatPlayer.CanReachForAttack(enemy, combatItem)) return enemy;
            }
            return null;
        }

        internal void InitializeInitiativeQueue()
        {
            _initiativeQueue = new InitiativeQueue(CombatMapUnits, _playerCharacterRecords);
            _initiativeQueue.InitializeInitiativeQueue();
            RefreshCurrentCombatPlayer();
        }

        protected override float GetAStarWeight(Point2D xy)
        {
            return 1.0f;
        }
        
        protected override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit)
        {
            switch (mapUnit)
            {
                case Enemy enemy:
                    return enemy.EnemyReference.IsWaterEnemy ? WalkableType.CombatWater : WalkableType.CombatLand;
                case CombatPlayer _:
                    return WalkableType.CombatLand;
                default:
                    return WalkableType.StandardWalking;
            }
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

                // make sure the tile that the player occupies is not walkable
                //aStarDictionary[WalkableType.CombatLand].
                GetAStarByWalkableType(WalkableType.CombatLand).SetWalkable(playerStartPositions[nPlayer], false);

                CombatMapUnits.CurrentMapUnits[nPlayer] = combatPlayer;
            }
        }

        /// <summary>
        /// Creates a single enemy in the context of the combat map.
        /// </summary>
        /// <param name="nEnemyIndex">0 based index that refcts the combat maps enemy index list</param>
        /// <param name="singleCombatMapReference">reference of the combat map</param>
        /// <param name="enemyReference">reference to enemy to be added (ignored for auto selected enemies)</param>
        /// <param name="npcRef"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void CreateEnemy(int nEnemyIndex, SingleCombatMapReference singleCombatMapReference,
            EnemyReference enemyReference, NonPlayerCharacterReference npcRef)
        {
             SingleCombatMapReference.CombatMapSpriteType combatMapSpriteType = 
                singleCombatMapReference.GetAdjustedEnemySprite(nEnemyIndex, out int nEnemySprite);
            Point2D enemyPosition = singleCombatMapReference.GetEnemyPosition(nEnemyIndex);

            Enemy enemy = null;
            switch (combatMapSpriteType)
            {
                case SingleCombatMapReference.CombatMapSpriteType.Nothing:
                    Debug.Assert(enemyPosition.X == 0 && enemyPosition.Y ==0);
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.Thing:
                    Debug.WriteLine("It's a chest or maybe a dead body!");
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.AutoSelected:
                    enemy = CombatMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
                        _enemyReferences.GetEnemyReference(nEnemySprite), out int _, npcRef);
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.EncounterBased:
                    Debug.Assert(!(enemyPosition.X == 0 && enemyPosition.Y == 0));
                    enemy = CombatMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
                        enemyReference, out int _, npcRef);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // make sure the tile that the enemy occupies is not walkable
            // we do both land and water in case there are overlappig tiles (which there shouldn't be!?)
            SetWalkable(enemyPosition, false);
        }

        private void SetWalkable(Point2D position, bool bWalkable)
        {
            GetAStarByWalkableType(WalkableType.CombatLand).SetWalkable(position, false);
            GetAStarByWalkableType(WalkableType.CombatWater).SetWalkable(position, false);
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
            AdvanceToNextCombatMapUnit();
        }

        // public string GetWalkableDebug()
        // {
        //     return AStar.GetWalkableDebug();
        // }

        /// <summary>
        /// Gets the best escape route based on current position
        /// </summary>
        /// <param name="fromPosition"></param>
        /// <param name="walkableType"></param>
        /// <returns>path to exit, or null if none exist</returns>
        public Stack<Node> GetEscapeRoute(Point2D fromPosition, WalkableType walkableType)
        {
            List<Point2D> points = GetEscapablePoints(fromPosition, walkableType);

            int nShortestPath = 0xFFFF;
            Stack<Node> shortestPath = null;
            
            foreach (Point2D destinationPoint in points)
            {
                Stack<Node> currentPath = GetAStarByWalkableType(walkableType).FindPath(fromPosition, destinationPoint);
                if (currentPath?.Count >= nShortestPath || currentPath == null) continue;

                nShortestPath = currentPath.Count;
                shortestPath = currentPath;
            }

            return shortestPath;
        }

        /// <summary>
        /// Gets all tiles that are at the edge of the screen and are escapable based on the given position
        /// </summary>
        /// <param name="fromPosition"></param>
        /// <param name="walkableType"></param>
        /// <returns>a list of all potential positions</returns>
        public List<Point2D> GetEscapablePoints(Point2D fromPosition, WalkableType walkableType)
        {
            _ = fromPosition;
            List<Point2D> points = new List<Point2D>();

            for (int nIndex = 0; nIndex < NumOfXTiles; nIndex++)
            {
                Point2D top = new Point2D(nIndex, 0);
                Point2D bottom = new Point2D(nIndex, NumOfYTiles - 1);
                Point2D left = new Point2D(0, nIndex);
                Point2D right = new Point2D(NumOfXTiles - 1, nIndex);

                switch (walkableType)
                {
                    case WalkableType.CombatLand:
                        if (GetTileReference(top).IsLandEnemyPassable) points.Add(top);
                        if (GetTileReference(bottom).IsLandEnemyPassable) points.Add(bottom);
                
                        if (nIndex == 0 || nIndex == NumOfYTiles - 1) continue; // we don't double count the top or bottom 
                
                        if (GetTileReference(left).IsLandEnemyPassable) points.Add(left);
                        if (GetTileReference(right).IsLandEnemyPassable) points.Add(right);
                        break;
                    case WalkableType.CombatWater:
                        if (GetTileReference(top).IsWaterEnemyPassable) points.Add(top);
                        if (GetTileReference(bottom).IsWaterEnemyPassable) points.Add(bottom);
                
                        if (nIndex == 0 || nIndex == NumOfYTiles - 1) continue; // we don't double count the top or bottom 
                
                        if (GetTileReference(left).IsWaterEnemyPassable) points.Add(left);
                        if (GetTileReference(right).IsWaterEnemyPassable) points.Add(right);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(walkableType), walkableType, null);
                }
            }
            
            return points;
        }
    }
    
}