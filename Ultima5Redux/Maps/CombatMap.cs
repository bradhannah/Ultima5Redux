using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Ultima5Redux.External;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.Maps
{
    public class CombatMap : Map
    {
        // when computing what should happen when a player hits - these states can be triggered 
        private enum AdditionalHitStateAction { None, EnemyDivided }

        public enum SpecificCombatMapUnit { All, CombatPlayer, Enemy }

        public enum SelectionAction { None, Magic, Attack }

        public enum TurnResult
        {
            RequireCharacterInput, EnemyMoved, EnemyAttacks, EnemyWandered, EnemyEscaped, EnemyMissed, EnemyGrazed,
            EnemyMissedButHit, CombatPlayerMissed, CombatPlayerMissedButHit, CombatPlayerHit, CombatPlayerGrazed,
            CombatPlayerBlocked, NoAction, Sleeping
        }

        private bool _bPlayerHasChanged = true;

        private Queue<CombatItem> _currentCombatItemQueue;

        private InitiativeQueue _initiativeQueue;

        /// <summary>
        ///     All current player characters
        /// </summary>
        private PlayerCharacterRecords _playerCharacterRecords;

        private IEnumerable<CombatMapUnit> AllCombatPlayersGeneric => AllCombatPlayers;
        private IEnumerable<CombatMapUnit> AllEnemiesGeneric => AllEnemies;

        /// <summary>
        ///     Current combat map units for current combat map
        /// </summary>
        private MapUnits.MapUnits CombatMapUnits { get; }

        public override int NumOfXTiles => SingleCombatMapReference.XTILES;
        public override int NumOfYTiles => SingleCombatMapReference.YTILES;

        public override bool ShowOuterSmallMapTiles => false;

        public override byte[][] TheMap
        {
            get => TheCombatMapReference.TheMap;
            protected set
            {
                // do nothing, not allowed
            }
        }

        public Enemy ActiveEnemy => _initiativeQueue.GetCurrentCombatUnit() is Enemy enemy ? enemy : null;

        public IEnumerable<CombatPlayer> AllCombatPlayers => CombatMapUnits.CurrentMapUnits.CombatPlayers;
        public IEnumerable<Enemy> AllEnemies => CombatMapUnits.CurrentMapUnits.Enemies;

        public IEnumerable<CombatMapUnit> AllVisibleAttackableCombatMapUnits =>
            CombatMapUnits.CurrentMapUnits.AllCombatMapUnits.Where(combatMapUnit =>
                combatMapUnit.IsAttackable && combatMapUnit.IsActive);

        /// <summary>
        ///     The player character who the player has selected to focus on (#1-#6)
        /// </summary>
        /// <returns>active player character record OR null if none selected</returns>
        public PlayerCharacterRecord ActivePlayerCharacterRecord => _initiativeQueue.ActivePlayerCharacterRecord;

        public bool AreCombatItemsInQueue => _currentCombatItemQueue != null && _currentCombatItemQueue.Count > 0;

        public bool AreEnemiesLeft => NumberOfEnemies > 0;

        public CombatMapUnit CurrentCombatMapUnit => _initiativeQueue.GetCurrentCombatUnit();

        public CombatPlayer CurrentCombatPlayer =>
            _initiativeQueue.GetCurrentCombatUnit() is CombatPlayer player ? player : null;

        /// <summary>
        ///     Current player or enemy who is active in current round
        /// </summary>
        public PlayerCharacterRecord CurrentPlayerCharacterRecord => CurrentCombatPlayer?.Record;

        public bool InEscapeMode { get; set; } = false;
        public int NumberOfCombatItemInQueue => _currentCombatItemQueue.Count;

        public int NumberOfEnemies => CombatMapUnits.CurrentMapUnits.Enemies.Count(enemy => enemy.IsActive);

        public int NumberOfVisiblePlayers =>
            CombatMapUnits.CurrentMapUnits.CombatPlayers.Count(combatPlayer => combatPlayer.IsActive);

        public int Round => _initiativeQueue.Round;

        public PlayerCharacterRecord SelectedCombatPlayerRecord => _initiativeQueue.ActivePlayerCharacterRecord;

        public SingleCombatMapReference TheCombatMapReference { get; }

        public int Turn => _initiativeQueue.Turn;

        protected sealed override Dictionary<Point2D, TileOverrideReference> XYOverrides { get; }

        protected override bool IsRepeatingMap => false;

        /// <summary>
        ///     Creates CombatMap.
        ///     Note: Does not initialize the combat map units.
        /// </summary>
        /// <param name="singleCombatMapReference"></param>
        public CombatMap(SingleCombatMapReference singleCombatMapReference)
        {
            CombatMapUnits = GameStateReference.State.TheVirtualMap.TheMapUnits;
            TheCombatMapReference = singleCombatMapReference;
            XYOverrides = GameReferences.TileOverrideRefs.GetTileXYOverrides(singleCombatMapReference);

            InitializeAStarMap(WalkableType.CombatLand);
            InitializeAStarMap(WalkableType.CombatWater);
            InitializeAStarMap(WalkableType.CombatFlyThroughWalls);
            InitializeAStarMap(WalkableType.CombatLandAndWater);
        }

        /// <summary>
        ///     Creates enemies in the combat map. If the map contains hard coded enemies then it will ignore the
        ///     specified enemies
        /// </summary>
        /// <param name="singleCombatMapReference"></param>
        /// <param name="primaryEnemyReference"></param>
        /// <param name="nPrimaryEnemies"></param>
        /// <param name="secondaryEnemyReference"></param>
        /// <param name="nSecondaryEnemies"></param>
        /// <param name="npcRef"></param>
        internal void CreateEnemies(SingleCombatMapReference singleCombatMapReference,
            EnemyReference primaryEnemyReference, int nPrimaryEnemies, EnemyReference secondaryEnemyReference,
            int nSecondaryEnemies, NonPlayerCharacterReference npcRef)
        {
            int nEnemyIndex = 0;

            // dungeons do not have encountered based enemies (but where are the dragons???)
            if (singleCombatMapReference.MapTerritory == SingleCombatMapReference.Territory.Dungeon)
            {
                for (nEnemyIndex = 0; nEnemyIndex < SingleCombatMapReference.NUM_ENEMIES; nEnemyIndex++)
                {
                    _ = CreateEnemy(nEnemyIndex, singleCombatMapReference, primaryEnemyReference);
                }

                return;
            }

            if (npcRef != null) Debug.Assert(nPrimaryEnemies == 1 && nSecondaryEnemies == 0);

            // if there is only a single enemy then we always give them first position (such as NPC fights)
            if (nPrimaryEnemies == 1 && nSecondaryEnemies == 0)
            {
                _ = CreateEnemy(0, singleCombatMapReference, primaryEnemyReference);
                return;
            }

            // for regular combat maps, we introduce some randomness 
            Queue<int> monsterIndex = Utils.CreateRandomizedIntegerQueue(SingleCombatMapReference.NUM_ENEMIES);

            for (int nIndex = 0; nIndex < nPrimaryEnemies; nIndex++, nEnemyIndex++)
            {
                _ = CreateEnemy(monsterIndex.Dequeue(), singleCombatMapReference, primaryEnemyReference);
            }

            for (int nIndex = 0; nIndex < nSecondaryEnemies; nIndex++, nEnemyIndex++)
            {
                _ = CreateEnemy(monsterIndex.Dequeue(), singleCombatMapReference, secondaryEnemyReference);
            }
        }

        /// <summary>
        ///     Creates a party in the context of the combat map
        /// </summary>
        /// <param name="entryDirection">which direction did they enter from?</param>
        /// <param name="activeRecords">all character records</param>
        internal void CreateParty(SingleCombatMapReference.EntryDirection entryDirection,
            PlayerCharacterRecords activeRecords)
        {
            _playerCharacterRecords = activeRecords;

            // clear any previous combat map units
            CombatMapUnits.InitializeCombatMapReferences();

            List<Point2D> playerStartPositions = TheCombatMapReference.GetPlayerStartPositions(entryDirection);

            // cycle through each player and make a map unit
            for (int nPlayer = 0; nPlayer < activeRecords.GetNumberOfActiveCharacters(); nPlayer++)
            {
                PlayerCharacterRecord record = activeRecords.Records[nPlayer];

                CombatPlayer combatPlayer = new(record, playerStartPositions[nPlayer]);

                // make sure the tile that the player occupies is not walkable
                GetAStarByWalkableType(WalkableType.CombatLand).SetWalkable(playerStartPositions[nPlayer], false);

                CombatMapUnits.CurrentMapUnits.AllMapUnits[nPlayer] = combatPlayer;
            }
        }

        internal void InitializeInitiativeQueue()
        {
            _initiativeQueue = new InitiativeQueue(CombatMapUnits, _playerCharacterRecords, this);
            _initiativeQueue.InitializeInitiativeQueue();
            RefreshCurrentCombatPlayer();
        }

        // if hit, but not killed
        private static bool IsHitButNotKilled(CombatMapUnit.HitState hitState) =>
            hitState == CombatMapUnit.HitState.BarelyWounded || hitState == CombatMapUnit.HitState.LightlyWounded ||
            hitState == CombatMapUnit.HitState.HeavilyWounded || hitState == CombatMapUnit.HitState.CriticallyWounded ||
            hitState == CombatMapUnit.HitState.Fleeing;

        private void ClearCurrentCombatItemQueue()
        {
            if (_currentCombatItemQueue == null)
                _currentCombatItemQueue = new Queue<CombatItem>();
            else
                _currentCombatItemQueue.Clear();
        }

        private Enemy CreateEnemy(Point2D position, EnemyReference enemyReference)
        {
            Enemy enemy = CombatMapUnits.CreateEnemy(position, enemyReference, out int _);
            return enemy;
        }

        /// <summary>
        ///     Creates a single enemy in the context of the combat map.
        /// </summary>
        /// <param name="nEnemyIndex">0 based index that reflects the combat maps enemy index list</param>
        /// <param name="singleCombatMapReference">reference of the combat map</param>
        /// <param name="enemyReference">reference to enemy to be added (ignored for auto selected enemies)</param>
        /// <returns>the enemy that was just created</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private Enemy CreateEnemy(int nEnemyIndex, SingleCombatMapReference singleCombatMapReference,
            EnemyReference enemyReference)
        {
            SingleCombatMapReference.CombatMapSpriteType combatMapSpriteType =
                singleCombatMapReference.GetAdjustedEnemySprite(nEnemyIndex, out int nEnemySprite);
            Point2D enemyPosition = singleCombatMapReference.GetEnemyPosition(nEnemyIndex);

            Enemy enemy = null;
            switch (combatMapSpriteType)
            {
                case SingleCombatMapReference.CombatMapSpriteType.Nothing:
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.Thing:
                    Debug.WriteLine("It's a chest or maybe a dead body!");
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.AutoSelected:
                    enemy = CombatMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
                        GameReferences.EnemyRefs.GetEnemyReference(nEnemySprite), out int _);
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.EncounterBased:
                    Debug.Assert(!(enemyPosition.X == 0 && enemyPosition.Y == 0));
                    enemy = CombatMapUnits.CreateEnemy(singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
                        enemyReference, out int _);
                    break;
                default:
                    throw new InvalidEnumArgumentException(((int)combatMapSpriteType).ToString());
            }

            // make sure the tile that the enemy occupies is not walkable
            // we do both land and water in case there are overlapping tiles (which there shouldn't be!?)
            SetNotWalkableDueToCombatMapUnit(enemyPosition);
            return enemy;
        }

        private T GetClosestCombatMapUnitInRange<T>(CombatMapUnit attackingUnit, int nRange) where T : CombatMapUnit
        {
            double dBestDistanceToAttack = 150f;
            T bestOpponent = null;

            foreach (CombatMapUnit combatMapUnit in CombatMapUnits.CombatMapMapUnitCollection.AllCombatMapUnits)
            {
                if (!(combatMapUnit is T enemy)) continue;
                if (!IsCombatMapUnitInRange(attackingUnit, enemy, nRange)) continue;
                // if the enemy unit is invisible or charmed then they should not be targeted
                if (enemy.IsInvisible || enemy.IsCharmed) continue;

                double dDistance = enemy.MapUnitPosition.XY.DistanceBetween(attackingUnit.MapUnitPosition.XY);
                if (dDistance < dBestDistanceToAttack)
                {
                    dBestDistanceToAttack = dDistance;
                    bestOpponent = enemy;
                }
            }

            return bestOpponent;
        }

        private CombatPlayer GetClosestCombatPlayerInRange(Enemy enemy)
        {
            return GetClosestCombatMapUnitInRange<CombatPlayer>(enemy, enemy.EnemyReference.AttackRange);
        }

        private int GetCombatMapUnitIndex(CombatMapUnit combatMapUnit)
        {
            if (combatMapUnit == null) return -1;
            for (int i = 0; i < CombatMapUnits.CurrentMapUnits.AllMapUnits.Count; i++)
            {
                if (CombatMapUnits.CurrentMapUnits.AllMapUnits[i] == combatMapUnit) return i;
            }

            return -1;
        }

        private CombatPlayer GetCurrentCombatPlayer()
        {
            CombatMapUnit activeCombatMapUnit = _initiativeQueue.GetCurrentCombatUnit();

            if (!(activeCombatMapUnit is CombatPlayer combatPlayer))
                throw new Ultima5ReduxException("Tried to get CurrentCombatPlayer, but there isn't one");

            return combatPlayer;
        }

        private int GetNextAvailableCombatMapUnitIndex()
        {
            for (int i = 0; i < CombatMapUnits.CurrentMapUnits.AllMapUnits.Count; i++)
            {
                if (CombatMapUnits.CurrentMapUnits.AllMapUnits[i] is EmptyMapUnit) return i;
            }

            return -1;
        }

        /// <summary>
        ///     Gets a random empty space surrounding a particular enemy. Typically for things like enemy division
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns>an available point, or null if no points are available</returns>
        private Point2D GetRandomEmptySpaceAroundEnemy(Enemy enemy)
        {
            List<Point2D> surroundingPoints =
                enemy.MapUnitPosition.XY.GetConstrainedSurroundingPoints(1, NumOfXTiles - 1, NumOfYTiles - 1);
            List<Point2D> emptySpacePoints = new();
            foreach (Point2D point in surroundingPoints)
            {
                // the check for IsTileWalkable may be redundant, but just in case
                // we create a list of potential free spaces around the enemy
                bool bIsAStarWalkable = GetAStarByMapUnit(enemy).GetWalkable(point);
                if (IsTileWalkable(point, GetWalkableTypeByEnemy(enemy))
                    //WalkableType.CombatLand) 
                    && bIsAStarWalkable)
                {
                    emptySpacePoints.Add(point);
                }
            }

            if (emptySpacePoints.Count == 0) return null;
            Point2D randomPoint = emptySpacePoints[Utils.GetNumberBetween(0, emptySpacePoints.Count - 1)];
            return (randomPoint);
        }

        /// <summary>
        ///     Returns a random point surrounding a particular point - that ISN'T "notThisPoint"
        ///     Think about it in terms of missing - you missed the enemy (notThisPoint) so you hit
        ///     some other tile surrounding it
        /// </summary>
        /// <param name="surroundThisPoint"></param>
        /// <param name="notThisPoint"></param>
        /// <returns></returns>
        private Point2D GetRandomSurroundingPointThatIsnt(Point2D surroundThisPoint, Point2D notThisPoint)
        {
            List<Point2D> surroundingCombatPlayerPoints =
                surroundThisPoint.GetConstrainedSurroundingPoints(1, NumOfXTiles - 1, NumOfYTiles - 1);
            Point2D randomSurroundingPoint;
            Random random = new();
            do
            {
                int nIndex = random.Next() % surroundingCombatPlayerPoints.Count;
                randomSurroundingPoint = surroundingCombatPlayerPoints[nIndex];
                if (randomSurroundingPoint != notThisPoint)
                    break;
                surroundingCombatPlayerPoints.RemoveAt(nIndex);
            } while (true);

            return randomSurroundingPoint;
        }

        private WalkableType GetWalkableTypeByEnemy(Enemy enemy)
        {
            WalkableType walkableType;
            if (enemy.EnemyReference.IsWaterEnemy)
                walkableType = WalkableType.CombatWater;
            else if (enemy.EnemyReference.CanPassThroughWalls)
                walkableType = WalkableType.CombatFlyThroughWalls;
            else if (enemy.EnemyReference.CanFlyOverWater)
                walkableType = WalkableType.CombatLandAndWater;
            else
                walkableType = WalkableType.CombatLand;
            return walkableType;
        }

        private AdditionalHitStateAction HandleHitState(CombatMapUnit.HitState hitState,
            CombatMapUnit affectedCombatMapUnit)
        {
            AdditionalHitStateAction additionalHitStateAction = AdditionalHitStateAction.None;
            // some things only occur if they are hit - but not if they are killed or missed
            if (IsHitButNotKilled(hitState) && affectedCombatMapUnit is Enemy enemy &&
                enemy.EnemyReference.IsEnemyAbility(EnemyReference.EnemyAbility.DivideOnHit) && Utils.OneInXOdds(2))
            {
                // do they multiply?
                Enemy newEnemy = DivideEnemy(enemy);
                if (newEnemy != null) additionalHitStateAction = AdditionalHitStateAction.EnemyDivided;
            }

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
                    break;
                case CombatMapUnit.HitState.Dead:
                    if (affectedCombatMapUnit is Enemy deadEnemy)
                    {
                        // if the enemy was an NPC then we kill them!
                        if (deadEnemy.NPCRef != null)
                        {
                            deadEnemy.NPCState.IsDead = true;
                        }

                        RecalculateWalkableTile(affectedCombatMapUnit.MapUnitPosition.XY, WalkableType.CombatLand);
                        RecalculateWalkableTile(affectedCombatMapUnit.MapUnitPosition.XY, WalkableType.CombatWater);
                        RecalculateWalkableTile(affectedCombatMapUnit.MapUnitPosition.XY,
                            WalkableType.CombatFlyThroughWalls);
                        RecalculateWalkableTile(affectedCombatMapUnit.MapUnitPosition.XY,
                            WalkableType.CombatLandAndWater);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hitState), hitState, null);
            }

            return additionalHitStateAction;
        }

        private TurnResult HandleRangedMissed(CombatMapUnit attackingCombatMapUnit, Point2D attackPosition,
            out CombatMapUnit targetedCombatMapUnit, int nAttackMax, out Point2D missedPoint, out string outputStr)
        {
            // they are ranged, and missed which means we need to pick a new tile to attack
            // get a list of all surround tiles surrounding the player that they are attacking
            outputStr = "";
            missedPoint = null;

            Point2D newAttackPosition = GetRandomSurroundingPointThatIsnt(attackPosition, attackPosition);
            Debug.Assert(newAttackPosition != null);

            targetedCombatMapUnit = GetCombatUnit(newAttackPosition);

            // We will check the raycast and determine if the missed shot in fact actually gets blocked - 
            // doing it in here will ensure that no damage is computed against an enemy if they are targeted
            // OR if the attack position is outside of the current map area, it means that a player has left the map
            if (IsRangedPathBlocked(attackPosition, newAttackPosition, out missedPoint) ||
                newAttackPosition.IsOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1))
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
            _ = attackingCombatMapUnit.Attack(targetedCombatMapUnit, nAttackMax,
                out string missedAttackOutputStr, out string debugStr, true);
            // temporary for debugging
            missedAttackOutputStr += "\n" + debugStr;
            outputStr += "\n" + missedAttackOutputStr;
            AdvanceToNextCombatMapUnit();
            return TurnResult.EnemyMissedButHit;
        }

        private bool IsCombatMapUnitInRange(CombatMapUnit attackingUnit, CombatMapUnit opponentCombatMapUnit,
            int nRange)
        {
            if (!opponentCombatMapUnit.IsActive) return false;
            if (!opponentCombatMapUnit.IsAttackable) return false;
            if (nRange == 1 && !attackingUnit.CanReachForMeleeAttack(opponentCombatMapUnit, nRange)) return false;
            if (nRange > 1 &&
                IsRangedPathBlocked(attackingUnit.MapUnitPosition.XY, opponentCombatMapUnit.MapUnitPosition.XY, out _))
                return false;

            return true;
        }

        private bool IsRangedPathBlocked(Point2D attackingPoint, Point2D opponentMapUnit, out Point2D firstBlockPoint)
        {
            // get the points between the player and opponent
            List<Point2D> points = attackingPoint.Raytrace(opponentMapUnit);
            for (int i = 0; i < points.Count - 1; i++)
            {
                Point2D point = points[i];
                if (!IsTileRangePathBlocked(point))
                    continue;

                // we can't penetrate this thing and need to give up
                firstBlockPoint = point;
                return true;
            }

            firstBlockPoint = null;
            return false;
        }

        private bool IsTileRangePathBlocked(Point2D xy)
        {
            TileReference tileReference = GetTileReference(xy);
            return !tileReference.RangeWeapon_Passable;
        }

        private bool IsWalkingPassable(TileReference tileReference) => tileReference.IsWalking_Passable ||
                                                                       tileReference.Index == GameReferences
                                                                           .SpriteTileReferences
                                                                           .GetTileReferenceByName("RegularDoor")
                                                                           .Index || tileReference.Index ==
                                                                       GameReferences.SpriteTileReferences
                                                                           .GetTileReferenceByName("RegularDoorView")
                                                                           .Index;

        /// <summary>
        ///     Moves the combat map unit to the CombatPlayer for whom they can reach in the fewest number of steps
        /// </summary>
        /// <param name="activeCombatUnit">the combat map unit that wants to attack a combat player</param>
        /// <param name="preferredAttackTarget">the type of target you will target</param>
        /// <param name="bMoved">did the CombatMapUnit move</param>
        /// <returns>The combat player that they are heading towards</returns>
        private CombatMapUnit MoveToClosestAttackableCombatMapUnit(CombatMapUnit activeCombatUnit,
            SpecificCombatMapUnit preferredAttackTarget, out bool bMoved)
        {
            if (activeCombatUnit == null)
                throw new Ultima5ReduxException("Passed a null active combat unit when moving to closest unit");

            const int NoPath = 0xFFFF;
            bMoved = false;

            int nMinMoves = 0xFFFF;
            Stack<Node> preferredRoute = null;
            CombatMapUnit preferredAttackVictim = null;

            AStar aStar = GetAStarByMapUnit(activeCombatUnit);

            List<Point2D> potentialTargetsPoints = new();

            foreach (CombatMapUnit combatMapUnit in GetActiveCombatMapUnitsByType(preferredAttackTarget))
            {
                Point2D combatMapUnitXY = combatMapUnit.MapUnitPosition.XY;

                potentialTargetsPoints.Add(combatMapUnitXY);

                // get the shortest path to the unit - we ignore the range value because by calling this method we are insisting
                // that they move
                Stack<Node> theWay = aStar.FindPath(activeCombatUnit.MapUnitPosition.XY, combatMapUnitXY);
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
                List<Point2D> surroundingPoints =
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
                List<Point2D> wanderablePoints = new();
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
                    Random ran = new();

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

            if (preferredRoute == null)
                throw new Ultima5ReduxException("Preferred route object was oddly empty");

            Point2D nextPosition = preferredRoute.Pop().Position;

            MoveActiveCombatMapUnit(nextPosition);
            bMoved = true;

            return preferredAttackVictim;
        }

        private CombatPlayer MoveToClosestAttackableCombatPlayer(CombatMapUnit activeCombatUnit, out bool bMoved) =>
            MoveToClosestAttackableCombatMapUnit(activeCombatUnit, SpecificCombatMapUnit.CombatPlayer, out bMoved) as
                CombatPlayer;

        private Enemy MoveToClosestAttackableEnemy(CombatMapUnit activeMapUnit, out string outputStr, out bool bMoved)
        {
            Enemy enemy =
                MoveToClosestAttackableCombatMapUnit(activeMapUnit, SpecificCombatMapUnit.Enemy, out bMoved) as Enemy;
            outputStr = "";
            if (enemy == null)
            {
                if (bMoved) outputStr = $"{activeMapUnit.FriendlyName} moved.\nUnable to target enemy.";
                else outputStr = "Unable to target or advance on enemy.";
            }
            else
            {
                if (CurrentCombatPlayer == null)
                    throw new Ultima5ReduxException(
                        "Current combat player was null when trying to move closer to an enemy");
                outputStr = $"{CurrentCombatPlayer.FriendlyName} advances on {enemy.FriendlyName}";
            }

            AdvanceToNextCombatMapUnit();
            return enemy;
        }

        private void RefreshCurrentCombatPlayer()
        {
            if (CurrentCombatPlayer is null)
            {
                ClearCurrentCombatItemQueue();
                return;
            }

            List<CombatItem> combatItems = CurrentCombatPlayer.GetAttackWeapons();
            BuildCombatItemQueue(combatItems);
        }

        private void SetNotWalkableDueToCombatMapUnit(Point2D position)
        {
            GetAStarByWalkableType(WalkableType.CombatLand).SetWalkable(position, false);
            GetAStarByWalkableType(WalkableType.CombatWater).SetWalkable(position, false);
            GetAStarByWalkableType(WalkableType.CombatFlyThroughWalls).SetWalkable(position, false);
            GetAStarByWalkableType(WalkableType.CombatLandAndWater).SetWalkable(position, false);
        }

        /// <summary>
        ///     Recalculates which tiles are visible based on position of players in map and the current map
        /// </summary>
        /// <param name="_"></param>
        public override void RecalculateVisibleTiles(in Point2D _)
        {
            // if we are in an overworld combat map then everything is always visible (I think!?)
            if (TheCombatMapReference.MapTerritory == SingleCombatMapReference.Territory.Britannia || XRayMode)
            {
                VisibleOnMap = Utils.Init2DBoolArray(NumOfXTiles, NumOfYTiles, true);
                return;
            }

            // reinitialize the array for all potential party members
            IEnumerable<CombatPlayer> combatPlayers = AllCombatPlayers;

            RefreshTestForVisibility(MapUnits.MapUnits.MAX_MAP_CHARACTERS);

            int nIndex = 0;
            foreach (CombatPlayer combatPlayer in combatPlayers)
            {
                FloodFillMap(combatPlayer.MapUnitPosition.X, combatPlayer.MapUnitPosition.Y, true, nIndex,
                    combatPlayer.MapUnitPosition.XY, true);
                nIndex++;
            }

            TouchedOuterBorder = false;
        }

        public CombatMapUnit AdvanceToNextCombatMapUnit()
        {
            CombatMapUnit combatMapUnit = _initiativeQueue.AdvanceToNextCombatMapUnit();

            _bPlayerHasChanged = true;

            RefreshCurrentCombatPlayer();
            return combatMapUnit;
        }

        public void BuildCombatItemQueue(List<CombatItem> combatItems) =>
            _currentCombatItemQueue = new Queue<CombatItem>(combatItems);

        public CombatItem DequeueCurrentCombatItem() => _currentCombatItemQueue.Dequeue();

        /// <summary>
        ///     Takes an enemy and divides them on the combat map, making a fresh copy of them
        /// </summary>
        /// <returns>the new enemy if they did divide, otherwise null</returns>
        /// <param name="enemy"></param>
        public Enemy DivideEnemy(Enemy enemy)
        {
            // is there a free spot surrounding the enemy?
            Point2D newEnemyPosition = GetRandomEmptySpaceAroundEnemy(enemy);
            if (newEnemyPosition == null)
                return null;

            // creates a new enemy of the same type and returns it
            int nNextCombatMapUnitIndex = GetNextAvailableCombatMapUnitIndex();
            if (nNextCombatMapUnitIndex == -1) return null;

            Enemy newEnemy = CreateEnemy(newEnemyPosition, enemy.EnemyReference);

            if (newEnemy == null)
                throw new Ultima5ReduxException("Tried to divide enemy, but they were null: " +
                                                enemy.EnemyReference.KeyTileReference.Name);
            newEnemy.MapUnitPosition = new MapUnitPosition(newEnemyPosition.X, newEnemyPosition.Y, 0);
            _initiativeQueue.AddCombatMapUnitToQueue(newEnemy);
            return newEnemy;
        }

        public IEnumerable<CombatMapUnit> GetActiveCombatMapUnitsByType(SpecificCombatMapUnit specificCombatMapUnit)
        {
            switch (specificCombatMapUnit)
            {
                case SpecificCombatMapUnit.All:
                    return AllVisibleAttackableCombatMapUnits;
                case SpecificCombatMapUnit.CombatPlayer:
                    return AllCombatPlayersGeneric.Where(combatPlayer => combatPlayer.IsActive);
                case SpecificCombatMapUnit.Enemy:
                    return AllEnemiesGeneric.Where(enemy => enemy.IsActive);
                default:
                    throw new ArgumentOutOfRangeException(nameof(specificCombatMapUnit), specificCombatMapUnit, null);
            }
        }

        public Enemy GetClosestEnemyInRange(CombatPlayer attackingCombatPlayer, CombatItem combatItem)
        {
            return GetClosestCombatMapUnitInRange<Enemy>(attackingCombatPlayer,
                combatItem.TheCombatItemReference.Range);
        }

        public CombatPlayer GetCombatPlayer(PlayerCharacterRecord record) =>
            CombatMapUnits.CurrentMapUnits.CombatPlayers.FirstOrDefault(player => player.Record == record);

        public string GetCombatPlayerOutputText()
        {
            CombatPlayer combatPlayer = GetCurrentCombatPlayer();
            if (combatPlayer == null)
                throw new Ultima5ReduxException("Invalid Combat Player");
            return combatPlayer.Record.Name + ", armed with " + combatPlayer.GetAttackWeaponsString();
        }

        public CombatMapUnit GetCombatUnit(Point2D unitPosition)
        {
            MapUnit mapUnit = GameStateReference.State.TheVirtualMap.GetTopVisibleMapUnit(unitPosition, true);

            if (mapUnit is CombatMapUnit unit) return unit;

            return null;
        }

        /// <summary>
        ///     Gets all tiles that are at the edge of the screen and are escapable based on the given position
        /// </summary>
        /// <param name="fromPosition"></param>
        /// <param name="walkableType"></param>
        /// <returns>a list of all potential positions</returns>
        public List<Point2D> GetEscapablePoints(Point2D fromPosition, WalkableType walkableType)
        {
            _ = fromPosition;
            List<Point2D> points = new();

            for (int nIndex = 0; nIndex < NumOfXTiles; nIndex++)
            {
                Point2D top = new(nIndex, 0);
                Point2D bottom = new(nIndex, NumOfYTiles - 1);
                Point2D left = new(0, nIndex);
                Point2D right = new(NumOfXTiles - 1, nIndex);

                if (IsTileWalkable(top, walkableType)) points.Add(top);
                if (IsTileWalkable(bottom, walkableType)) points.Add(bottom);

                if (nIndex == 0 || nIndex == NumOfYTiles - 1) continue; // we don't double count the top or bottom 

                if (IsTileWalkable(left, walkableType)) points.Add(left);
                if (IsTileWalkable(right, walkableType)) points.Add(right);
            }

            return points;
        }

        /// <summary>
        ///     Gets the best escape route based on current position
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

        public Enemy GetFirstEnemy(CombatItem combatItem) => GetNextEnemy(null, combatItem);

        public bool GetHasPlayerChangedSinceLastCheckAndReset()
        {
            if (!_bPlayerHasChanged) return false;
            _bPlayerHasChanged = false;
            return true;
        }

        public Enemy GetNextEnemy(Enemy currentEnemy, CombatItem combatItem)
        {
            int nOffset = GetCombatMapUnitIndex(currentEnemy);
            // -1 indicates it wasn't found, so could be dead or null. We set it to the beginning
            if (nOffset == -1) nOffset = 0;

            int nMapUnits = CombatMapUnits.CurrentMapUnits.AllMapUnits.Count;

            for (int i = 0; i < nMapUnits; i++)
            {
                // we start at the next position, and wrap around ensuring we have hit all possible enemies
                int nIndex = (i + nOffset + 1) % nMapUnits;
                if (!(CombatMapUnits.CurrentMapUnits.AllMapUnits[nIndex] is Enemy enemy)) continue;
                if (!enemy.IsActive) continue;
                if (CurrentCombatPlayer == null)
                    throw new Ultima5ReduxException("Tried to get next enemy, but couldn't find the active player");
                if (CurrentCombatPlayer.CanReachForAttack(enemy, combatItem)) return enemy;
            }

            return null;
        }

        public List<CombatMapUnit> GetTopNCombatMapUnits(int nUnits)
        {
            // if there aren't the minimum number of turns, then we force it to add additional turns to at least
            // the stated number of units
            if (_initiativeQueue.TotalTurnsInQueue < nUnits)
                _initiativeQueue.CalculateNextInitiativeQueue();

            return _initiativeQueue.GetTopNCombatMapUnits(nUnits);
        }

        public void MakePlayerEscape(CombatPlayer combatPlayer)
        {
            Debug.Assert(!combatPlayer.HasEscaped);

            combatPlayer.HasEscaped = true;
            AdvanceToNextCombatMapUnit();
        }

        /// <summary>
        ///     Moves the active combat unit to a new map location
        /// </summary>
        /// <param name="xy"></param>
        public void MoveActiveCombatMapUnit(Point2D xy)
        {
            CombatMapUnit currentCombatUnit = _initiativeQueue.GetCurrentCombatUnit();
            if (currentCombatUnit == null)
                throw new Ultima5ReduxException(
                    "Tried to move active combat unit, but couldn't find them in initiative queue");
            // reset the a star walking rules
            RecalculateWalkableTile(currentCombatUnit.MapUnitPosition.XY, WalkableType.CombatLand);
            RecalculateWalkableTile(currentCombatUnit.MapUnitPosition.XY, WalkableType.CombatWater);
            RecalculateWalkableTile(currentCombatUnit.MapUnitPosition.XY, WalkableType.CombatFlyThroughWalls);
            RecalculateWalkableTile(currentCombatUnit.MapUnitPosition.XY, WalkableType.CombatLandAndWater);

            currentCombatUnit.MapUnitPosition = new MapUnitPosition(xy.X, xy.Y, 0);

            WalkableType walkableType = GetWalkableTypeByMapUnit(currentCombatUnit);
            SetWalkableTile(xy, false, walkableType);
        }

        public Enemy MoveToClosestAttackableEnemy(out string outputStr, out bool bMoved) =>
            MoveToClosestAttackableEnemy(CurrentCombatPlayer, out outputStr, out bMoved);

        /// <summary>
        ///     Makes the next available character escape
        /// </summary>
        /// <param name="escapedPlayer">the player who escaped, or null if none left</param>
        /// <returns>true if a player escaped, false if none were found</returns>
        public bool NextCharacterEscape(out CombatPlayer escapedPlayer)
        {
            foreach (CombatPlayer combatPlayer in CombatMapUnits.CurrentMapUnits.CombatPlayers)
            {
                if (combatPlayer.HasEscaped) continue;

                MakePlayerEscape(combatPlayer);
                escapedPlayer = combatPlayer;
                return true;
            }

            escapedPlayer = null;
            return false;
        }

        public CombatItem PeekCurrentCombatItem() => _currentCombatItemQueue.Peek();

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
                        return TurnResult.CombatPlayerMissed;
                    }

                    // if the top most unit is a combat map unit, then let's fight!
                    preAttackOutputStr += opponentCombatMapUnit.Name;

                    weapon = _currentCombatItemQueue.Dequeue();

                    preAttackOutputStr += " with " + weapon.LongName + "!";

                    // let's first make sure that any range weapons do not hit a wall first!
                    if (weapon.TheCombatItemReference.Range > 1)
                    {
                        if (opponentMapUnit == null)
                            throw new Ultima5ReduxException("Opponent map unit is null");
                        if (combatPlayer == null)
                            throw new Ultima5ReduxException("Combat player is null");

                        bool bIsBlocked = IsRangedPathBlocked(combatPlayer.MapUnitPosition.XY,
                            opponentMapUnit.MapUnitPosition.XY, out missedPoint);
                        if (bIsBlocked)
                        {
                            postAttackOutputStr = combatPlayer.FriendlyName +
                                                  GameReferences.DataOvlRef.StringReferences
                                                      .GetString(DataOvlReference.BattleStrings._MISSED_BANG_N)
                                                      .TrimEnd().Replace("!", " ") + opponentMapUnit.FriendlyName +
                                                  " because it was blocked!";

                            AdvanceToNextCombatMapUnit();
                            targetedHitState = CombatMapUnit.HitState.Missed;
                            return TurnResult.CombatPlayerBlocked;
                        }
                    }

                    // do the attack logic
                    if (combatPlayer == null)
                        throw new Ultima5ReduxException("Combat player unexpectedly null");
                    targetedHitState = combatPlayer.Attack(opponentCombatMapUnit, weapon, out string stateOutput,
                        out string debugStr);
                    stateOutput += "\n" + debugStr;
                    postAttackOutputStr = stateOutput;

                    // if the player attacks, but misses with a range weapon the we need see if they
                    // accidentally hit someone else
                    bool bMissedButHit = false;
                    if (targetedHitState == CombatMapUnit.HitState.Missed && weapon.TheCombatItemReference.Range > 1)
                    {
                        TurnResult turnResult = HandleRangedMissed(combatPlayer,
                            opponentCombatMapUnit.MapUnitPosition.XY, out targetedCombatMapUnit,
                            weapon.TheCombatItemReference.AttackStat, out missedPoint, out string addStr);
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

                    AdditionalHitStateAction additionalHitStateAction =
                        HandleHitState(targetedHitState, targetedCombatMapUnit);

                    switch (additionalHitStateAction)
                    {
                        case AdditionalHitStateAction.EnemyDivided:
                            postAttackOutputStr += "\n" + targetedCombatMapUnit.FriendlyName + GameReferences.DataOvlRef
                                .StringReferences.GetString(DataOvlReference.Battle2Strings._DIVIDES_BANG_N).TrimEnd();
                            break;
                        case AdditionalHitStateAction.None:
                        default:
                            break;
                    }

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
                    throw new InvalidEnumArgumentException(((int)selectedAction).ToString());
            }
        }

        /// <summary>
        ///     Attempts to processes the turn of the current combat unit - either CombatPlayer or Enemy.
        ///     Can result in advancing to next turn, or indicate user input required
        /// </summary>
        /// <param name="activeCombatMapUnit">the combat unit that is taking the action</param>
        /// <param name="targetedCombatMapUnit">an optional unit that is being affected by the active combat unit</param>
        /// <param name="preAttackOutputStr"></param>
        /// <param name="postAttackOutputStr"></param>
        /// <param name="missedPoint">if the target is empty or missed then this gives the point that the attack landed</param>
        /// <returns></returns>
        public TurnResult ProcessEnemyTurn(out CombatMapUnit activeCombatMapUnit,
            out CombatMapUnit targetedCombatMapUnit, out string preAttackOutputStr, out string postAttackOutputStr,
            out Point2D missedPoint)
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
            if (enemy == null)
                throw new Ultima5ReduxException("Enemy unexpectedly null");
            if (enemy.IsCharmed)
            {
                // the player get's to control the enemy!
                preAttackOutputStr = enemy.FriendlyName + ":";

                return TurnResult.RequireCharacterInput;
            }

            if (enemy.IsSleeping)
            {
                preAttackOutputStr = enemy.FriendlyName + ": Sleeping";
                return TurnResult.Sleeping;
            }

            bool bAtLeastOnePlayerSeenOnCombatMap = _playerCharacterRecords.AtLeastOnePlayerSeenOnCombatMap;

            // the enemy is badly wounded and is going to try to escape OR if no players are visible on the current combat map
            // HOWEVER if the enemy is immobile (like Reaper) then they will just keep attacking and skip the escape
            if ((enemy.IsFleeing || !bAtLeastOnePlayerSeenOnCombatMap) && !enemy.EnemyReference.DoesNotMove)
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

                TileReference tileReference = enemy.FleeingPath != null
                    ? GetTileReference(enemy.FleeingPath.Peek().Position)
                    : null;
                CombatMapUnit combatMapUnit =
                    enemy.FleeingPath != null ? GetCombatUnit(enemy.FleeingPath.Peek().Position) : null;
                bool bIsTileWalkable = false;
                Debug.Assert(tileReference != null);

                WalkableType walkableType = GetWalkableTypeByEnemy(enemy);

                if (combatMapUnit == null)
                {
                    bIsTileWalkable = IsTileWalkable(enemy.MapUnitPosition.XY, walkableType);
                }

                // does the monster not yet have a flee path OR
                // does the enemy have a flee path already established that is now block OR
                if (enemy.FleeingPath == null || !bIsTileWalkable)
                {
                    enemy.FleeingPath = GetEscapeRoute(enemy.MapUnitPosition.XY, walkableType);
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
            bool bPreviousTargetPresent =
                enemy.PreviousAttackTarget != null && enemy.PreviousAttackTarget.Stats.CurrentHp > 0;
            bool bPreviousTargetInRange;
            // if it is a melee attacked then check for melee attack distance otherwise check for ranged blockage and distance
            bool bPreviousTargetUnAttackable = enemy.PreviousAttackTarget != null &&
                                               (enemy.PreviousAttackTarget.IsInvisible ||
                                                enemy.PreviousAttackTarget.IsCharmed);
            if (enemy.EnemyReference.AttackRange == 1)
                bPreviousTargetInRange = bPreviousTargetPresent && !bPreviousTargetUnAttackable &&
                                         (enemy.CanReachForMeleeAttack(enemy.PreviousAttackTarget));
            else
                bPreviousTargetInRange = bPreviousTargetPresent && !bPreviousTargetUnAttackable &&
                                         !IsRangedPathBlocked(enemy.MapUnitPosition.XY,
                                             enemy.PreviousAttackTarget.MapUnitPosition.XY, out _);

            CombatMapUnit bestCombatPlayer = bPreviousTargetInRange
                ? enemy.PreviousAttackTarget
                : GetClosestCombatPlayerInRange(enemy);

            Debug.Assert(bestCombatPlayer?.IsAttackable ?? true);

            // if the best combat player is attackable and reachable, then we do just that!
            if (bestCombatPlayer != null)
            {
                CombatMapUnit.HitState hitState = enemy.Attack(bestCombatPlayer,
                    enemy.EnemyReference.TheDefaultEnemyStats.Damage, out postAttackOutputStr, out string debugStr);
                // temporary for debugging
                postAttackOutputStr += "\n" + debugStr;
                switch (hitState)
                {
                    case CombatMapUnit.HitState.Missed:
                        // oh oh - the enemy missed
                        if (enemy.EnemyReference.TheMissileType == CombatItemReference.MissileType.None)
                        {
                            targetedCombatMapUnit = bestCombatPlayer;
                            break;
                        }

                        Debug.Assert(enemy.EnemyReference.AttackRange > 1,
                            "Cannot have a ranged weapon if no missile type set");

                        TurnResult turnResult = HandleRangedMissed(enemy, bestCombatPlayer.MapUnitPosition.XY,
                            out targetedCombatMapUnit, enemy.EnemyReference.TheDefaultEnemyStats.Damage,
                            out missedPoint, out string addStr);

                        postAttackOutputStr += addStr;
                        return turnResult;
                    case CombatMapUnit.HitState.Grazed:
                    case CombatMapUnit.HitState.BarelyWounded:
                    case CombatMapUnit.HitState.LightlyWounded:
                    case CombatMapUnit.HitState.HeavilyWounded:
                    case CombatMapUnit.HitState.CriticallyWounded:
                    case CombatMapUnit.HitState.Dead:
                    case CombatMapUnit.HitState.Fleeing:
                        targetedCombatMapUnit = bestCombatPlayer;
                        break;
                    case CombatMapUnit.HitState.None:
                    default:
                        throw new InvalidEnumArgumentException(((int)hitState).ToString());
                }

                AdvanceToNextCombatMapUnit();
                return hitState == CombatMapUnit.HitState.Grazed ? TurnResult.EnemyGrazed : TurnResult.EnemyAttacks;
            }

            CombatMapUnit pursuedCombatMapUnit = null;
            bool bMoved = false;
            if (!enemy.EnemyReference.DoesNotMove)
            {
                pursuedCombatMapUnit = MoveToClosestAttackableCombatPlayer(enemy, out bMoved);
            }

            if (bMoved)
            {
                // we have exhausted all potential attacking possibilities, so instead we will just move 
                preAttackOutputStr = enemy.EnemyReference.MixedCaseSingularName + " moved.";
                if (pursuedCombatMapUnit != null)
                    preAttackOutputStr += "\nThey really seem to have it in for " + pursuedCombatMapUnit.FriendlyName +
                                          "!";
            }
            else
            {
                preAttackOutputStr = enemy.EnemyReference.MixedCaseSingularName + " is unable to move or attack.";
            }

            AdvanceToNextCombatMapUnit();

            return TurnResult.EnemyMoved;
        }

        public void SetActivePlayerCharacter(PlayerCharacterRecord record)
        {
            _initiativeQueue.SetActivePlayerCharacter(record);
            _bPlayerHasChanged = true;
            RefreshCurrentCombatPlayer();
        }

        protected override float GetAStarWeight(in Point2D xy)
        {
            return 1.0f;
        }

        protected override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit)
        {
            return mapUnit switch
            {
                Enemy enemy => GetWalkableTypeByEnemy(enemy),
                CombatPlayer _ => WalkableType.CombatLand,
                _ => WalkableType.StandardWalking
            };
        }

        protected override bool IsTileWalkable(TileReference tileReference, WalkableType walkableType)
        {
            switch (walkableType)
            {
                case WalkableType.CombatWater:
                    return tileReference.IsWaterEnemyPassable;
                case WalkableType.CombatFlyThroughWalls:
                    // if you can fly through walls, then you can fly through anything except people and enemies
                    return true;
                case WalkableType.CombatLandAndWater:
                    return IsWalkingPassable(tileReference) || tileReference.IsWaterEnemyPassable;
                case WalkableType.CombatLand:
                    return IsWalkingPassable(tileReference);
                default:
                    throw new Ultima5ReduxException(
                        "Someone is trying to walk to determine they can walk on an unfamiliar WalkableType");
            }
        }
    }
}