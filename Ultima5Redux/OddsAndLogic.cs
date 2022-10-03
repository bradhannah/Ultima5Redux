using System;
using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux
{
    public static class OddsAndLogic
    {
        private const float ODDS_BASE_COMPLEX_TRAP_EXPLODE_ON_SEARCH = 0.4f;
        private const float ODDS_BASE_SIMPLE_TRAP_EXPLODE_ON_SEARCH = 0.2f;

        private const float ODDS_CHEST_LOCKED = 0.2f;
        private const float ODDS_COMPLEX_TRAP_ON_CHEST = 0.2f;

        /// <summary>
        ///     Improvement to odds of not setting off a trap based on a characters dexterity
        /// </summary>
        private const float ODDS_DEX_ADJUST_TRAP_EXPLODE = 0.01f;

        private const float ODDS_SIMPLE_TRAP_ON_CHEST = 0.2f;
        private const float ODDS_TREASURE_FROM_BLOOD_SPATTER = 0.2f;

        private const float ODDS_TREASURE_FROM_SEARCHING_BODY = 0.2f;
        private const int WEIGHT_BLOODSPATTER_TRAP_NONE = 3;

        private const int WEIGHT_BLOODSPATTER_TRAP_POISON = 1;

        private const int WEIGHT_CHEST_TRAP_ACID = 9;
        private const int WEIGHT_CHEST_TRAP_BOMB = 1;

        private const int WEIGHT_CHEST_TRAP_NONE = WEIGHT_CHEST_TRAP_ACID + WEIGHT_CHEST_TRAP_SLEEP +
                                                   WEIGHT_CHEST_TRAP_POISON + WEIGHT_CHEST_TRAP_BOMB;

        private const int WEIGHT_CHEST_TRAP_POISON = 3;
        private const int WEIGHT_CHEST_TRAP_SLEEP = 3;
        private const int WEIGHT_DEADBODY_TRAP_NONE = 3;

        private const int WEIGHT_DEADBODY_TRAP_POISON = 1;

        private static readonly Dictionary<NonAttackingUnit.TrapType, int> ChestTrapsWeighted = new()
        {
            { NonAttackingUnit.TrapType.ACID, WEIGHT_CHEST_TRAP_ACID },
            { NonAttackingUnit.TrapType.BOMB, WEIGHT_CHEST_TRAP_BOMB },
            { NonAttackingUnit.TrapType.POISON, WEIGHT_CHEST_TRAP_POISON },
            { NonAttackingUnit.TrapType.SLEEP, WEIGHT_CHEST_TRAP_SLEEP },
            { NonAttackingUnit.TrapType.NONE, WEIGHT_CHEST_TRAP_NONE }
        };

        private static readonly Dictionary<NonAttackingUnit.TrapType, int> DeadBodyTrapsWeighted = new()
        {
            { NonAttackingUnit.TrapType.POISON, WEIGHT_DEADBODY_TRAP_POISON + AGGRESSIVE_TRAP_MODIFIER },
            { NonAttackingUnit.TrapType.NONE, WEIGHT_DEADBODY_TRAP_NONE }
        };

        private static readonly Dictionary<NonAttackingUnit.TrapType, int> BloodSpatterTrapsWeighted = new()
        {
            { NonAttackingUnit.TrapType.POISON, WEIGHT_BLOODSPATTER_TRAP_POISON + AGGRESSIVE_TRAP_MODIFIER },
            { NonAttackingUnit.TrapType.NONE, WEIGHT_BLOODSPATTER_TRAP_NONE }
        };

        private static readonly List<NonAttackingUnit.TrapType> BloodSpatterTrapsWeightedList =
            Utils.MakeWeightedList(BloodSpatterTrapsWeighted).ToList();


        private static readonly List<NonAttackingUnit.TrapType> ChestTrapsWeightedList =
            Utils.MakeWeightedList(ChestTrapsWeighted).ToList();


        private static readonly List<NonAttackingUnit.TrapType> DeadBodyTrapsWeightedList =
            Utils.MakeWeightedList(DeadBodyTrapsWeighted).ToList();

        private static readonly Dictionary<TileReference.SpriteIndex, int> GenericDropAfterKillingEnemy =
            new()
            {
                { TileReference.SpriteIndex.Nothing, 10 },
                { TileReference.SpriteIndex.Chest, 3 },
                { TileReference.SpriteIndex.BloodSpatter, 3 }
                //{ NonAttackingUnitFactory.DropSprites.DeadBody, 3 }
            };

        private static readonly List<TileReference.SpriteIndex> GenericDropAfterKillingEnemyList =
            Utils.MakeWeightedList(GenericDropAfterKillingEnemy).ToList();

        public const int ACID_DAMAGE_MAX = 10;

        public const int ACID_DAMAGE_MIN = 3;
        public const int AGGRESSIVE_TRAP_MODIFIER = AGGRESSIVE_TRAPS ? 100 : 0;
        public const bool AGGRESSIVE_TRAPS = true;
        public const int BOMB_DAMAGE_MAX = 10;

        public const int BOMB_DAMAGE_MIN = 3;
        public const int ELECTRIC_DAMAGE_MAX = 10;

        public const int ELECTRIC_DAMAGE_MIN = 3;
        public const int POISON_DAMAGE_MAX = 1;

        public const int POISON_DAMAGE_MIN = 1;

        public const int ONE_IN_OF_BROKEN_KEY = 5;

        public static bool IsJimmySuccessful(int nDexterity) => !Utils.OneInXOdds(ONE_IN_OF_BROKEN_KEY);

        /// <summary>
        ///     When the given user tries to open the chest - does it explode?
        /// </summary>
        /// <param name="record"></param>
        /// <param name="trapComplexity"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool DoesChestTrapTrigger(PlayerCharacterRecord record,
            NonAttackingUnit.TrapComplexity trapComplexity)
        {
            return trapComplexity switch
            {
                NonAttackingUnit.TrapComplexity.Simple => Utils.RandomOdds(Math.Max(
                    ODDS_BASE_SIMPLE_TRAP_EXPLODE_ON_SEARCH - record.Stats.Dexterity * ODDS_DEX_ADJUST_TRAP_EXPLODE,
                    0)),
                NonAttackingUnit.TrapComplexity.Complex => Utils.RandomOdds(Math.Max(
                    ODDS_BASE_COMPLEX_TRAP_EXPLODE_ON_SEARCH - record.Stats.Dexterity * ODDS_DEX_ADJUST_TRAP_EXPLODE,
                    0)),
                _ => throw new ArgumentOutOfRangeException(nameof(trapComplexity), trapComplexity, null)
            };
        }

        /// <summary>
        ///     Generates the appropriate drop and inner contents of that drop based on enemy type
        /// </summary>
        /// <param name="enemyReference"></param>
        /// <param name="dropType"></param>
        /// <param name="mapUnitPosition"></param>
        /// <returns></returns>
        public static NonAttackingUnit GenerateDropForDeadEnemy(EnemyReference enemyReference,
            TileReference.SpriteIndex dropType, SmallMapReferences.SingleMapReference.Location location,
            MapUnitPosition mapUnitPosition) =>
            // todo: need to tailor what is in the drop based on the enemy reference
            NonAttackingUnitFactory.Create((int)dropType, location, mapUnitPosition);

        /// <summary>
        ///     After killing an enemy, do you get a drop?
        /// </summary>
        /// <param name="enemyReference"></param>
        /// <returns></returns>
        public static TileReference.SpriteIndex GetIsDropAfterKillingEnemy(EnemyReference enemyReference)
        {
            if (enemyReference.IsEnemyAbility(EnemyReference.EnemyAbility.NoCorpse)
                || enemyReference.IsWaterEnemy
                || enemyReference.IsEnemyAbility(EnemyReference.EnemyAbility.DisappearsOnDeath))
                return TileReference.SpriteIndex.Nothing;

            // todo: this is just a temporary method until I have some better drop logic based on the actual enemy
            TileReference.SpriteIndex dropSprite =
                GenericDropAfterKillingEnemyList[Utils.Ran.Next() % GenericDropAfterKillingEnemyList.Count];
            return dropSprite;
        }

        /// <summary>
        ///     When creating a new chest - is it locked?
        /// </summary>
        /// <returns></returns>
        public static bool GetIsNewChestLocked() => Utils.RandomOdds(ODDS_CHEST_LOCKED);

        /// <summary>
        ///     Is there treasure in the new dead blood spatter?
        /// </summary>
        /// <returns></returns>
        public static bool GetIsTreasureBloodSpatter() => Utils.RandomOdds(ODDS_TREASURE_FROM_BLOOD_SPATTER);

        /// <summary> is there treasure in the new dead body? </summary>
        /// <returns></returns>
        /// <remarks>UNVERIFIED</remarks>
        public static bool GetIsTreasureInDeadBody() => Utils.RandomOdds(ODDS_TREASURE_FROM_SEARCHING_BODY);

        /// <summary>
        ///     Gets the trap type for a new blood spatter - this includes the NONE type indicating no trap
        /// </summary>
        /// <returns></returns>
        public static NonAttackingUnit.TrapType GetNewBloodSpatterTrapType() =>
            BloodSpatterTrapsWeightedList[Utils.Ran.Next() % BloodSpatterTrapsWeightedList.Count];

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <remarks>UNVERIFIED</remarks>
        public static NonAttackingUnit.TrapComplexity GetNewChestTrappedComplexity()
        {
            if (Utils.RandomOdds(ODDS_SIMPLE_TRAP_ON_CHEST)) return NonAttackingUnit.TrapComplexity.Simple;
            return Utils.RandomOdds(ODDS_COMPLEX_TRAP_ON_CHEST)
                ? NonAttackingUnit.TrapComplexity.Complex
                : NonAttackingUnit.TrapComplexity.Simple;
        }

        /// <summary>
        ///     Gets the trap type for a new chest - this includes the NONE type indicating no trap
        /// </summary>
        /// <returns></returns>
        public static NonAttackingUnit.TrapType GetNewChestTrapType() =>
            ChestTrapsWeightedList[Utils.Ran.Next() % ChestTrapsWeightedList.Count];

        /// <summary>
        ///     Gets the trap type for a new dead body - this includes the NONE type indicating no trap
        /// </summary>
        /// <returns></returns>
        public static NonAttackingUnit.TrapType GetNewDeadBodyTrapType() =>
            DeadBodyTrapsWeightedList[Utils.Ran.Next() % DeadBodyTrapsWeightedList.Count];
    }
}

// bool getChestTrapHandler(int player) {
//     TileEffect trapType;
//     int randNum = xu4_random(4);
//
//     /* Do we use u4dos's way of trap-determination, or the original intended way? */
//     int passTest = (xu4.settings->enhancements && xu4.settings->enhancementsOptions.c64chestTraps) ?
//         (xu4_random(2) == 0) : /* xu4-enhanced */
//         ((randNum & 1) == 0); /* u4dos original way (only allows even numbers through, so only acid and poison show) */
//
//     /* Chest is trapped! 50/50 chance */
//     if (passTest)
//     {
//         /* Figure out which trap the chest has */
//         switch(randNum & xu4_random(4)) {
//         case 0: trapType = EFFECT_FIRE; break;   /* acid trap (56% chance - 9/16) */
//         case 1: trapType = EFFECT_SLEEP; break;  /* sleep trap (19% chance - 3/16) */
//         case 2: trapType = EFFECT_POISON; break; /* poison trap (19% chance - 3/16) */
//         case 3: trapType = EFFECT_LAVA; break;   /* bomb trap (6% chance - 1/16) */
//         default: trapType = EFFECT_FIRE; break;
//         }
//
//         /* apply the effects from the trap */
//         if (trapType == EFFECT_FIRE)
//             screenMessage("%cAcid%c Trap!\n", FG_RED, FG_WHITE);
//         else if (trapType == EFFECT_POISON)
//             screenMessage("%cPoison%c Trap!\n", FG_GREEN, FG_WHITE);
//         else if (trapType == EFFECT_SLEEP)
//             screenMessage("%cSleep%c Trap!\n", FG_PURPLE, FG_WHITE);
//         else if (trapType == EFFECT_LAVA)
//             screenMessage("%cBomb%c Trap!\n", FG_RED, FG_WHITE);
//
//         // player is < 0 during the 'O'pen spell (immune to traps)
//         //
//         // if the chest was opened by a PC, see if the trap was
//         // evaded by testing the PC's dex
//         //
//         if ((player >= 0) &&
//             (c->saveGame->players[player].dex + 25 < xu4_random(100)))
//         {
//             Map* map = c->location->map;
//
//             // Play sound for acid & bomb since applyEffect does not.
//             if (trapType == EFFECT_LAVA || trapType == EFFECT_FIRE)
//                 soundPlay(SOUND_POISON_EFFECT);
//
//             if (trapType == EFFECT_LAVA) /* bomb trap */
//                 c->party->applyEffect(map, trapType);
//             else
//                 c->party->member(player)->applyEffect(map, trapType);
//         } else {
//             soundPlay(SOUND_EVADE);
//             screenMessage("Evaded!\n");
//         }
//
//         return true;
//     }
//
//     return false;
// }