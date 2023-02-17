using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.Maps
{
    public abstract class RegularMap : Map
    {
        [DataMember] public bool DeclinedExtortion { get; set; }

        /// <summary>
        ///     Are you wanted by the guards? For example - did you murder someone?
        /// </summary>
        [DataMember]
        public bool IsWantedManByThePoPo { get; set; }

        /// <summary>
        ///     The single source of truth for the Avatar's current position within the current map
        /// </summary>
        [IgnoreDataMember]
        internal MapUnitPosition CurrentAvatarPosition
        {
            get => GetAvatarMapUnit().MapUnitPosition;
            private set => GetAvatarMapUnit().MapUnitPosition = value;
        }

        [IgnoreDataMember]
        public bool IsAvatarInFrigate =>
            GetAvatarMapUnit()?.CurrentBoardedMapUnit is Frigate;

        [IgnoreDataMember]
        public bool IsAvatarInSkiff =>
            GetAvatarMapUnit().CurrentBoardedMapUnit is Skiff;

        [IgnoreDataMember]
        public bool IsAvatarRidingCarpet =>
            GetAvatarMapUnit().CurrentBoardedMapUnit is MagicCarpet;

        [IgnoreDataMember]
        public bool IsAvatarRidingHorse =>
            GetAvatarMapUnit().CurrentBoardedMapUnit is Horse;


        [IgnoreDataMember] public bool IsAvatarRidingSomething => GetAvatarMapUnit().IsAvatarOnBoardedThing;


        [IgnoreDataMember]
        protected int TotalMapUnitsOnMap => CurrentMapUnits.AllMapUnits.Count(m => m is not EmptyMapUnit);

        [IgnoreDataMember]
        public override MapUnitPosition CurrentPosition
        {
            get => CurrentAvatarPosition;
            set => CurrentAvatarPosition = value;
        }

        [IgnoreDataMember]
        protected sealed override Dictionary<Point2D, TileOverrideReference> XYOverrides =>
            _xyOverrides ??= GameReferences.Instance.TileOverrideRefs.GetTileXYOverrides(CurrentSingleMapReference);

        private Dictionary<Point2D, TileOverrideReference> _xyOverrides;

        [JsonConstructor] protected RegularMap()
        {
        }

        protected RegularMap(SmallMapReferences.SingleMapReference.Location location, int mapFloor) : base(location,
            mapFloor)
        {
            MapLocation = location;
            MapFloor = mapFloor;
        }

        /// <summary>
        ///     Safe method to board a MapUnit and removing it from the world
        /// </summary>
        /// <param name="mapUnit"></param>
        internal void BoardAndCleanFromWorld(MapUnit mapUnit)
        {
            // board the unit
            GetAvatarMapUnit().BoardMapUnit(mapUnit);
            // clean it from the world so it no longer appears
            ClearAndSetEmptyMapUnits(mapUnit);
        }


        /// <summary>
        ///     Creates a new Magic Carpet and places it on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once OutParameterValueIsAlwaysDiscarded.Global
        internal MagicCarpet CreateMagicCarpet(Point2D xy, Point2D.Direction direction, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(TheMapType);

            if (nIndex == -1) return null;

            MagicCarpet magicCarpet = new(MapLocation, direction, null, new MapUnitPosition(xy.X, xy.Y, 0));

            AddNewMapUnit(TheMapType, magicCarpet, nIndex);
            return magicCarpet;
        }

        /// <summary>
        ///     Creates a skiff and places it on the map
        /// </summary>
        /// <param name="xy"></param>
        /// <param name="direction"></param>
        /// <param name="nIndex"></param>
        /// <returns></returns>
        // ReSharper disable once OutParameterValueIsAlwaysDiscarded.Global
        internal Skiff CreateSkiff(Point2D xy, Point2D.Direction direction, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(TheMapType);
            if (nIndex == -1) return null;

            Skiff skiff = new(
                new MapUnitMovement(nIndex),
                SmallMapReferences.SingleMapReference.Location.Britannia_Underworld, direction, null,
                new MapUnitPosition(xy.X, xy.Y, 0));

            AddNewMapUnit(Maps.Overworld, skiff, nIndex);
            return skiff;
        }

        internal void DamageShip(Point2D.Direction windDirection, TurnResults turnResults)
        {
            Avatar avatar = GetAvatarMapUnit();

            int nDamage = Utils.Ran.Next(5, 15);

            Debug.Assert(avatar.CurrentBoardedMapUnit is Frigate);
            if (avatar.CurrentBoardedMapUnit is not Frigate frigate)
                throw new Ultima5ReduxException("Tried to get Avatar's frigate, but it returned  null");

            // if the wind is blowing the same direction then we double the damage
            if (avatar.Direction == windDirection) nDamage *= 2;
            // decrement the damage from the frigate
            frigate.Hitpoints -= nDamage;

            turnResults.PushOutputToConsole(
                GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WorldStrings
                    .BREAKING_UP), false);
            // if we hit zero hitpoints then the ship is destroyed and a skiff is boarded
            if (frigate.Hitpoints <= 0)
            {
                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveShipDestroyed));
                // destroy the ship and leave board the Avatar onto a skiff
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                    DataOvlReference.WorldStrings2
                        .SHIP_SUNK_BANG_N), false);
                turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.WorldStrings2.ABANDON_SHIP_BANG_N).TrimEnd(), false);

                MapUnit newFrigate =
                    XitCurrentMapUnit(out string _);
                ClearAndSetEmptyMapUnits(newFrigate);
                MakeAndBoardSkiff();
            }
            else
            {
                if (frigate.Hitpoints <= 10)
                    turnResults.PushOutputToConsole(GameReferences.Instance.DataOvlRef.StringReferences.GetString(
                        DataOvlReference.WorldStrings
                            .HULL_WEAK), false);

                turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.ActionMoveShipBreakingUp));
            }
        }

        /// <summary>
        ///     Gathers the details of what if any aggressive action the mapunits would do this turn
        /// </summary>
        /// <returns></returns>
        internal Dictionary<MapUnit, VirtualMap.AggressiveMapUnitInfo> GetNonCombatMapAggressiveMapUnitInfo(
            TurnResults turnResults)
        {
            Dictionary<MapUnit, VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos = new();

            SmallMapReferences.SingleMapReference singleMapReference = CurrentSingleMapReference;
            if (singleMapReference == null)
                throw new Ultima5ReduxException(
                    "Tried to GetAggressiveMapUnitInfo but CurrentMap.CurrentSingleMapReference was null");

            foreach (MapUnit mapUnit in CurrentMapUnits.AllActiveMapUnits)
            {
                // we don't calculate any special movement or events for map units on different floors
                if (mapUnit.MapUnitPosition.Floor != CurrentSingleMapReference.Floor)
                    continue;

                // we don't want to add anything that can never attack, so we keep only enemies and NPCs 
                // in the list of aggressors
                switch (mapUnit)
                {
                    case Horse:
                    case Enemy:
                    case NonPlayerCharacter:
                        break;
                    default:
                        // it's not an aggressive Npc or Enemy so skip on past - nothing to see here
                        continue;
                }

                VirtualMap.AggressiveMapUnitInfo mapUnitInfo =
                    GetNonCombatMapAggressiveMapUnitInfo(mapUnit.MapUnitPosition.XY,
                        CurrentAvatarPosition.XY,
                        SingleCombatMapReference.Territory.Britannia, mapUnit);

                if (mapUnitInfo.CombatMapReference != null)
                    turnResults.PushOutputToConsole(mapUnitInfo.AttackingMapUnit.FriendlyName + " fight me in " +
                                                    mapUnitInfo.CombatMapReference.Description);
                aggressiveMapUnitInfos.Add(mapUnit, mapUnitInfo);
            }

            return aggressiveMapUnitInfos;
        }

        /// <summary>
        ///     Gets a suitable random position when wandering
        /// </summary>
        /// <param name="characterPosition">position of character</param>
        /// <param name="scheduledPosition">scheduled position of the character</param>
        /// <param name="nMaxDistance">max number of tiles the wander can be from the scheduled position</param>
        /// <param name="direction">OUT - the direction that the character should travel</param>
        /// <returns></returns>
        internal Point2D GetWanderCharacterPosition(Point2D characterPosition, Point2D scheduledPosition,
            int nMaxDistance, out MapUnitMovement.MovementCommandDirection direction)
        {
            Random ran = new();
            List<MapUnitMovement.MovementCommandDirection> possibleDirections =
                GetPossibleDirectionsList(characterPosition, scheduledPosition, nMaxDistance, true);

            // if no directions are returned then we tell them not to move
            if (possibleDirections.Count == 0)
            {
                direction = MapUnitMovement.MovementCommandDirection.None;

                return characterPosition.Copy();
            }

            direction = possibleDirections[ran.Next() % possibleDirections.Count];

            Point2D adjustedPosition = MapUnitMovement.GetAdjustedPos(characterPosition, direction);

            return adjustedPosition;
        }

        internal bool IsTileFreeToTravelForAvatar(in Point2D xy, bool bNoStaircases = false) =>
            IsTileFreeToTravel(xy, bNoStaircases, GetAvatarMapUnit().CurrentAvatarState);

        /// <summary>
        ///     Advances each of the NPCs by one movement each
        /// </summary>
        internal void MoveNonCombatMapMapUnitsToNextMove(
            Dictionary<MapUnit, VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos)
        {
            // go through each of the NPCs on the map
            foreach (MapUnit mapUnit in CurrentMapUnits.AllActiveMapUnits)
            {
                VirtualMap.AggressiveMapUnitInfo aggressiveMapUnitInfo =
                    aggressiveMapUnitInfos.ContainsKey(mapUnit) ? aggressiveMapUnitInfos[mapUnit] : null;
                // if we don't match the aggressive map unit then it means the map unit is not mobile
                if (aggressiveMapUnitInfo == null) continue;

                // if the map unit doesn't haven't a particular aggression then it moves 
                if (aggressiveMapUnitInfo.GetDecidedAction() == VirtualMap.AggressiveMapUnitInfo.DecidedAction.MoveUnit)
                    mapUnit.CompleteNextNonCombatMove(this, GameStateReference.State.TheTimeOfDay); //,
            }
        }

        // Performs all of the aggressive actions and stores results
        internal void ProcessNonCombatMapAggressiveMapUnitAttacks(PlayerCharacterRecords records,
            Dictionary<MapUnit, VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos,
            out VirtualMap.AggressiveMapUnitInfo combatMapAggressor, TurnResults turnResults)
        {
            combatMapAggressor = null;

            // if there are monsters with combat maps attached to them - then we look at them first
            // if you are going to a combat map then we will never process overworld ranged and melee attacks
            List<VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfosWithCombatMaps =
                aggressiveMapUnitInfos.Values.Where(ag => ag.CombatMapReference != null).ToList();

            if (aggressiveMapUnitInfosWithCombatMaps.Count > 0)
            {
                // there is at least one combat map reference
                int nCombatMapEnemies = aggressiveMapUnitInfosWithCombatMaps.Count;

                int nChoice = Utils.GetNumberFromAndTo(0, nCombatMapEnemies - 1);

                combatMapAggressor = aggressiveMapUnitInfosWithCombatMaps[nChoice];
                aggressiveMapUnitInfos.Clear();
                aggressiveMapUnitInfos[combatMapAggressor.AttackingMapUnit] = combatMapAggressor;
                return;
            }

            // we are certain at this point that there is no combat map, so it's all ranged if anything at all
            foreach (KeyValuePair<MapUnit, VirtualMap.AggressiveMapUnitInfo> kvp in aggressiveMapUnitInfos)
            {
                VirtualMap.AggressiveMapUnitInfo aggressiveMapUnitInfo = kvp.Value;
                MapUnit mapUnit = kvp.Key;

                Debug.Assert(aggressiveMapUnitInfo.CombatMapReference == null);

                // bajh: I know all the conditions look identical now - but I suspect they have different attack
                // powers I will tweak later

                VirtualMap.AggressiveMapUnitInfo.DecidedAction decidedAction = aggressiveMapUnitInfo.GetDecidedAction();
                switch (decidedAction)
                {
                    case VirtualMap.AggressiveMapUnitInfo.DecidedAction.AttemptToArrest:
                    {
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried to arrest me. They are a {mapUnit.GetType()}");
                        turnResults.PushTurnResult(
                            new AttemptToArrest(TurnResult.TurnResultType.NPCAttemptingToArrest, npc));
                        continue;
                    }
                    case VirtualMap.AggressiveMapUnitInfo.DecidedAction.WantsToChat:
                    {
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried to arrest me. They are a {mapUnit.GetType()}");
                        turnResults.PushTurnResult(new NpcTalkInteraction(npc));
                        // if they want to chat, then we start a pissed off counter
                        // it only really matters for guards though 
                        if (npc.NpcRef.IsGuard && npc.NpcState.PissedOffCountDown <= 0)
                            npc.NpcState.PissedOffCountDown = OddsAndLogic.TURNS_UNTIL_PISSED_OFF_GUARD_ARRESTS_YOU;
                        continue;
                    }
                    case VirtualMap.AggressiveMapUnitInfo.DecidedAction.Begging:
                    {
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried beg. They are a {mapUnit.GetType()}");
                        turnResults.PushTurnResult(new NpcTalkInteraction(npc));
                        continue;
                    }
                    case VirtualMap.AggressiveMapUnitInfo.DecidedAction.BlackthornGuardPasswordCheck:
                    {
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried extort half my gold. They are a {mapUnit.GetType()}");
                        if (npc.NpcState.HasExtortedAvatar) continue;
                        npc.NpcState.HasExtortedAvatar = true;
                        turnResults.PushTurnResult(new GuardExtortion(npc,
                            GuardExtortion.ExtortionType.BlackthornPassword, 0));
                        continue;
                    }
                    case VirtualMap.AggressiveMapUnitInfo.DecidedAction.StraightToBlackthornDungeon:
                        if (mapUnit is not NonPlayerCharacter blackthornGuard)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried extort half my gold. They are a {mapUnit.GetType()}");

                        turnResults.PushTurnResult(new GoToBlackthornDungeon(blackthornGuard));
                        break;
                    case VirtualMap.AggressiveMapUnitInfo.DecidedAction.HalfYourGoldExtortion:
                    {
                        // we only extort once per load of a map, we aren't monsters after all!
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried extort half my gold. They are a {mapUnit.GetType()}");
                        if (npc.NpcState.HasExtortedAvatar) continue;
                        npc.NpcState.HasExtortedAvatar = true;
                        turnResults.PushTurnResult(new GuardExtortion(npc, GuardExtortion.ExtortionType.HalfGold, 0));
                        continue;
                    }
                    case VirtualMap.AggressiveMapUnitInfo.DecidedAction.GenericGuardExtortion:
                    {
                        // we only extort once per load of a map, we aren't monsters after all!
                        if (mapUnit is not NonPlayerCharacter npc)
                            throw new Ultima5ReduxException(
                                $"A non-npc tried generic extortion. They are a {mapUnit.GetType()}");
                        if (npc.NpcState.HasExtortedAvatar) continue;
                        npc.NpcState.HasExtortedAvatar = true;
                        turnResults.PushTurnResult(new GuardExtortion(npc, GuardExtortion.ExtortionType.Generic,
                            OddsAndLogic.GetGuardExtortionAmount(
                                OddsAndLogic.GetEraByTurn(GameStateReference.State.TurnsSinceStart))));
                        continue;
                    }
                }

                // it's possible that the aggressor may not actually be attacking even if they can
                if (decidedAction != VirtualMap.AggressiveMapUnitInfo.DecidedAction.RangedAttack) continue;

                switch (aggressiveMapUnitInfo.AttackingMissileType)
                {
                    case CombatItemReference.MissileType.None:
                        break;

                    case CombatItemReference.MissileType.Arrow:
                        // do they have any melee attacks? Melee attacks are noted with .Arrow for now
                        // if on skiff then party takes damage
                        // if on frigate then frigate takes damage
                        if (IsAvatarInFrigate)
                            // frigate takes damage instead
                            DamageShip(Point2D.Direction.None, turnResults);
                        else
                            records.DamageEachCharacter(turnResults, 1, 9);

                        turnResults.PushOutputToConsole(
                            $"{mapUnit.FriendlyName} attacks {records.AvatarRecord.Name} and party (melee)", false);
                        continue;
                    case CombatItemReference.MissileType.CannonBall:
                        if (IsAvatarInFrigate)
                            DamageShip(Point2D.Direction.None, turnResults);
                        else
                            records.DamageEachCharacter(turnResults, 1, 9);

                        turnResults.PushOutputToConsole(
                            $"{mapUnit.FriendlyName} attacks {records.AvatarRecord.Name} and party (cannonball)",
                            false);

                        continue;
                    case CombatItemReference.MissileType.Red:
                        // if on a frigate then only the frigate takes damage, like a shield!
                        if (IsAvatarInFrigate)
                            DamageShip(Point2D.Direction.None, turnResults);
                        else
                            records.DamageEachCharacter(turnResults, 1, 9);

                        turnResults.PushOutputToConsole(
                            $"{mapUnit.FriendlyName} attacks {records.AvatarRecord.Name} and party (ranged)", false);

                        continue;
                    default:
                        throw new Ultima5ReduxException(
                            "Only \"Red\" and CannonBall ranged attacks have been configured");
                }
            }
        }


        private void ForceAttack(VirtualMap.AggressiveMapUnitInfo mapUnitInfo, TileReference attackFromTileReference)
        {
            mapUnitInfo.ForceDecidedAction(VirtualMap.AggressiveMapUnitInfo.DecidedAction.EnemyAttackCombatMap);
            SingleCombatMapReference singleCombatMapReference =
                GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(
                    attackFromTileReference.CombatMapIndex,
                    SingleCombatMapReference.Territory.Britannia);

            mapUnitInfo.CombatMapReference = singleCombatMapReference;
        }

        /// <summary>
        ///     Create a list of the free spaces surrounding around the Avatar suitable for something to be generated onto
        ///     Uses all 8 directions
        /// </summary>
        /// <returns></returns>
        private List<Point2D> GetFreeSpacesSurroundingAvatar()
        {
            List<Point2D> freeSpacesAroundAvatar = new();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Point2D pointToCheck = new(Math.Max(CurrentPosition.X + x, 0),
                        Math.Max(CurrentPosition.Y + y, 0));
                    if (!IsMapUnitOccupiedTile(pointToCheck) && GetTileReference(pointToCheck).IsWalking_Passable)
                        freeSpacesAroundAvatar.Add(pointToCheck);
                }
            }

            return freeSpacesAroundAvatar;
        }

        /// <summary>
        ///     Gets the appropriate (if any) SingleCombatMapReference based on the map and mapunits attempting to engage in
        ///     combat
        /// </summary>
        /// <param name="attackFromPosition">where are they attacking from</param>
        /// <param name="attackToPosition">where are they attack to</param>
        /// <param name="territory"></param>
        /// <param name="aggressorMapUnit">who is the one attacking?</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>
        private VirtualMap.AggressiveMapUnitInfo GetNonCombatMapAggressiveMapUnitInfo(Point2D attackFromPosition,
            Point2D attackToPosition, SingleCombatMapReference.Territory territory, MapUnit aggressorMapUnit)
        {
            SingleCombatMapReference getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps map) =>
                GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(territory, (int)map);

            TileReference attackToTileReference = GetTileReference(attackToPosition);
            TileReference attackFromTileReference = GetTileReference(attackFromPosition);

            List<MapUnit> mapUnits = GetMapUnitsByPosition(attackToPosition,
                CurrentSingleMapReference.Floor);

            switch (mapUnits.Count)
            {
                case 0:
                    break;
                case > 1:
                    // the only excuse you can have for having more than one is if the avatar is on top of a known map unit
                    if (mapUnits.Any(m => m is Avatar))
                    {
                    }
                    else
                    {
                        throw new Ultima5ReduxException($"Did not expect {mapUnits.Count} mapunits on targeted tile");
                    }

                    break;
            }

            VirtualMap.AggressiveMapUnitInfo mapUnitInfo = new(aggressorMapUnit);

            bool bIsMadGuard = false;
            if (aggressorMapUnit is NonPlayerCharacter npc) bIsMadGuard = IsWantedManByThePoPo && npc.NpcRef.IsGuard;

            // if the guard is next to you, then they will ask you to come quietly
            bool bNextToEachOther = attackFromPosition.IsWithinNFourDirections(attackToPosition);
            if (bIsMadGuard && bNextToEachOther && !TileReferences.IsHeadOfBed(
                    GetTileReference(aggressorMapUnit.MapUnitPosition.XY).Index))
            {
                // if they are at the head of the bed then we don't try to arrest, this keeps guards who are "injured" 
                // at the healers from trying to arrest
                // if avatar is being attacked..
                // we get to assume that the avatar is not necessarily next to the enemy
                mapUnitInfo.ForceDecidedAction(VirtualMap.AggressiveMapUnitInfo.DecidedAction.AttemptToArrest);
            }

            // IF NPC is next to Avatar then we check for any AI behaviours such as arrests or extortion
            if (bNextToEachOther && aggressorMapUnit is NonPlayerCharacter nextToEachOtherNpc)
            {
                NonPlayerCharacterSchedule.AiType aiType =
                    aggressorMapUnit.GetCurrentAiType(GameStateReference.State.TheTimeOfDay);

                // because this overrides a LOT of AI behaviours, I just let all guards try to attack
                // you if you turned down the extortion
                if (nextToEachOtherNpc.NpcRef.IsGuard && IsWantedManByThePoPo && DeclinedExtortion)
                {
                    ForceAttack(mapUnitInfo, attackFromTileReference);
                }
                else
                {
                    switch (aiType)
                    {
                        case NonPlayerCharacterSchedule.AiType.BlackthornGuardFixed:
                        case NonPlayerCharacterSchedule.AiType.BlackthornGuardWander:
                            // let's add some randomness and only check them half the time
                            if (Utils.OneInXOdds(2))
                            {
                                // are you wearing the black badge? 
                                // temporary - if you have the badge then that's good enough
                                if (GameStateReference.State.CharacterRecords.WearingBlackBadge)
                                {
                                    // if the guard has already harassed the Avatar, then they won't bug him
                                    // again until he re-enters the castle
                                    mapUnitInfo.ForceDecidedAction(VirtualMap.AggressiveMapUnitInfo.DecidedAction
                                        .BlackthornGuardPasswordCheck);
                                }
                                else
                                {
                                    mapUnitInfo.ForceDecidedAction(VirtualMap.AggressiveMapUnitInfo.DecidedAction
                                        .StraightToBlackthornDungeon);
                                }
                            }

                            break;
                        case NonPlayerCharacterSchedule.AiType.Begging:
                            mapUnitInfo.ForceDecidedAction(VirtualMap.AggressiveMapUnitInfo.DecidedAction.Begging);
                            break;
                        case NonPlayerCharacterSchedule.AiType.HalfYourGoldExtortingGuard:
                        case NonPlayerCharacterSchedule.AiType.GenericExtortingGuard:
                        case NonPlayerCharacterSchedule.AiType.ExtortOrAttackOrFollow:
                            // even if they are extortionists, if you did some super bad, they will try to arrest 
                            if (IsWantedManByThePoPo && DeclinedExtortion)
                            {
                                // attack them
                                goto case NonPlayerCharacterSchedule.AiType.DrudgeWorthThing;
                            }

                            if (IsWantedManByThePoPo)
                            {
                                mapUnitInfo.ForceDecidedAction(VirtualMap.AggressiveMapUnitInfo.DecidedAction
                                    .AttemptToArrest);
                                break;
                            }

                            mapUnitInfo.ForceDecidedAction(
                                aiType == NonPlayerCharacterSchedule.AiType.HalfYourGoldExtortingGuard
                                    ? VirtualMap.AggressiveMapUnitInfo.DecidedAction.HalfYourGoldExtortion
                                    : VirtualMap.AggressiveMapUnitInfo.DecidedAction.GenericGuardExtortion);
                            break;
                        case NonPlayerCharacterSchedule.AiType.SmallWanderWantsToChat:
                            // if they wanted to chat and they are a guard they can get pissed off and arrest you
                            if (IsWantedManByThePoPo || (nextToEachOtherNpc.NpcState.PissedOffCountDown == 0 &&
                                                         nextToEachOtherNpc.NpcRef.IsGuard))
                            {
                                mapUnitInfo.ForceDecidedAction(VirtualMap.AggressiveMapUnitInfo.DecidedAction
                                    .AttemptToArrest);
                            }
                            else
                            {
                                // some times non guard NPCs are just keen to chat
                                mapUnitInfo.ForceDecidedAction(VirtualMap.AggressiveMapUnitInfo.DecidedAction
                                    .WantsToChat);
                            }

                            break;
                        case NonPlayerCharacterSchedule.AiType.FixedExceptAttackWhenIsWantedByThePoPo:
                            // wow - I just leaned how to do this goto
                            // they only attack when you are wanted by the popo
                            if (IsWantedManByThePoPo) ForceAttack(mapUnitInfo, attackFromTileReference);

                            break;
                        case NonPlayerCharacterSchedule.AiType.DrudgeWorthThing:
                            ForceAttack(mapUnitInfo, attackFromTileReference);
                            break;
                    }
                }
            }
            // if a guard wants to chat, they lose patience after a while and want to arrest you
            // so we count down like a stern parent
            else if (aggressorMapUnit is NonPlayerCharacter pissedOffNonPlayerCharacter
                     && pissedOffNonPlayerCharacter.NpcState.PissedOffCountDown > 0
                     && pissedOffNonPlayerCharacter.NpcRef.IsGuard
                     && pissedOffNonPlayerCharacter.NpcState.OverridenAiType ==
                     NonPlayerCharacterSchedule.AiType.SmallWanderWantsToChat)
            {
                pissedOffNonPlayerCharacter.NpcState.PissedOffCountDown--;
            }

            if (aggressorMapUnit is not Enemy enemy) return mapUnitInfo;

            if (!bNextToEachOther)
            {
                switch (enemy.EnemyReference.LargeMapMissileType)
                {
                    case CombatItemReference.MissileType.None:
                        return mapUnitInfo;
                    // pirates = cannonball, snakes = poison, serpents = red, squid =
                    // the aggressor is an enemy so let's check to see if they have LargeMap projectiles
                    case CombatItemReference.MissileType.CannonBall:
                        // if it's a cannon ball, and they are on the same X or Y then it can fire!
                        if ((attackFromPosition.X == attackToPosition.X ||
                             attackFromPosition.Y == attackToPosition.Y) &&
                            attackFromPosition.IsWithinN(attackToPosition, 3))
                            mapUnitInfo.AttackingMissileType = CombatItemReference.MissileType.CannonBall;
                        break;
                    default:
                        // it's not a cannon ball but it is a missile
                        if (attackFromPosition.IsWithinN(attackToPosition, 3))
                            mapUnitInfo.AttackingMissileType = enemy.EnemyReference.LargeMapMissileType;
                        break;
                }

                return mapUnitInfo;
            }

            bool bIsPirate = enemy.EnemyReference.LargeMapMissileType == CombatItemReference.MissileType.CannonBall;
            // if avatar on skiff or carpet and avatar is on water then it's immediate ouch, no map
            if ((IsAvatarInSkiff || IsAvatarRidingCarpet) &&
                attackToTileReference.CombatMapIndex is SingleCombatMapReference.BritanniaCombatMaps.BoatCalc)
            {
                // we will use Arrow to denote the enemy attacking on the overworld, but no combat map
                mapUnitInfo.AttackingMissileType = CombatItemReference.MissileType.Arrow;
            }
            // avatar not on a boat
            // return avatar's current tile combat map
            else if (!IsAvatarInFrigate)
            {
                // when the avatar is not in boat and a water enemy attacks - they will always fight in the bay
                if (enemy.EnemyReference.IsWaterEnemy)
                {
                    mapUnitInfo.CombatMapReference = getSingleCombatMapReference(bIsPirate
                        ? SingleCombatMapReference.BritanniaCombatMaps.BoatNorth
                        : SingleCombatMapReference.BritanniaCombatMaps.Bay);
                }
                else
                {
                    // if you end up on a bay tile, but the monster is not a water monster, then we need to either
                    // substitute another map, or have them attack them in the overworld
                    if (attackToTileReference.CombatMapIndex is SingleCombatMapReference.BritanniaCombatMaps.Bay)
                    {
                        mapUnitInfo.CombatMapReference = null;
                        mapUnitInfo.AttackingMissileType = CombatItemReference.MissileType.Arrow;
                    }
                    else
                    {
                        mapUnitInfo.CombatMapReference =
                            getSingleCombatMapReference(attackToTileReference.CombatMapIndex);
                    }
                }
            }
            // if the enemy is a water enemy and we know the avatar is on a frigate, then we fight on the ocean
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            else if (enemy.EnemyReference.IsWaterEnemy)
            {
                // we are on a frigate AND we are fighting a pirate ship
                mapUnitInfo.CombatMapReference = getSingleCombatMapReference(bIsPirate
                    ? SingleCombatMapReference.BritanniaCombatMaps.BoatBoat
                    : SingleCombatMapReference.BritanniaCombatMaps.BoatOcean);
            }
            else
            {
                if (!enemy.EnemyReference.IsWaterEnemy)
                    // NOTE: this looks like unreachable code
                    mapUnitInfo.CombatMapReference =
                        getSingleCombatMapReference(SingleCombatMapReference.BritanniaCombatMaps.BoatSouth);
                else
                    mapUnitInfo.CombatMapReference = GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(
                        territory,
                        (int)attackToTileReference.CombatMapIndex);
            }

            return mapUnitInfo;
        }

        private Point2D GetPositionIfUserCanMove(MapUnitMovement.MovementCommandDirection direction,
            Point2D characterPosition, bool bNoStaircases, Point2D scheduledPosition, int nMaxDistance)
        {
            Point2D adjustedPosition = MapUnitMovement.GetAdjustedPos(characterPosition, direction);

            // always include none
            if (direction == MapUnitMovement.MovementCommandDirection.None) return adjustedPosition;

            if (adjustedPosition.X < 0 || adjustedPosition.X >= TheMap.Length || adjustedPosition.Y < 0 ||
                adjustedPosition.Y >= TheMap[0].Length) return null;

            // is the tile free to travel to? even if it is, is it within N tiles of the scheduled tile?
            if (IsTileFreeToTravelForAvatar(adjustedPosition, bNoStaircases) &&
                scheduledPosition.IsWithinN(adjustedPosition, nMaxDistance)) return adjustedPosition;

            return null;
        }

        /// <summary>
        ///     Gets possible directions that are accessible from a particular point
        /// </summary>
        /// <param name="characterPosition">the current position of the character</param>
        /// <param name="scheduledPosition">the place they are supposed to be</param>
        /// <param name="nMaxDistance">max distance they can travel from that position</param>
        /// <param name="bNoStaircases"></param>
        /// <returns></returns>
        private List<MapUnitMovement.MovementCommandDirection> GetPossibleDirectionsList(in Point2D characterPosition,
            in Point2D scheduledPosition, int nMaxDistance, bool bNoStaircases)
        {
            List<MapUnitMovement.MovementCommandDirection> directionList = new();

            // gets an adjusted position OR returns null if the position is not valid

            foreach (MapUnitMovement.MovementCommandDirection direction in Enum.GetValues(
                         typeof(MapUnitMovement.MovementCommandDirection)))
            {
                // we may be asked to avoid including .None in the list
                if (direction == MapUnitMovement.MovementCommandDirection.None) continue;

                Point2D adjustedPos = GetPositionIfUserCanMove(direction, characterPosition, bNoStaircases,
                    scheduledPosition, nMaxDistance);
                // if adjustedPos == null then the particular direction was not allowed for one reason or another
                if (adjustedPos != null) directionList.Add(direction);
            }

            return directionList;
        }

        public int ClosestTileReferenceAround(int nRadius, Func<int, bool> checkTile) =>
            ClosestTileReferenceAround(CurrentPosition.XY, nRadius, checkTile);

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public Horse CreateHorse(MapUnitPosition mapUnitPosition, Maps map, out int nIndex)
        {
            nIndex = FindNextFreeMapUnitIndex(TheMapType);
            if (nIndex == -1) return null;

            Horse horse = new(
                new MapUnitMovement(nIndex),
                //importedMovements.GetMovement(nIndex), 
                MapLocation, Point2D.Direction.Right,
                null, mapUnitPosition)
            {
                MapUnitPosition = mapUnitPosition
            };

            // set position of frigate in the world
            AddNewMapUnit(map, horse, nIndex);
            return horse;
        }

        /// <summary>
        ///     Creates a horse MapUnit in the surrounding tiles of the Avatar - if one exists
        /// </summary>
        /// <returns>the new horse or null if there was no where to put it</returns>
        public Horse CreateHorseAroundAvatar(TurnResults turnResults)
        {
            List<Point2D> freeSpacesAroundAvatar = GetFreeSpacesSurroundingAvatar();
            if (freeSpacesAroundAvatar.Count <= 0) return null;

            Random ran = new();
            Point2D chosenLocation = freeSpacesAroundAvatar[ran.Next() % freeSpacesAroundAvatar.Count];
            Horse horse = CreateHorse(
                new MapUnitPosition(chosenLocation.X, chosenLocation.Y, CurrentPosition.Floor), TheMapType,
                out int nIndex);

            if (nIndex == -1 || horse == null) return null;

            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.PoofHorse));

            return horse;
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        protected MoonstoneNonAttackingUnit CreateMoonstoneNonAttackingUnit(Point2D xy, Moonstone moonstone,
            SmallMapReferences.SingleMapReference singleMapReference)
        {
            int nIndex = FindNextFreeMapUnitIndex(TheMapType);
            if (nIndex == -1) return null;

            MapUnitPosition mapUnitPosition = new(xy.X, xy.Y, singleMapReference.Floor);
            var moonstoneNonAttackingUnit =
                new MoonstoneNonAttackingUnit(moonstone, mapUnitPosition);

            // set position of frigate in the world
            CurrentMapUnits.AddMapUnit(moonstoneNonAttackingUnit);
            return moonstoneNonAttackingUnit;
        }

        public Avatar GetAvatarMapUnit()
        {
            if (CurrentMapUnits == null)
                throw new Ultima5ReduxException("Tried to get Avatar but CurrentMapUnits is null");
            if (CurrentMapUnits.TheAvatar == null)
                throw new Ultima5ReduxException("Tried to get Avatar but CurrentMapUnits.TheAvatar is null");

            return CurrentMapUnits.TheAvatar;
        }

        public SingleCombatMapReference GetCombatMapReferenceForAvatarAttacking(Point2D attackFromPosition,
            Point2D attackToPosition, SingleCombatMapReference.Territory territory)
        {
            // note - attacking from a skiff OR carpet is NOT permitted unless touching a piece of land 
            // otherwise is twill say Attack-On foot!
            // note - cannot exit a skiff unless land is nearby

            // let's use this method to also determine if an enemy CAN attack the avatar from afar

            TileReference attackToTileReference = GetTileReference(attackToPosition);
            if (attackToTileReference.CombatMapIndex == SingleCombatMapReference.BritanniaCombatMaps.None) return null;

            TileReference attackFromTileReference = GetTileReference(attackFromPosition);

            List<MapUnit> mapUnits = GetMapUnitsByPosition(attackToPosition,
                CurrentSingleMapReference.Floor);

            MapUnit targetedMapUnit = null;
            TileReference targetedMapUniTileReference = null;

            switch (mapUnits.Count)
            {
                case 0:
                    // the avatar is attacking, but actually doesn't have anyone directly in their sights
                    // nothing to do
                    return null;
                case >= 1:
                    // the only excuse you can have for having more than one is if the avatar is on top of a known map unit
                    if (mapUnits.Any(m => m is Avatar))
                        throw new Ultima5ReduxException(
                            "Did not expect Avatar mapunit on targeted tile when Avatar is attacking");

                    // a little lazy for now
                    targetedMapUnit = mapUnits[0];
                    targetedMapUniTileReference = targetedMapUnit.KeyTileReference;
                    break;
            }

            // if the avatar is in a skiff of on a carpet, but is in the ocean then they aren't allowed to attack
            if (IsAvatarInSkiff || IsAvatarRidingCarpet)
            {
                bool bAvatarOnWaterTile = attackFromTileReference.IsWaterTile;

                if (bAvatarOnWaterTile &&
                    attackToTileReference.CombatMapIndex is
                        SingleCombatMapReference.BritanniaCombatMaps
                            .BoatCalc) // if no surrounding tiles are water tile then we skip the attack
                    return null;
            }

            // there is someone to target
            if (!IsAvatarInFrigate)
            {
                // last second check for water enemy - they can occasionally appear on a "land" tile like bridges
                // so we take the chance to force a Bay map just in case
                if (targetedMapUnit is Enemy waterCheckEnemy)
                {
                    if (waterCheckEnemy.EnemyReference.IsWaterEnemy)
                        return GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(
                            SingleCombatMapReference.BritanniaCombatMaps.Bay, territory);
                    // if the enemy is on bay but is not a water creature then we cannot attack them
                    if (attackToTileReference.CombatMapIndex == SingleCombatMapReference.BritanniaCombatMaps.Bay)
                        return null;
                }

                return GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(
                    attackToTileReference.CombatMapIndex, territory);
            }

            // BoatCalc indicates it is a water tile and requires special consideration
            if (attackToTileReference.CombatMapIndex != SingleCombatMapReference.BritanniaCombatMaps.BoatCalc)
            {
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (attackToTileReference.IsWaterEnemyPassable)
                    return GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(
                        SingleCombatMapReference.BritanniaCombatMaps.BoatOcean,
                        territory);

                // BoatSouth indicates the avatar is on the frigate, and the enemy on land
                return GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(
                    SingleCombatMapReference.BritanniaCombatMaps.BoatSouth, territory);
            }

            // if attacking another frigate, then it's boat to boat
            if (GameReferences.Instance.SpriteTileReferences.IsFrigate(targetedMapUniTileReference.Index))
                return GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(
                    SingleCombatMapReference.BritanniaCombatMaps.BoatBoat, territory);

            // otherwise it's boat (ours) to ocean
            return GameReferences.Instance.CombatMapRefs.GetSingleCombatMapReference(
                SingleCombatMapReference.BritanniaCombatMaps.BoatOcean, territory);
            // it is not boat calc, but there is an enemy, so refer to our default combat map
        }

        /// <summary>
        ///     Gets a map unit on the current tile (that ISN'T the Avatar)
        /// </summary>
        /// <returns>MapUnit or null if none exist</returns>
        public MapUnit GetMapUnitOnCurrentTile() => GetTopVisibleMapUnit(CurrentPosition.XY, true);


        /// <summary>
        ///     Gets a tile reference from the tile the avatar currently resides on
        /// </summary>
        /// <returns></returns>
        public TileReference GetTileReferenceOnCurrentTile() => GetTileReference(CurrentPosition.XY);

        public bool IsLandNearbyForAvatar() =>
            IsLandNearby(CurrentPosition.XY, false, GetAvatarMapUnit().CurrentAvatarState);

        // ReSharper disable once UnusedMethodReturnValue.Local
        private Skiff MakeAndBoardSkiff()
        {
            Skiff skiff = CreateSkiff(GetAvatarMapUnit().MapUnitPosition.XY, GetAvatarMapUnit().Direction,
                out int _);
            GetAvatarMapUnit().BoardMapUnit(skiff);
            ClearAndSetEmptyMapUnits(skiff);
            return skiff;
        }

        public void MoveAvatar(Point2D newPosition)
        {
            CurrentAvatarPosition =
                new MapUnitPosition(newPosition.X, newPosition.Y, CurrentAvatarPosition.Floor);
        }

        /// <summary>
        ///     Makes the Avatar exit the current MapUnit they are occupying
        /// </summary>
        /// <returns>The MapUnit object they were occupying - you need to re-add it the map after</returns>
        public MapUnit XitCurrentMapUnit(out string retStr)
        {
            retStr = GameReferences.Instance.DataOvlRef.StringReferences
                .GetString(DataOvlReference.KeypressCommandsStrings.XIT)
                .TrimEnd();

            if (!GetAvatarMapUnit().IsAvatarOnBoardedThing)
            {
                retStr += " " + GameReferences.Instance.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.KeypressCommandsStrings.WHAT_Q).Trim();
                return null;
            }

            if (!GetAvatarMapUnit().CurrentBoardedMapUnit.CanBeExited(this))
            {
                retStr += "\n" + GameReferences.Instance.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.SleepTransportStrings.N_NO_LAND_NEARBY_BANG_N).Trim();
                return null;
            }

            MapUnit unboardedMapUnit = GetAvatarMapUnit().UnboardedAvatar();
            Debug.Assert(unboardedMapUnit != null);

            // set the current positions to the equal the Avatar's as he exits the vehicle 
            unboardedMapUnit.MapLocation = MapLocation;
            unboardedMapUnit.MapUnitPosition = CurrentAvatarPosition;
            unboardedMapUnit.Direction = GetAvatarMapUnit().Direction;
            unboardedMapUnit.KeyTileReference = unboardedMapUnit.GetNonBoardedTileReference();

            AddNewMapUnit(TheMapType, unboardedMapUnit);
            retStr += " " + unboardedMapUnit.BoardXitName;

            // if the Avatar is on a frigate then we will check for Skiffs and exit on a skiff instead
            if (unboardedMapUnit is not Frigate avatarFrigate) return unboardedMapUnit;

            Debug.Assert(avatarFrigate != null, nameof(avatarFrigate) + " != null");

            // if we have skiffs, AND do not have land close by then we deploy a skiff
            if (avatarFrigate.SkiffsAboard <= 0 || IsLandNearby(GetAvatarMapUnit().CurrentAvatarState))
                return unboardedMapUnit;

            MakeAndBoardSkiff();
            avatarFrigate.SkiffsAboard--;

            return unboardedMapUnit;
        }

        /// <summary>
        ///     Generates a new map unit
        /// </summary>
        /// <param name="mapUnitMovement"></param>
        /// <param name="bInitialLoad"></param>
        /// <param name="location"></param>
        /// <param name="npcState"></param>
        /// <param name="tileReference"></param>
        /// <param name="smallMapCharacterState"></param>
        /// <param name="mapUnitPosition"></param>
        /// <returns></returns>
        protected MapUnit CreateNewMapUnit(MapUnitMovement mapUnitMovement, bool bInitialLoad,
            SmallMapReferences.SingleMapReference.Location location, NonPlayerCharacterState npcState,
            MapUnitPosition mapUnitPosition, TileReference tileReference,
            SmallMapCharacterState smallMapCharacterState = null)
        {
            MapUnit newUnit;

            if (tileReference == null || tileReference.Index == 256)
            {
                Debug.WriteLine("An empty map unit was created with no tile reference");
                newUnit = new EmptyMapUnit();
            }
            else if (smallMapCharacterState != null && npcState != null && smallMapCharacterState.Active &&
                     npcState.NPCRef.NormalNPC)
            {
                newUnit = new NonPlayerCharacter(smallMapCharacterState, mapUnitMovement, bInitialLoad, location,
                    mapUnitPosition, npcState);
                // special condition where people who were previously freed are not dead, but also do not appear 
                if (newUnit.OverrideAiType && !GameStateReference.State.TheGameOverrides.PreferenceFreedPeopleDontDie)
                {
                    NonPlayerCharacterSchedule.AiType aiType =
                        newUnit.GetCurrentAiType(GameStateReference.State.TheTimeOfDay);
                    if (aiType == NonPlayerCharacterSchedule.AiType.FollowAroundAndBeAnnoyingThenNeverSeeAgain)
                        newUnit = new EmptyMapUnit();
                }
            }
            else if (GameReferences.Instance.SpriteTileReferences.IsFrigate(tileReference.Index))
            {
                newUnit = new Frigate(mapUnitMovement, location, tileReference.GetDirection(), npcState,
                    mapUnitPosition);
            }
            else if (GameReferences.Instance.SpriteTileReferences.IsSkiff(tileReference.Index))
            {
                newUnit = new Skiff(mapUnitMovement, location, tileReference.GetDirection(), npcState, mapUnitPosition);
            }
            else if (TileReferences.IsMagicCarpet(tileReference.Index))
            {
                newUnit = new MagicCarpet(location, tileReference.GetDirection(), npcState, mapUnitPosition);
            }
            else if (TileReferences.IsUnmountedHorse(tileReference.Index))
            {
                newUnit = new Horse(mapUnitMovement, location, tileReference.GetDirection(), npcState, mapUnitPosition);
            }
            else if (smallMapCharacterState != null && npcState != null && smallMapCharacterState.Active)
            {
                // This is where we will do custom stuff for special NPS
                // guard or daemon or stone gargoyle or fighter or bard or townes person or rat or bat or shadowlord
                if ((TileReference.SpriteIndex)npcState.NPCRef.NPCKeySprite is
                    TileReference.SpriteIndex.Guard_KeyIndex
                    or TileReference.SpriteIndex.Daemon1_KeyIndex
                    or TileReference.SpriteIndex.StoneGargoyle_KeyIndex
                    or TileReference.SpriteIndex.Fighter_KeyIndex
                    or TileReference.SpriteIndex.Bard_KeyIndex
                    or TileReference.SpriteIndex.TownsPerson_KeyIndex
                    or TileReference.SpriteIndex.Rat_KeyIndex
                    or TileReference.SpriteIndex.Bat_KeyIndex
                    or TileReference.SpriteIndex.ShadowLord_KeyIndex)
                {
                    newUnit = new NonPlayerCharacter(smallMapCharacterState, mapUnitMovement, bInitialLoad, location,
                        mapUnitPosition, npcState);
                }
                else
                {
                    newUnit = NonAttackingUnitFactory.Create(npcState.NPCRef.NPCKeySprite, location, mapUnitPosition);
                    newUnit.MapLocation = location;
                }
            }
            else if (GameReferences.Instance.SpriteTileReferences.IsMonster(tileReference.Index))
            {
                Debug.Assert(GameReferences.Instance.EnemyRefs != null);
                newUnit = new Enemy(mapUnitMovement, GameReferences.Instance.EnemyRefs.GetEnemyReference(tileReference),
                    location, npcState, mapUnitPosition);
            }
            // this is where we will create monsters too
            else
            {
                Debug.WriteLine("An empty map unit was created with " + tileReference.Name);
                newUnit = new EmptyMapUnit();
            }

            // force to use extended sprites if they exist
            newUnit.UseFourDirections = UseExtendedSprites;

            return newUnit;
        }
    }
}