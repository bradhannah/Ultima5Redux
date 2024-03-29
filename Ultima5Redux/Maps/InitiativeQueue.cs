﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.Maps
{
    public class InitiativeQueue
    {
        private const int MIN_TURNS_IN_QUEUE = 6;

        /// <summary>
        ///     A running tally of combat initiatives. This provides a running tally of initiative to provide
        ///     ongoing boosts to high dexterity map units
        /// </summary>
        private readonly Dictionary<CombatMapUnit, int> _combatInitiativeTally = new();

        private readonly CombatMap _combatMap;

        /// <summary>
        ///     Queue that provides order attack for all players and enemies
        /// </summary>
        private readonly Queue<Queue<CombatMapUnit>> _initiativeQueue = new();

        private readonly MapUnitCollection _mapUnitCollection;

        private readonly PlayerCharacterRecords _playerCharacterRecords;

        /// <summary>
        ///     Lowest player/enemy dexterity encountered
        /// </summary>
        private int _nLowestDexterity;

        public PlayerCharacterRecord ActivePlayerCharacterRecord { get; private set; }

        public int Round { get; private set; }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public int TotalTurnsInQueue => _initiativeQueue.Sum(combatMapUnitQueue =>
            combatMapUnitQueue.Count(CombatMapUnitIsPresentAndActive));

        public int Turn { get; private set; }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public int TurnsLeftsInRound => _initiativeQueue?.Peek()?.Count ?? 0;

        public InitiativeQueue(MapUnitCollection combatMapUnits, PlayerCharacterRecords playerCharacterRecords,
            CombatMap combatMap)
        {
            _mapUnitCollection = combatMapUnits;
            _playerCharacterRecords = playerCharacterRecords;
            _combatMap = combatMap;
            InitializeInitiativeQueue();
        }

        /// <summary>
        ///     Gets the next combat map unit that will be playing
        /// </summary>
        /// <returns></returns>
        internal CombatMapUnit AdvanceToNextCombatMapUnit()
        {
            Debug.Assert(_initiativeQueue.Count > 0);
            Turn++;

            // we are done with this unit now, so we just toss them out
            if (_initiativeQueue.Peek().Count > 0)
            {
                _initiativeQueue.Peek().Dequeue();
            }
            else
            {
                _initiativeQueue.Dequeue();
            }

            // if the queue is empty then we will recalculate it before continuing
            if (_initiativeQueue.Count == 0) CalculateNextInitiativeQueue();

            Debug.Assert(_initiativeQueue.Count > 0);
            return GetCurrentCombatUnitAndClean();
        }

        /// <summary>
        ///     Calculates an initiative queue giving the order of all attacks or moves within a single round
        /// </summary>
        /// <returns>
        ///     true if was able to calculate an initiative queue with minimum number of items, false
        ///     if it is not full (should be zero!)
        /// </returns>
        private bool CalculateNextInitiativeQueue()
        {
            while (true)
            {
                Debug.Assert(_playerCharacterRecords != null);
                int nStartingItems = GetNumberOfTurnsInQueue();

                Queue<CombatMapUnit> newInitiativeQueue = new();
                _initiativeQueue.Enqueue(newInitiativeQueue);

                // a mapping of dexterity values to an ordered list of combat map units 
                Dictionary<int, List<CombatMapUnit>> dexterityToCombatUnits = new();

                // go through each combat map unit and place them in priority order based on their dexterity values 
                foreach (CombatMapUnit combatMapUnit in _mapUnitCollection.AllCombatMapUnits.Where(
                             IsCombatMapUnit))
                {
                    Debug.Assert(IsCombatMapUnit(combatMapUnit));

                    int nDexterity = combatMapUnit.Dexterity;
                    // if the combat unit is not in the list, but is also not an Active Attacker then we skip them 
                    if (!_combatInitiativeTally.ContainsKey(combatMapUnit) && combatMapUnit is Enemy enemy &&
                        !enemy.EnemyReference.ActivelyAttacks)
                    {
                        continue;
                    }

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
                        if (_initiativeQueue.Count <= 0)
                            throw new Ultima5ReduxException(
                                "Tried to queue a CombatMapUnit but there is no active queue");

                        newInitiativeQueue.Enqueue(combatMapUnit);
                    }
                }

                // if there are not at least the minimum number of turns in the queue, then we recursively call this method
                // to continue population. We are assuming each turn results in at least a single additional turn
                if (!IsAtLeastNTurnsInQueue(MIN_TURNS_IN_QUEUE))
                {
                    if (nStartingItems == GetNumberOfTurnsInQueue())
                    {
                        Debug.Assert(nStartingItems == 0);
                        return false;
                    }

                    continue;
                }

                break;
            }

            return true;
        }

        internal bool CombatPlayerIsActive(CombatPlayer player) =>
            ActivePlayerCharacterRecord == null || player.Record == ActivePlayerCharacterRecord;

        /// <summary>
        ///     Gets the active combat unit - either CombatPlayer or Enemy.
        /// </summary>
        /// <returns></returns>
        internal CombatMapUnit GetCurrentCombatUnitAndClean()
        {
            while (_initiativeQueue.Count > 0)
            {
                if (_initiativeQueue.Peek().Count == 0)
                {
                    _initiativeQueue.Dequeue();
                    // we have reached the end of the current queue, which means we have completed a full round
                    Round++;
                    continue;
                }
                // we know that there is something in the queue at this point

                if (CombatMapUnitIsPresentAndActive(_initiativeQueue.Peek().Peek()))
                    break;
                // if the combat map unit is dead then we pop them off and move on
                _ = _initiativeQueue.Peek().Dequeue();
            }

            // if the queue is empty then we will recalculate it before continuing
            if (_initiativeQueue.Count == 0) CalculateNextInitiativeQueue();

            if (_initiativeQueue.Peek().Count == 0) return null;

            Debug.Assert(_initiativeQueue.Count > 0);
            Debug.Assert(_initiativeQueue.Peek().Count > 0);

            CombatMapUnit combatMapUnit = _initiativeQueue.Peek().Peek();
            if (combatMapUnit == null)
                throw new Ultima5ReduxException(
                    "Tried to get current combat unit from initiative queue, but they were null");
            return combatMapUnit;
        }

        /// <summary>
        ///     Clears all queues and tallies, and populates with new data
        /// </summary>
        internal void InitializeInitiativeQueue()
        {
            _initiativeQueue.Clear();
            _combatInitiativeTally.Clear();
            _nLowestDexterity = 50;
            // Highest player/enemy dexterity encountered
            int nHighestDexterity = 0;
            Round = 0;
            Turn = 0;

            foreach (CombatMapUnit combatMapUnit in _mapUnitCollection.AllCombatMapUnits)
            {
                switch (combatMapUnit)
                {
                    // if it's an enemy and they aren't an active attacker such as a POISON FIELD
                    // then we just skip them since they have a DEX of 0
                    case Enemy enemy when !enemy.EnemyReference.ActivelyAttacks:
                    // we definitely do not care about dex values for inanimate objects
                    case NonAttackingUnit:
                        continue;
                }

                int nDexterity = combatMapUnit.Dexterity;
                if (nDexterity == 0)
                    throw new Ultima5ReduxException($"Got a 0 dex value for an enemy {combatMapUnit.FriendlyName}");

                // get the highest and lowest dexterity values to be used in ongoing tally
                if (_nLowestDexterity > nDexterity) _nLowestDexterity = nDexterity;
                if (nHighestDexterity < nDexterity) nHighestDexterity = nDexterity;

                AddCombatMapUnitToQueue(combatMapUnit);
            }
        }

        private bool CombatMapUnitIsPresentAndActive(CombatMapUnit combatMapUnit)
        {
            if (combatMapUnit is CombatPlayer player)
            {
                return CombatPlayerIsActive(player) && CombatMapUnitIsPresentOnMap(combatMapUnit);
            }

            return CombatMapUnitIsPresentOnMap(combatMapUnit);
        }

        private bool CombatMapUnitIsPresentOnMap(CombatMapUnit combatMapUnit, bool bFilterNotVisible = true)
        {
            // if the combat unit is not visible then we skip them and don't ruin the surprise that they are on the map
            if (bFilterNotVisible && !IsCombatMapUnitVisible(combatMapUnit)) return false;

            // if we have enemies piled on top of each other (like dungeon 60)
            bool bIsOnTop = _combatMap.GetTopVisibleMapUnit(combatMapUnit.MapUnitPosition.XY, false) == combatMapUnit;

            return !combatMapUnit.HasEscaped && combatMapUnit.IsActive && combatMapUnit.Stats.CurrentHp > 0 && bIsOnTop;
        }

        private int GetNumberOfTurnsInQueue() =>
            _initiativeQueue.Sum(mapUnits => mapUnits.Count(CombatMapUnitIsPresentAndActive));

        private bool IsAtLeastNTurnsInQueue(int nMin)
        {
            int nTally = 0;

            foreach (Queue<CombatMapUnit> mapUnits in _initiativeQueue)
            {
                nTally += mapUnits.Count(CombatMapUnitIsPresentAndActive);
                // do a check inside the minimize the number of iterations once the minimum is met
                if (nTally >= nMin) return true;
            }

            return nTally >= nMin;
        }

        /// <summary>
        ///     Is the map unit a combat map unit type?
        /// </summary>
        /// <param name="mapUnit"></param>
        /// <returns></returns>
        private bool IsCombatMapUnit(MapUnit mapUnit)
        {
            return mapUnit switch
            {
                CombatPlayer playerUnit => CombatMapUnitIsPresentOnMap(playerUnit),
                Enemy enemyUnit => CombatMapUnitIsPresentOnMap(enemyUnit, false),
                NonAttackingUnit => false,
                _ => false
            };
        }

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
        private bool IsCombatMapUnitVisible(CombatMapUnit combatMapUnit) =>
            _combatMap.VisibleOnMap == null ||
            _combatMap.VisibleOnMap[combatMapUnit.MapUnitPosition.X][combatMapUnit.MapUnitPosition.Y];

        public void AddCombatMapUnitToQueue(CombatMapUnit combatMapUnit)
        {
            _combatInitiativeTally.Add(combatMapUnit, 0);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public List<CombatMapUnit> GetTopNCombatMapUnits(int nUnits)
        {
            int nTally = 0;
            List<CombatMapUnit> combatMapUnits = new(nUnits);

            while (!IsAtLeastNTurnsInQueue(nUnits))
            {
                // if we can't calculate any further initiatives then we break out
                if (!CalculateNextInitiativeQueue())
                    break;
            }

            foreach (CombatMapUnit combatMapUnit in _initiativeQueue.SelectMany(mapUnits => mapUnits))
            {
                if (!IsCombatMapUnitVisible(combatMapUnit)) continue;

                if (CombatMapUnitIsPresentAndActive(combatMapUnit))
                {
                    if (combatMapUnit is CombatPlayer player && !CombatPlayerIsActive(player)) continue;

                    combatMapUnits.Add(combatMapUnit);
                    nTally++;
                }

                if (nTally == nUnits) return combatMapUnits;
            }

            // we will return what we have which is likely zero
            return combatMapUnits;
        }

        public void RefreshFutureRounds()
        {
            if (_initiativeQueue.Count <= 1) return;

            Queue<CombatMapUnit> currentRound = _initiativeQueue.Dequeue();
            _initiativeQueue.Clear();
            _initiativeQueue.Enqueue(currentRound);
        }

        public void SetActivePlayerCharacter(PlayerCharacterRecord record)
        {
            ActivePlayerCharacterRecord = record;
            Turn += 1;
        }
    }
}