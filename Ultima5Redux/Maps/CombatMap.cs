using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Ultima5Redux.External;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;
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
        private readonly MapUnits.MapUnits _mapUnits;

        //private readonly MapOverrides _mapOverrides;
        // when computing what should happen when a player hits - these states can be triggered 

        public enum SelectionAction { None, Magic, Attack }

        public enum SpecificCombatMapUnit { All, CombatPlayer, Enemy, NonAttackUnit }

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

        public override bool IsRepeatingMap => false;

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

        public Enemy ActiveEnemy => _initiativeQueue.GetCurrentCombatUnitAndClean() is Enemy enemy ? enemy : null;

        /// <summary>
        ///     The player character who the player has selected to focus on (#1-#6)
        /// </summary>
        /// <returns>active player character record OR null if none selected</returns>
        public PlayerCharacterRecord ActivePlayerCharacterRecord => _initiativeQueue.ActivePlayerCharacterRecord;

        public IEnumerable<CombatPlayer> AllCombatPlayers => CombatMapUnits.CurrentMapUnits.CombatPlayers;
        public IEnumerable<Enemy> AllEnemies => CombatMapUnits.CurrentMapUnits.Enemies;

        public IEnumerable<MapUnit> AllMapUnits => CombatMapUnits.CurrentMapUnits.AllMapUnits;
        public IEnumerable<NonAttackingUnit> AllNonAttackUnits => CombatMapUnits.CurrentMapUnits.NonAttackingUnits;

        public IEnumerable<CombatMapUnit> AllVisibleAttackableCombatMapUnits =>
            CombatMapUnits.CurrentMapUnits.AllCombatMapUnits.Where(combatMapUnit =>
                combatMapUnit.IsAttackable && combatMapUnit.IsActive);

        public IEnumerable<CombatMapUnit> AllVisibleCombatMapUnits =>
            CombatMapUnits.CurrentMapUnits.AllCombatMapUnits.Where(combatMapUnit =>
                combatMapUnit.IsActive);

        public bool AreCombatItemsInQueue => _currentCombatItemQueue is { Count: > 0 };

        public bool AreEnemiesLeft => NumberOfEnemies > 0;

        public CombatPlayer CurrentCombatPlayer =>
            _initiativeQueue.GetCurrentCombatUnitAndClean() is CombatPlayer player ? player : null;

        /// <summary>
        ///     Current player or enemy who is active in current round
        /// </summary>
        public PlayerCharacterRecord CurrentPlayerCharacterRecord => CurrentCombatPlayer?.Record;

        public bool InEscapeMode { get; set; } = false;
        public int NumberOfCombatItemInQueue => _currentCombatItemQueue.Count;

        public int NumberOfEnemies => CombatMapUnits.CurrentMapUnits.Enemies.Count(enemy => enemy.IsActive);

        public int NumberOfVisiblePlayers =>
            CombatMapUnits.CurrentMapUnits.CombatPlayers.Count(combatPlayer => combatPlayer.IsActive);

        public int NumberOfAlivePlayers =>
            CombatMapUnits.CurrentMapUnits.CombatPlayers.Count(combatPlayer =>
                combatPlayer.IsActive && combatPlayer.Stats.Status != PlayerCharacterRecord.CharacterStatus.Dead);

        public int Round => _initiativeQueue.Round;

        public PlayerCharacterRecord SelectedCombatPlayerRecord => _initiativeQueue.ActivePlayerCharacterRecord;

        public SingleCombatMapReference TheCombatMapReference { get; }

        public int Turn => _initiativeQueue.Turn;

        protected sealed override Dictionary<Point2D, TileOverrideReference> XYOverrides { get; }

        /// <summary>
        ///     For tracking which escape route was used
        /// </summary>
        public enum EscapeType { None, EscapeKey, KlimbDown, KlimbUp, North, South, East, West }

        private readonly EscapeType _escapeType = EscapeType.None;

        /// <summary>
        ///     Creates CombatMap.
        ///     Note: Does not initialize the combat map units.
        /// </summary>
        /// <param name="singleCombatMapReference"></param>
        /// <param name="mapUnits"></param>
        /// <param name="mapOverrides"></param>
        public CombatMap(SingleCombatMapReference singleCombatMapReference, MapUnits.MapUnits mapUnits)
        {
            _mapUnits = mapUnits;
            //_mapOverrides = mapOverrides;
            CombatMapUnits = GameStateReference.State.TheVirtualMap.TheMapUnits;
            TheCombatMapReference = singleCombatMapReference;
            XYOverrides = GameReferences.Instance.TileOverrideRefs.GetTileXYOverrides(singleCombatMapReference);
        }

        internal override void ProcessTileEffectsForMapUnit(TurnResults turnResults, MapUnit mapUnit)
        {
            // we need to check for elemental fields for the current map unit and apply traps
            List<CombatMapUnit> combatMapUnits =
                AllVisibleCombatMapUnits.Where(m => m.MapUnitPosition.XY == mapUnit.MapUnitPosition.XY).ToList();
            foreach (CombatMapUnit combatMapUnit in combatMapUnits)
            {
                switch (combatMapUnit)
                {
                    case ElementalField elementalField:
                        //elementalField.TriggerTrap(turnResults, );
                        break;
                    // no default action
                }
            }
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
                for (nEnemyIndex = 0; nEnemyIndex < SingleCombatMapReference.NUM_MAP_UNITS; nEnemyIndex++)
                {
                    _ = CreateEnemiesAndNonAttackingUnits(nEnemyIndex, singleCombatMapReference, primaryEnemyReference);
                }

                return;
            }

            if (npcRef != null) Debug.Assert(nPrimaryEnemies == 1 && nSecondaryEnemies == 0);

            // special logic that ensures when fighting guards in the overworld
            // we actually fight more than one at a time!
            bool bIsGuard = primaryEnemyReference?.KeyTileReference.Index is >= 368 and <= 371;
            if (bIsGuard && singleCombatMapReference.MapTerritory != SingleCombatMapReference.Territory.Dungeon)
            {
                nPrimaryEnemies = Utils.GetNumberFromAndTo(4, 5);
            }

            // if there is only a single enemy then we always give them first position (such as NPC fights)
            if (nPrimaryEnemies == 1 && nSecondaryEnemies == 0)
            {
                _ = CreateEnemiesAndNonAttackingUnits(0, singleCombatMapReference, primaryEnemyReference);
                return;
            }

            // for regular combat maps, we introduce some randomness 
            Queue<int> monsterIndex = Utils.CreateRandomizedIntegerQueue(SingleCombatMapReference.NUM_MAP_UNITS);

            for (int nIndex = 0; nIndex < nPrimaryEnemies; nIndex++, nEnemyIndex++)
            {
                _ = CreateEnemiesAndNonAttackingUnits(monsterIndex.Dequeue(), singleCombatMapReference,
                    primaryEnemyReference);
            }

            for (int nIndex = 0; nIndex < nSecondaryEnemies; nIndex++, nEnemyIndex++)
            {
                _ = CreateEnemiesAndNonAttackingUnits(monsterIndex.Dequeue(), singleCombatMapReference,
                    secondaryEnemyReference);
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
                // dead players don't show up when you are loading a fresh combat map
                if (record.Stats.Status == PlayerCharacterRecord.CharacterStatus.Dead) continue;

                CombatPlayer combatPlayer = new(record, playerStartPositions[nPlayer]);

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
            hitState is CombatMapUnit.HitState.BarelyWounded or CombatMapUnit.HitState.LightlyWounded
                or CombatMapUnit.HitState.HeavilyWounded or CombatMapUnit.HitState.CriticallyWounded
                or CombatMapUnit.HitState.Fleeing;

        private static bool IsWalkingPassable(TileReference tileReference) =>
            tileReference.IsWalking_Passable ||
            tileReference.Index ==
            GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("RegularDoor").Index ||
            tileReference.Index == GameReferences.Instance.SpriteTileReferences
                .GetTileReferenceByName("RegularDoorView")
                .Index;

        private void ClearCurrentCombatItemQueue()
        {
            if (_currentCombatItemQueue == null)
                _currentCombatItemQueue = new Queue<CombatItem>();
            else
                _currentCombatItemQueue.Clear();
        }

        /// <summary>
        ///     Creates a single enemy in the context of the combat map.
        /// </summary>
        /// <param name="nEnemyIndex">0 based index that reflects the combat maps enemy index list</param>
        /// <param name="singleCombatMapReference">reference of the combat map</param>
        /// <param name="enemyReference">reference to enemy to be added (ignored for auto selected enemies)</param>
        /// <returns>the enemy that was just created</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private CombatMapUnit CreateEnemiesAndNonAttackingUnits(int nEnemyIndex,
            SingleCombatMapReference singleCombatMapReference,
            EnemyReference enemyReference)
        {
            SingleCombatMapReference.CombatMapSpriteType combatMapSpriteType =
                singleCombatMapReference.GetAdjustedEnemySprite(nEnemyIndex, out int nEnemySprite);
            Point2D mapUnitPosition = singleCombatMapReference.GetEnemyPosition(nEnemyIndex);

            // we want to make sure and not spawn an enemy on top of a player character - but like 
            // dungeon 58 - sometimes enemies spawn on NonAttackingUnits
            if (GetCombatUnit(mapUnitPosition) is CombatPlayer) return null;

            CombatMapUnit combatMapUnit = null;
            switch (combatMapSpriteType)
            {
                case SingleCombatMapReference.CombatMapSpriteType.Nothing:
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.Whirlpool:
                case SingleCombatMapReference.CombatMapSpriteType.Field:
                case SingleCombatMapReference.CombatMapSpriteType.Thing:
                    Debug.WriteLine("It's a chest or maybe a dead body!");
                    combatMapUnit = CombatMapUnits.CreateNonAttackUnitOnCombatMap(mapUnitPosition,
                        nEnemySprite, out int nIndex);
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.AutoSelected:
                    combatMapUnit = CombatMapUnits.CreateEnemyOnCombatMap(
                        singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
                        GameReferences.Instance.EnemyRefs.GetEnemyReference(nEnemySprite), out int _);
                    break;
                case SingleCombatMapReference.CombatMapSpriteType.EncounterBased:
                    Debug.Assert(!(mapUnitPosition.X == 0 && mapUnitPosition.Y == 0));
                    combatMapUnit = CombatMapUnits.CreateEnemyOnCombatMap(
                        singleCombatMapReference.GetEnemyPosition(nEnemyIndex),
                        enemyReference, out int _);
                    break;
                default:
                    throw new InvalidEnumArgumentException(((int)combatMapSpriteType).ToString());
            }

            return combatMapUnit;
        }

        private Enemy CreateEnemiesAndNonAttackingUnits(Point2D position, EnemyReference enemyReference)
        {
            Enemy enemy = CombatMapUnits.CreateEnemyOnCombatMap(position, enemyReference, out int _);
            return enemy;
        }

        private T GetClosestCombatMapUnitInRange<T>(CombatMapUnit attackingUnit, int nRange) where T : CombatMapUnit
        {
            double dBestDistanceToAttack = 150f;
            T bestOpponent = null;

            foreach (CombatMapUnit combatMapUnit in CombatMapUnits.CombatMapMapUnitCollection.AllCombatMapUnits)
            {
                if (combatMapUnit is not T enemy) continue;
                if (!IsCombatMapUnitInRange(attackingUnit, enemy, nRange)) continue;
                // if the enemy unit is invisible or charmed then they should not be targeted
                if (enemy.IsInvisible || enemy.IsCharmed) continue;
                if (enemy.Stats.Status == PlayerCharacterRecord.CharacterStatus.Dead) continue;

                double dDistance = enemy.MapUnitPosition.XY.DistanceBetween(attackingUnit.MapUnitPosition.XY);
                if (dDistance < dBestDistanceToAttack)
                {
                    dBestDistanceToAttack = dDistance;
                    bestOpponent = enemy;
                }
            }

            return bestOpponent;
        }

        private CombatPlayer GetClosestCombatPlayerInRange(Enemy enemy) =>
            GetClosestCombatMapUnitInRange<CombatPlayer>(enemy, enemy.EnemyReference.AttackRange);

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
            CombatMapUnit activeCombatMapUnit = _initiativeQueue.GetCurrentCombatUnitAndClean();

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
            AStar aStar = GetAStarMap(GetWalkableTypeByEnemy(enemy), _mapUnits);
            foreach (Point2D point in surroundingPoints)
            {
                // the check for IsTileWalkable may be redundant, but just in case
                // we create a list of potential free spaces around the enemy
                bool bIsAStarWalkable = aStar.GetWalkable(point); 
                if (IsTileWalkable(point, GetWalkableTypeByEnemy(enemy))
                    && bIsAStarWalkable)
                {
                    emptySpacePoints.Add(point);
                }
            }

            if (emptySpacePoints.Count == 0) return null;
            Point2D randomPoint = emptySpacePoints[Utils.GetNumberFromAndTo(0, emptySpacePoints.Count - 1)];
            return randomPoint;
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

            // remove all the bad points
            //notThesePoints.ForEach(m => surroundingCombatPlayerPoints.Remove(m));
            // if the play character is some how in range then remove them (shouldn't happen for range)
            surroundingCombatPlayerPoints.Remove(notThisPoint);

            Random random = new();
            int nIndex = random.Next() % surroundingCombatPlayerPoints.Count;
            Point2D randomSurroundingPoint = surroundingCombatPlayerPoints[nIndex];
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

        /// <summary>
        /// </summary>
        /// <param name="turnResults"></param>
        /// <param name="attackingCombatMapUnit"></param>
        /// <param name="attackPosition"></param>
        /// <param name="targetedCombatMapUnit"></param>
        /// <param name="nAttackMax"></param>
        /// <param name="missileType"></param>
        /// <exception cref="Ultima5ReduxException"></exception>
        private void HandleRangedMissed(TurnResults turnResults, CombatMapUnit attackingCombatMapUnit,
            Point2D attackPosition, out CombatMapUnit targetedCombatMapUnit, int nAttackMax,
            CombatItemReference.MissileType missileType)
        {
            // they are ranged, and missed which means we need to pick a new tile to attack
            // get a list of all surround tiles surrounding the player that they are attacking

            Point2D newAttackPosition =
                GetRandomSurroundingPointThatIsnt(attackPosition, //new(),
                    attackingCombatMapUnit.MapUnitPosition.XY);
            //{ attackingCombatMapUnit.MapUnitPosition.XY, attackPosition });

            Debug.Assert(newAttackPosition != null);
            if (newAttackPosition == attackingCombatMapUnit.MapUnitPosition.XY)
            {
                throw new Ultima5ReduxException(
                    $"You asked for a surrounding point but it gave your the origin: {newAttackPosition.X}, {newAttackPosition.Y}");
            }

            targetedCombatMapUnit = GetCombatUnit(newAttackPosition);

            bool bIsAttackerEnemy = attackingCombatMapUnit is Enemy;

            // We will check the raycast and determine if the missed shot in fact actually gets blocked - 
            // doing it in here will ensure that no damage is computed against an enemy if they are targeted
            // OR if the attack position is outside of the current map area, it means that a player has left the map
            if (IsRangedPathBlocked(attackPosition, newAttackPosition, out Point2D missedPoint) ||
                newAttackPosition.IsOutOfRange(NumOfXTiles - 1, NumOfYTiles - 1))
            {
                AdvanceToNextCombatMapUnit();

                turnResults.PushTurnResult(new AttackerTurnResult(
                    bIsAttackerEnemy
                        ? TurnResult.TurnResultType.Combat_Result_EnemyMissedRangedAttack
                        : TurnResult.TurnResultType.Combat_Result_CombatPlayerMissedRangedAttack,
                    //Combat_MissedRangedAttack,
                    attackingCombatMapUnit, null, missileType, CombatMapUnit.HitState.Missed, missedPoint));
                return;
            }

            if (targetedCombatMapUnit == null)
            {
                AdvanceToNextCombatMapUnit();
                turnResults.PushTurnResult(new AttackerTurnResult(bIsAttackerEnemy
                        ? TurnResult.TurnResultType.Combat_Result_EnemyMissedRangedAttack
                        : TurnResult.TurnResultType.Combat_Result_CombatPlayerMissedRangedAttack,
                    attackingCombatMapUnit, null, missileType, CombatMapUnit.HitState.Missed, newAttackPosition));
                return;
            }

            turnResults.PushOutputToConsole("\nBut they accidentally hit another!", false, false);
            // we attack the thing we accidentally hit
            CombatMapUnit.HitState hitState = attackingCombatMapUnit.Attack(turnResults, targetedCombatMapUnit,
                nAttackMax, missileType,
                out NonAttackingUnit nonAttackingUnitDrop,
                attackingCombatMapUnit is Enemy, true);

            // if they drop something then we add it to the map
            if (nonAttackingUnitDrop != null) CombatMapUnits.AddCombatMapUnit(nonAttackingUnitDrop);

            AdvanceToNextCombatMapUnit();
            switch (attackingCombatMapUnit)
            {
                case Enemy:
                    turnResults.PushTurnResult(new AttackerTurnResult(
                        TurnResult.TurnResultType.Combat_Result_EnemyMissedButHit,
                        attackingCombatMapUnit, targetedCombatMapUnit, missileType, hitState));
                    return;
                case CombatPlayer:
                    turnResults.PushTurnResult(new AttackerTurnResult(
                        TurnResult.TurnResultType.Combat_Result_CombatPlayerMissedButHit,
                        attackingCombatMapUnit, targetedCombatMapUnit, missileType, hitState));
                    return;
                default:
                    throw new Ultima5ReduxException("Something missed but hit but wasn't an enemy or a CombatPlayer: " +
                                                    attackingCombatMapUnit.GetType().Name);
            }
        }

        private bool IsCombatMapUnitInRange(CombatMapUnit attackingUnit, CombatMapUnit opponentCombatMapUnit,
            int nRange)
        {
            if (!opponentCombatMapUnit.IsActive) return false;
            if (!opponentCombatMapUnit.IsAttackable) return false;
            if (nRange == 1 && !attackingUnit.CanReachForMeleeAttack(opponentCombatMapUnit, nRange)) return false;
            //if they are not within range.. then they can't attack!
            if (nRange > 1 &&
                attackingUnit.MapUnitPosition.XY.DistanceBetween(opponentCombatMapUnit.MapUnitPosition.XY) > nRange)
                return false;
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
            TileReference tileReference = GameStateReference.State.TheVirtualMap.GetTileReference(xy);
            //GetOriginalTileReference(xy);
            return !tileReference.RangeWeapon_Passable;
        }

        /// <summary>
        ///     Moves the combat map unit to the CombatPlayer for whom they can reach in the fewest number of steps
        /// </summary>
        /// <param name="turnResults"></param>
        /// <param name="activeCombatUnit">the combat map unit that wants to attack a combat player</param>
        /// <param name="preferredAttackTarget">the type of target you will target</param>
        /// <param name="bMoved">did the CombatMapUnit move</param>
        /// <returns>The combat player that they are heading towards</returns>
        private CombatMapUnit MoveToClosestAttackableCombatMapUnit(TurnResults turnResults,
            CombatMapUnit activeCombatUnit, SpecificCombatMapUnit preferredAttackTarget, out bool bMoved
        )
        {
            if (activeCombatUnit == null)
                throw new Ultima5ReduxException("Passed a null active combat unit when moving to closest unit");

            const int noPath = 0xFFFF;
            bMoved = false;

            int nMinMoves = 0xFFFF;
            Stack<Node> preferredRoute = null;
            CombatMapUnit preferredAttackVictim = null;

            AStar aStar =
                GetAStarMap(GetWalkableTypeByMapUnit(activeCombatUnit), _mapUnits); 
    
            List<Point2D> potentialTargetsPoints = new();

            foreach (CombatMapUnit combatMapUnit in GetActiveCombatMapUnitsByType(preferredAttackTarget))
            {
                Point2D combatMapUnitXY = combatMapUnit.MapUnitPosition.XY;

                potentialTargetsPoints.Add(combatMapUnitXY);

                // get the shortest path to the unit - we ignore the range value because by calling this method we are insisting
                // that they move
                Stack<Node> theWay = aStar.FindPath(activeCombatUnit.MapUnitPosition.XY, combatMapUnitXY);
                int nMoves = theWay?.Count ?? noPath;
                if (nMoves < nMinMoves)
                {
                    nMinMoves = nMoves;
                    preferredRoute = theWay;
                    preferredAttackVictim = combatMapUnit;
                }
            }

            Point2D activeCombatUnitXY = activeCombatUnit.MapUnitPosition.XY;

            if (nMinMoves == noPath)
            {
                // if there is no path, then lets do some dirty checks to see if we can at least move closer

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

                Point2D nextBestMovePoint =
                    activeCombatUnit.GetBestNextPositionToMoveTowardsWalkablePointAStar(this, bestOpponentPoint, aStar);

                if (nextBestMovePoint == null)
                {
                    // only a 50% chance they will wander
                    if (Utils.Ran.Next() % 2 == 0) return null;

                    nextBestMovePoint = activeCombatUnit.GetValidRandomWanderPointAStar(this, aStar);
                    if (nextBestMovePoint == null) return null;
                }

                // we think we found the next best path
                preferredRoute = new Stack<Node>();
                preferredRoute.Push(aStar.GetNode(nextBestMovePoint));
            }

            if (preferredRoute == null)
                throw new Ultima5ReduxException("Preferred route object was oddly empty");

            Point2D nextPosition = preferredRoute.Pop().Position;

            MoveActiveCombatMapUnit(turnResults, nextPosition);
            bMoved = true;

            return preferredAttackVictim;
        }

        private CombatPlayer MoveToClosestAttackableCombatPlayer(TurnResults turnResults,
            CombatMapUnit activeCombatUnit, out bool bMoved) =>
            MoveToClosestAttackableCombatMapUnit(turnResults, activeCombatUnit,
                    SpecificCombatMapUnit.CombatPlayer, out bMoved) as
                CombatPlayer;

        private Enemy MoveToClosestAttackableEnemy(TurnResults turnResults, CombatMapUnit activeMapUnit,
            out string outputStr, out bool bMoved, MapUnits.MapUnits mapUnits)
        {
            var enemy =
                MoveToClosestAttackableCombatMapUnit(turnResults, activeMapUnit, SpecificCombatMapUnit.Enemy,
                    out bMoved) as Enemy;
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

        /// <summary>
        ///     Takes care of extra stuff that happens as a result of the hit such as division - or the death of an NPC
        /// </summary>
        /// <param name="turnResults"></param>
        /// <param name="hitState"></param>
        /// <param name="affectedCombatMapUnit"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void PerformAdditionalHitProcessing(TurnResults turnResults, CombatMapUnit.HitState hitState,
            CombatMapUnit affectedCombatMapUnit)
        {
            // some things only occur if they are hit - but not if they are killed or missed
            if (IsHitButNotKilled(hitState) && affectedCombatMapUnit is Enemy enemy &&
                enemy.EnemyReference.IsEnemyAbility(EnemyReference.EnemyAbility.DivideOnHit) && Utils.OneInXOdds(2))
            {
                // do they multiply?
                DivideEnemy(enemy);

                turnResults.PushOutputToConsole("\n" + affectedCombatMapUnit.FriendlyName + GameReferences.Instance
                    .DataOvlRef
                    .StringReferences.GetString(DataOvlReference.Battle2Strings._DIVIDES_BANG_N)
                    .TrimEnd(), false, false);
            }

            switch (hitState)
            {
                case CombatMapUnit.HitState.Grazed:
                case CombatMapUnit.HitState.Missed:
                case CombatMapUnit.HitState.BarelyWounded:
                case CombatMapUnit.HitState.LightlyWounded:
                case CombatMapUnit.HitState.HeavilyWounded:
                case CombatMapUnit.HitState.CriticallyWounded:
                case CombatMapUnit.HitState.Fleeing:
                case CombatMapUnit.HitState.Dead:
                    if (affectedCombatMapUnit is Enemy deadEnemy)
                    {
                        // if the enemy was an NPC then we kill them!
                        if (deadEnemy.NPCRef != null)
                        {
                            deadEnemy.NPCState.IsDead = true;
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hitState), hitState, null);
            }
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
                RecalculatedHash = Utils.Ran.Next();
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
            RecalculatedHash = Utils.Ran.Next();
        }

        public CombatMapUnit AdvanceToNextCombatMapUnit()
        {
            CombatMapUnit combatMapUnit = _initiativeQueue.AdvanceToNextCombatMapUnit();

            _bPlayerHasChanged = true;

            RefreshCurrentCombatPlayer();
            return combatMapUnit;
        }

        public void BuildCombatItemQueue(List<CombatItem> combatItems)
        {
            _currentCombatItemQueue = new Queue<CombatItem>(combatItems);
        }

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

            Enemy newEnemy = CreateEnemiesAndNonAttackingUnits(newEnemyPosition, enemy.EnemyReference);

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
                case SpecificCombatMapUnit.NonAttackUnit:
                    return AllNonAttackUnits.Where(nau => nau.IsActive);
                default:
                    throw new ArgumentOutOfRangeException(nameof(specificCombatMapUnit), specificCombatMapUnit, null);
            }
        }

        public CombatMapUnit GetAndRefreshCurrentCombatMapUnit()
        {
            CombatMapUnit combatUnit = _initiativeQueue.GetCurrentCombatUnitAndClean();
            // we need to refresh current combat player in case the preceding method rips out some old
            // or hidden enemies - this way we know that the CombatItem queue is up to date
            RefreshCurrentCombatPlayer();
            return combatUnit;
        }

        public Enemy GetClosestEnemyInRange(CombatPlayer attackingCombatPlayer, CombatItem combatItem) =>
            GetClosestCombatMapUnitInRange<Enemy>(attackingCombatPlayer,
                combatItem.TheCombatItemReference.Range);

        public CombatPlayer GetCombatPlayer(PlayerCharacterRecord record)
        {
            return CombatMapUnits.CurrentMapUnits.CombatPlayers.FirstOrDefault(player => player.Record == record);
        }

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
                Stack<Node> currentPath = GetAStarMap(walkableType, _mapUnits)
                    .FindPath(fromPosition, destinationPoint);
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


        private readonly List<Type> _visibilePriorityOrder = new()
        {
            typeof(DiscoverableLoot), typeof(Horse), typeof(MagicCarpet), typeof(Skiff), typeof(Frigate),
            typeof(NonPlayerCharacter),
            typeof(Enemy), typeof(CombatPlayer), typeof(Avatar), typeof(ItemStack), typeof(StackableItem),
            typeof(Chest), typeof(DeadBody), typeof(BloodSpatter), typeof(ElementalField), typeof(Whirlpool)
        };

        /// <summary>
        ///     Gets the top visible map unit - excluding the Avatar
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="bExcludeAvatar"></param>
        /// <returns>MapUnit or null</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public MapUnit GetTopVisibleMapUnit(Point2D xy, bool bExcludeAvatar)
        {
            List<CombatMapUnit> combatMapUnits =
                AllVisibleCombatMapUnits.Where(m => m.MapUnitPosition.XY == xy).ToList();

            // this is inefficient, but the lists are so small it is unlikely to matter
            foreach (Type type in _visibilePriorityOrder)
            {
                if (bExcludeAvatar && type == typeof(Avatar)) continue;
                foreach (CombatMapUnit combatMapUnit in combatMapUnits)
                {
                    if (!combatMapUnit.IsActive) continue;
                    // if it's a combat unit but they dead or gone then we skip
                    if ((combatMapUnit.HasEscaped || combatMapUnit.Stats.CurrentHp <= 0) &&
                        combatMapUnit is not NonAttackingUnit) continue;

                    // if we find the first highest priority item, then we simply return it
                    if (combatMapUnit.GetType() == type) return combatMapUnit;
                }
            }

            return null;
        }

        public static EscapeType DirectionToEscapeType(Point2D.Direction direction)
        {
            switch (direction)
            {
                case Point2D.Direction.Up: return EscapeType.North;
                case Point2D.Direction.Down: return EscapeType.South;
                case Point2D.Direction.Left: return EscapeType.West;
                case Point2D.Direction.Right: return EscapeType.East;
                case Point2D.Direction.None: return EscapeType.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public bool TryToMakePlayerEscape(TurnResults turnResults, CombatPlayer combatPlayer, EscapeType escapeType)
        {
            turnResults.PushOutputToConsole("\nLEAVING", false);

            //EscapeType escapeType = DirectionToEscapeType(direction);

            if (_escapeType == EscapeType.None || _escapeType == escapeType)
            {
                // allowed to escape
                MakePlayerEscape(combatPlayer);
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.Combat_CombatPlayerEscaped));
                return true;
            }

            turnResults.PushOutputToConsole("All must use the same exit!");

            return false;
        }
        
        public void MakePlayerEscape(CombatPlayer combatPlayer)
        {
            Debug.Assert(!combatPlayer.HasEscaped);

            combatPlayer.HasEscaped = true;
            AdvanceToNextCombatMapUnit();
        }

        /// <summary>
        ///     Moves the active combat unit to a new map position
        /// </summary>
        /// <param name="turnResults"></param>
        /// <param name="xy"></param>
        public void MoveActiveCombatMapUnit(TurnResults turnResults, Point2D xy)
        {
            CombatMapUnit currentCombatUnit = _initiativeQueue.GetCurrentCombatUnitAndClean();
            if (currentCombatUnit == null)
                throw new Ultima5ReduxException(
                    "Tried to move active combat unit, but couldn't find them in initiative queue");

            // we MUST be certain at this point that we are actually allowed to move to this tile
            Point2D originalPosition = currentCombatUnit.MapUnitPosition.XY;

            bool bIsMapUnitOccupyingNextTile =
                IsMapUnitOccupiedFromList(xy, 0, AllVisibleCombatMapUnits.Where(m => !m.CanStackMapUnitsOnTop));

            if (bIsMapUnitOccupyingNextTile)
            {
                MapUnit mapUnit = GetTopVisibleMapUnit(xy, false);
                throw new Ultima5ReduxException(
                    $"Tried to move {currentCombatUnit.FriendlyName} to {xy.GetFriendlyString()} but it was occupied by {mapUnit?.FriendlyName ?? "NO MAP UNIT"}");
            }

            currentCombatUnit.MapUnitPosition = new MapUnitPosition(xy.X, xy.Y, 0);

            switch (currentCombatUnit)
            {
                case Enemy enemy:
                    turnResults.PushTurnResult(new EnemyMoved(TurnResult.TurnResultType.Combat_EnemyMoved, enemy,
                        originalPosition,
                        enemy.MapUnitPosition.XY));
                    break;
                case CombatPlayer combatPlayer:
                    turnResults.PushTurnResult(new CombatPlayerMoved(TurnResult.TurnResultType.Combat_CombatPlayerMoved,
                        combatPlayer, originalPosition, combatPlayer.MapUnitPosition.XY,
                        GameStateReference.State.TheVirtualMap.GetTileReference(combatPlayer.MapUnitPosition.XY)));
                    break;
                default:
                    throw new Ultima5ReduxException(
                        "Tried to move active combat unit - but was neither Enemy or CombatPlayer: " +
                        currentCombatUnit.GetType().Name);
            }

            ProcessTileEffectsForMapUnit(turnResults, currentCombatUnit);

            // you can trigger tiles by simply walking on them
            if (TheCombatMapReference.HasTriggers && TheCombatMapReference.TheTriggerTiles.HasTriggerAtPosition(xy))
            {
                bool bWasTrigger = HandleTrigger(turnResults, xy);
                if (bWasTrigger)
                {
                    _initiativeQueue.RefreshFutureRounds();
                }
            }
        }

        public Enemy MoveToClosestAttackableEnemy(TurnResults turnResults, out string outputStr, out bool bMoved,
            MapUnits.MapUnits mapUnits) =>
            MoveToClosestAttackableEnemy(turnResults, CurrentCombatPlayer, out outputStr, out bMoved, mapUnits);

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

        public void ProcessCombatPlayerTurn(TurnResults turnResults, SelectionAction selectedAction,
            Point2D actionPosition, out CombatMapUnit activeCombatMapUnit, out CombatMapUnit targetedCombatMapUnit,
            MapUnits.MapUnits mapUnits)
        {
            targetedCombatMapUnit = null;

            CombatPlayer combatPlayer = GetCurrentCombatPlayer();
            if (combatPlayer == null)
                throw new Ultima5ReduxException("Tried to ProcessCombatPlayerTurn but CombatPlayer was null");

            activeCombatMapUnit = null;

            switch (selectedAction)
            {
                case SelectionAction.None:
                case SelectionAction.Magic:
                    return;
                case SelectionAction.Attack:
                    string attackStr = "Attack ";
                    MapUnit opponentMapUnit = GetCombatUnit(actionPosition);

                    // you have tried to attack yourself!?
                    if (opponentMapUnit == combatPlayer)
                    {
                        HandleCombatPlayerTryingToAttackThemselves(turnResults, attackStr, combatPlayer);
                        return;
                    }

                    // the top most unit is NOT a combat unit, so they hit nothing - OR - they are attacking a trigger
                    if (opponentMapUnit is not CombatMapUnit opponentCombatMapUnit)
                    {
                        HandleCombatPlayerAttackingNoOpponent(turnResults, actionPosition, attackStr, combatPlayer);
                        return;
                    }

                    // if the top most unit is a combat map unit, then let's fight!
                    attackStr += opponentCombatMapUnit.Name;

                    // TODO: might be a bug - what about bare hands?
                    CombatItem weapon = _currentCombatItemQueue.Dequeue();

                    attackStr += " with " + weapon.LongName + "!";
                    turnResults.PushOutputToConsole(attackStr);

                    // let's first make sure that any range weapons do not hit a wall first!
                    if (weapon.TheCombatItemReference.Range > 1)
                    {
                        if (opponentMapUnit == null)
                            throw new Ultima5ReduxException("Opponent map unit is null");
                        if (combatPlayer == null)
                            throw new Ultima5ReduxException("Combat player is null");

                        bool bIsBlocked = IsRangedPathBlocked(combatPlayer.MapUnitPosition.XY,
                            opponentMapUnit.MapUnitPosition.XY, out Point2D firstBlockPoint);
                        // the character tried to attack an opponent, but was blocked
                        if (bIsBlocked)
                        {
                            HandleCombatPlayerBlockedAttackOnOpponent(turnResults, combatPlayer, opponentMapUnit,
                                weapon, firstBlockPoint);
                            return;
                        }
                    }

                    // do the attack logic
                    if (combatPlayer == null)
                        throw new Ultima5ReduxException("Combat player unexpectedly null");

                    CombatMapUnit.HitState targetedHitState = combatPlayer.Attack(turnResults, opponentCombatMapUnit,
                        weapon, out NonAttackingUnit nonAttackingUnitDrop, false);
                    if (nonAttackingUnitDrop != null)
                    {
                        // it's already created - we just add it to the mapunits list we track
                        CombatMapUnits.AddCombatMapUnit(nonAttackingUnitDrop);
                    }

                    // if the player attacks, but misses with a range weapon the we need see if they
                    // accidentally hit someone else
                    switch (targetedHitState)
                    {
                        // we missed - BUT - we have a range weapon so we may have hit someone/something else
                        case CombatMapUnit.HitState.Missed when weapon.TheCombatItemReference.Range > 1:
                        {
                            HandleRangedMissed(turnResults, combatPlayer,
                                opponentCombatMapUnit.MapUnitPosition.XY, out targetedCombatMapUnit,
                                weapon.TheCombatItemReference.AttackStat, weapon.TheCombatItemReference.Missile);
                            break;
                        }
                        case CombatMapUnit.HitState.Missed:
                            // we missed but are not using a ranged weapon
                            // this is now added inside the CombatMapUnit.Attack function
                            break;
                        default:
                            // we know they attacked this particular opponent at this point, we definitely didn't miss
                            // The Attack() call should have already taken care of putting the right stuff in
                            targetedCombatMapUnit = opponentCombatMapUnit;
                            break;
                    }

                    // must check to see if any special 
                    PerformAdditionalHitProcessing(turnResults, targetedHitState, targetedCombatMapUnit);

                    AdvanceIfSafe(combatPlayer, turnResults);

                    return;
                default:
                    throw new InvalidEnumArgumentException(((int)selectedAction).ToString());
            }
        }

        private void HandleCombatPlayerBlockedAttackOnOpponent(TurnResults turnResults, CombatPlayer combatPlayer,
            MapUnitDetails opponentMapUnit, CombatItem weapon, Point2D firstBlockPoint)
        {
            string blockedAttackStr = combatPlayer.FriendlyName +
                                      GameReferences.Instance.DataOvlRef.StringReferences
                                          .GetString(DataOvlReference.BattleStrings._MISSED_BANG_N)
                                          .TrimEnd().Replace("!", " ") + opponentMapUnit.FriendlyName +
                                      " because it was blocked!";
            turnResults.PushOutputToConsole(blockedAttackStr);
            turnResults.PushTurnResult(new AttackerTurnResult(
                TurnResult.TurnResultType.Combat_CombatPlayerRangedAttackBlocked,
                combatPlayer, null, weapon.TheCombatItemReference.Missile,
                CombatMapUnit.HitState.Missed, firstBlockPoint));

            AdvanceIfSafe(combatPlayer, turnResults);
        }

        private void HandleCombatPlayerTryingToAttackThemselves(TurnResults turnResults, string attackStr,
            CombatPlayer combatPlayer)
        {
            attackStr = attackStr.TrimEnd() + "... yourself? ";
            attackStr += "\nYou think better of it and skip your turn.";
            turnResults.PushOutputToConsole(attackStr);
            turnResults.PushTurnResult(new SinglePlayerCharacterAffected(
                TurnResult.TurnResultType.Combat_CombatPlayerTriedToAttackSelf, combatPlayer.Stats));
            AdvanceIfSafe(combatPlayer, turnResults);
        }

        private void AdvanceIfSafe(CombatPlayer theCombatPlayer, TurnResults turnResults)
        {
            if (_currentCombatItemQueue != null && _currentCombatItemQueue.Count != 0) return;

            theCombatPlayer?.Record.ProcessPlayerTurn(turnResults);
            AdvanceToNextCombatMapUnit();
        }

        private void TriggerTile(TurnResults turnResults, TriggerTileData triggerTileData, Point2D triggerPosition)
        {
            TileReference newTileReference = triggerTileData.TriggerSprite;

            if (newTileReference.Index == (int)TileReference.SpriteIndex.Portcullis)
            {
                // this is for sound effects likely
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.OpenPortcullis));
            }

            // this is to actually replace the tile
            //GameStateReference.State.TheVirtualMap.CurrentMap.
            SetOverridingTileReferece(newTileReference, triggerPosition);

            // Not sure I need this - but maybe I will react visually 
            turnResults.PushTurnResult(new TileOverrideOnCombatMap(triggerPosition,
                newTileReference
            ));
        }

        private bool HandleTrigger(TurnResults turnResults, Point2D position)
        {
            if (!TheCombatMapReference.TheTriggerTiles.HasTriggerAtPosition(position))
                return false;

            List<TriggerTileData> triggerTiles =
                TheCombatMapReference.TheTriggerTiles.GetTriggerTileDataByPosition(position);

            foreach (TriggerTileData triggerTileData in triggerTiles.Where(t => !t.Triggered))
            {
                // walk through each of the tiles that we will now be changing
                foreach (Point2D changeTilePosition in triggerTileData.TriggerChangePositions)
                {
                    TriggerTile(turnResults, triggerTileData, changeTilePosition);
                }
            }

            return true;
        }

        private void HandleCombatPlayerAttackingNoOpponent(TurnResults turnResults, Point2D actionPosition,
            string attackStr, CombatPlayer combatPlayer)
        {
            CombatItem weapon = null;
            attackStr += "nothing with ";
            if (_currentCombatItemQueue == null)
            {
                // bare hands
                attackStr += " with bare hands!";
            }
            else
            {
                weapon = _currentCombatItemQueue.Dequeue();
                attackStr += weapon.LongName + "!";
            }

            bool bIsRanged = (weapon?.TheCombatItemReference.Range ?? 1) > 1;
            bool bIsBlocked = IsRangedPathBlocked(combatPlayer.MapUnitPosition.XY,
                actionPosition, out Point2D firstBlockPoint);
            CombatItemReference.MissileType missileType = weapon?.TheCombatItemReference.Missile ??
                                                          CombatItemReference.MissileType.None;

            // if there is a trigger on this tile - then let's trigger!
            if (TheCombatMapReference.HasTriggers)
            {
                if (HandleTrigger(turnResults, actionPosition))
                {
                    // required to actually show an attack animation
                    turnResults.PushTurnResult(new AttackerTurnResult(
                        TurnResult.TurnResultType.Combat_CombatPlayerAttackedTrigger,
                        combatPlayer, null, missileType,
                        CombatMapUnit.HitState.HitTrigger, actionPosition));

                    AdvanceToNextCombatMapUnit();
                    return;
                }
            }

            // it's not good enough just to not show any attack - the player needs to know that
            // their input was accepted - it just happens that it doesn't do anything of value
            // if ranged
            // -- If it is not ranged, then it's impossible to be "blocked" right??
            if (bIsRanged && bIsBlocked)
            {
                turnResults.PushTurnResult(new AttackerTurnResult(
                    TurnResult.TurnResultType.Combat_CombatPlayerRangedAttackBlocked,
                    combatPlayer, null, missileType,
                    CombatMapUnit.HitState.Blocked, firstBlockPoint));
                AdvanceIfSafe(combatPlayer, turnResults);
                return;
            }

            AdvanceIfSafe(combatPlayer, turnResults);

            turnResults.PushOutputToConsole(attackStr);
            turnResults.PushTurnResult(new AttackerTurnResult(
                TurnResult.TurnResultType.Combat_CombatPlayerTriedToAttackNothing,
                combatPlayer, null, missileType, CombatMapUnit.HitState.HitNothingOfNote, actionPosition));
        }

        private bool CanEnemyMoveToSpace(Point2D xy, Enemy enemy)
        {
            bool bIsTileWalkable = false;
            // let's check to see if a map unit is blocking - but not all block, such as blood spatter
            if (enemy.FleeingPath != null)
                bIsTileWalkable |= !IsMapUnitBlockingSpace(xy); //enemy.FleeingPath.Peek().Position);

            // Debug.Assert(tileReference != null);

            WalkableType walkableType = GetWalkableTypeByEnemy(enemy);

            // let's also check the current tile reference and see if we are allowed to walk
            // on it - regardless if there is a map unit or not
            bIsTileWalkable &= IsTileWalkable(enemy.MapUnitPosition.XY, walkableType);

            return bIsTileWalkable;
        }

        /// <summary>
        ///     Attempts to processes the turn of the current combat unit - either CombatPlayer or Enemy.
        ///     Can result in advancing to next turn, or indicate user input required
        /// </summary>
        /// <param name="turnResults"></param>
        /// <param name="activeCombatMapUnit">the combat unit that is taking the action</param>
        /// <param name="targetedCombatMapUnit">an optional unit that is being affected by the active combat unit</param>
        /// <param name="missedPoint">if the target is empty or missed then this gives the point that the attack landed</param>
        /// <returns></returns>
        public void ProcessEnemyTurn(TurnResults turnResults, out CombatMapUnit activeCombatMapUnit,
            out CombatMapUnit targetedCombatMapUnit,
            out Point2D missedPoint, MapUnits.MapUnits mapUnits)
        {
            activeCombatMapUnit = _initiativeQueue.GetCurrentCombatUnitAndClean();
            targetedCombatMapUnit = null;
            missedPoint = null;

            // Everything after is ENEMY logic!
            // either move the ENEMY or have them attack someone
            Debug.Assert(activeCombatMapUnit is Enemy);
            var enemy = activeCombatMapUnit as Enemy;

            // if the enemy is charmed then the player get's to control them instead!
            if (enemy == null)
                throw new Ultima5ReduxException("Enemy unexpectedly null");
            if (enemy.IsCharmed)
            {
                // the player gets to control the enemy!
                turnResults.PushOutputToConsole(enemy.FriendlyName + ":");
                turnResults.PushTurnResult(
                    new EnemyFocusedTurnResult(TurnResult.TurnResultType.Combat_EnemyToAttackRequiresInput, enemy));
                return;
            }

            if (enemy.IsSleeping)
            {
                turnResults.PushOutputToConsole(enemy.FriendlyName + ": Sleeping");
                turnResults.PushTurnResult(new EnemyFocusedTurnResult(TurnResult.TurnResultType.Combat_EnemyIsSleeping,
                    enemy));
                return;
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
                    turnResults.PushOutputToConsole(enemy.EnemyReference.MixedCaseSingularName + " escaped!");

                    turnResults.PushTurnResult(
                        new EnemyFocusedTurnResult(TurnResult.TurnResultType.Combat_EnemyEscaped, enemy));

                    AdvanceToNextCombatMapUnit();
                    return;
                }

                bool bIsTileWalkable = enemy.FleeingPath != null &&
                                       CanEnemyMoveToSpace(enemy.FleeingPath.Peek().Position, enemy);

                // does the monster not yet have a flee path OR
                // does the enemy have a flee path already established that is now block OR
                if (enemy.FleeingPath == null || !bIsTileWalkable)
                {
                    WalkableType walkableType = GetWalkableTypeByEnemy(enemy);

                    enemy.FleeingPath = GetEscapeRoute(enemy.MapUnitPosition.XY, walkableType);
                    // if the enemy is unable to calculate an exit path
                    if (enemy.FleeingPath == null)
                    {
                        // if I decide to do something, then I will do it here
                        turnResults.PushTurnResult(
                            new EnemyFocusedTurnResult(TurnResult.TurnResultType.Combat_EnemyWantsToFleeButNoPath,
                                enemy));
                        // they keep going - they are desperate so they may try to attack
                    }
                }

                // if there is a path then follow it, otherwise fall through and attack like normal
                if (enemy.FleeingPath?.Count > 0)
                {
                    Point2D nextStep = enemy.FleeingPath.Pop().Position;
                    MoveActiveCombatMapUnit(turnResults, nextStep);
                    turnResults.PushOutputToConsole(enemy.EnemyReference.MixedCaseSingularName + " fleeing!");
                    turnResults.PushTurnResult(
                        new EnemyFocusedTurnResult(TurnResult.TurnResultType.Combat_EnemyIsFleeing, enemy));
                    AdvanceToNextCombatMapUnit();
                    return;
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
                                         enemy.CanReachForMeleeAttack(enemy.PreviousAttackTarget);
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
                if (enemy.EnemyReference.IsEnemyAbility(EnemyReference.EnemyAbility.StealsFood)
                    && OddsAndLogic.DidEnemyStealFood())
                {
                    GameStateReference.State.PlayerInventory.TheProvisions.FoodStolen(turnResults, enemy.EnemyReference,
                        OddsAndLogic.DEFAULT_FOOD_STOLEN);
                }

                CombatMapUnit.HitState hitState = enemy.Attack(turnResults, bestCombatPlayer,
                    enemy.EnemyReference.TheDefaultEnemyStats.Damage, enemy.EnemyReference.TheMissileType,
                    out NonAttackingUnit _, true);

                switch (hitState)
                {
                    case CombatMapUnit.HitState.Missed:
                        // oh oh - the enemy missed
                        if (enemy.EnemyReference.TheMissileType == CombatItemReference.MissileType.None)
                        {
                            // it's a melee attack
                            targetedCombatMapUnit = bestCombatPlayer;
                            var attackerTurnResult = new AttackerTurnResult(
                                TurnResult.TurnResultType.Combat_Result_Missed_EnemyMelee,
                                activeCombatMapUnit, targetedCombatMapUnit, enemy.EnemyReference.TheMissileType,
                                hitState);
                            turnResults.PushTurnResult(attackerTurnResult);
                            break;
                        }

                        Debug.Assert(enemy.EnemyReference.AttackRange > 1,
                            "Cannot have a ranged weapon if no missile type set");

                        HandleRangedMissed(turnResults, enemy,
                            bestCombatPlayer.MapUnitPosition.XY,
                            out targetedCombatMapUnit, enemy.EnemyReference.TheDefaultEnemyStats.Damage,
                            enemy.EnemyReference.TheMissileType);
                        return;
                    case CombatMapUnit.HitState.Grazed:
                    case CombatMapUnit.HitState.BarelyWounded:
                    case CombatMapUnit.HitState.LightlyWounded:
                    case CombatMapUnit.HitState.HeavilyWounded:
                    case CombatMapUnit.HitState.CriticallyWounded:
                        targetedCombatMapUnit = bestCombatPlayer;
                        break;
                    case CombatMapUnit.HitState.Dead:
                        // if the current active player dies - then we set it to the whole party again
                        if (bestCombatPlayer is CombatPlayer combatPlayer &&
                            _initiativeQueue.CombatPlayerIsActive(combatPlayer))
                        {
                            _initiativeQueue.SetActivePlayerCharacter(null);
                        }

                        targetedCombatMapUnit = bestCombatPlayer;
                        break;
                    case CombatMapUnit.HitState.Fleeing:
                        targetedCombatMapUnit = bestCombatPlayer;
                        break;
                    case CombatMapUnit.HitState.None:
                    default:
                        throw new InvalidEnumArgumentException(((int)hitState).ToString());
                }

                AdvanceToNextCombatMapUnit();
                return;
            }

            bool bMoved = false;
            if (!enemy.EnemyReference.DoesNotMove)
            {
                MoveToClosestAttackableCombatPlayer(turnResults, enemy, out bMoved);
            }

            if (bMoved)
            {
                // we have exhausted all potential attacking possibilities, so instead we will just move 
                turnResults.PushOutputToConsole(enemy.EnemyReference.MixedCaseSingularName + " moved.");
            }
            else
            {
                turnResults.PushOutputToConsole(enemy.EnemyReference.MixedCaseSingularName +
                                                " is unable to move or attack.");
            }

            AdvanceToNextCombatMapUnit();
        }

        public void SetActivePlayerCharacter(PlayerCharacterRecord record)
        {
            _initiativeQueue.SetActivePlayerCharacter(record);
            _bPlayerHasChanged = true;
            RefreshCurrentCombatPlayer();
        }

        private bool IsMapUnitBlockingSpace(Point2D xy)
        {
            return AllVisibleCombatMapUnits.Where(m => m.MapUnitPosition.XY == xy).ToList()
                .Any(m => !m.CanStackMapUnitsOnTop);
        }

        protected override float GetAStarWeight(in Point2D xy) => 1.0f;

        public override WalkableType GetWalkableTypeByMapUnit(MapUnit mapUnit)
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

        public void TryToPushAThing(Point2D avatarXy, Point2D.Direction direction,
            out bool bPushedAThing, TurnResults turnResults)
        {
            bPushedAThing = false;
            Point2D adjustedPos = avatarXy.GetAdjustedPosition(direction);

            TileReference adjustedTileReference = GameStateReference.State.TheVirtualMap.GetTileReference(adjustedPos);

            if (adjustedTileReference.Index == (int)TileReference.SpriteIndex.Portcullis)
            {
                HandleTrigger(turnResults, adjustedPos);
            }

            AdvanceToNextCombatMapUnit();
        }
    }
}