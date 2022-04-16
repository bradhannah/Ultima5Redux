using System;
using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux
{
    public static class OddsAndLogic
    {
        private const float ODDS_POISON_FROM_SEARCHING_BODY = 0.5f;
        private const float ODDS_TREASURE_FROM_SEARCHING_BODY = 0.2f;

        private const float ODDS_POISON_FROM_SEARCHING_BLOOD_SPATTER = 0.5f;
        private const float ODDS_TREASURE_FROM_BLOOD_SPATTER = 0.2f;

        private const float ODDS_CHEST_LOCKED = 0.2f;
        private const float ODDS_COMPLEX_TRAP_ON_CHEST = 0.2f;
        private const float ODDS_SIMPLE_TRAP_ON_CHEST = 0.2f;
        private const float ODDS_BASE_COMPLEX_TRAP_EXPLODE_ON_SEARCH = 0.4f;
        private const float ODDS_BASE_SIMPLE_TRAP_EXPLODE_ON_SEARCH = 0.2f;

        private const int WEIGHT_CHEST_TRAP_NONE = WEIGHT_CHEST_TRAP_ACID + WEIGHT_CHEST_TRAP_SLEEP +
                                                   WEIGHT_CHEST_TRAP_POISON + WEIGHT_CHEST_TRAP_BOMB;

        private const int WEIGHT_CHEST_TRAP_ACID = 9;
        private const int WEIGHT_CHEST_TRAP_SLEEP = 3;
        private const int WEIGHT_CHEST_TRAP_POISON = 3;
        private const int WEIGHT_CHEST_TRAP_BOMB = 1;

        private static readonly Dictionary<Chest.ChestTrapType, int> ChestTrapsWeighted = new()
        {
            { Chest.ChestTrapType.ACID, WEIGHT_CHEST_TRAP_ACID },
            { Chest.ChestTrapType.BOMB, WEIGHT_CHEST_TRAP_BOMB },
            { Chest.ChestTrapType.POISON, WEIGHT_CHEST_TRAP_POISON },
            { Chest.ChestTrapType.SLEEP, WEIGHT_CHEST_TRAP_SLEEP },
            { Chest.ChestTrapType.NONE, WEIGHT_CHEST_TRAP_NONE }
        };

        private static readonly List<Chest.ChestTrapType> ChestTrapsWeightedList =
            Utils.MakeWeightedList(ChestTrapsWeighted).ToList();

        /// <summary>
        ///     Improvement to odds of not setting off a trap based on a characters dexterity
        /// </summary>
        private const float ODDS_DEX_ADJUST_TRAP_EXPLODE = 0.01f;

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <remarks>UNVERIFIED</remarks>
        public static bool GetIsPoisonFromSearchingBody() => Utils.RandomOdds(ODDS_POISON_FROM_SEARCHING_BODY);

        public static bool GetIsTreasureInDeadBody() => Utils.RandomOdds(ODDS_TREASURE_FROM_SEARCHING_BODY);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <remarks>UNVERIFIED</remarks>
        public static bool GetIsPoisonFromSearchingBloodSpatter() =>
            Utils.RandomOdds(ODDS_POISON_FROM_SEARCHING_BLOOD_SPATTER);

        public static bool GetIsTreasureBloodSpatter() => Utils.RandomOdds(ODDS_TREASURE_FROM_SEARCHING_BODY);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <remarks>UNVERIFIED</remarks>
        public static Chest.ChestTrapComplexity GetNewChestTrappedComplexity()
        {
            if (Utils.RandomOdds(ODDS_SIMPLE_TRAP_ON_CHEST)) return Chest.ChestTrapComplexity.Simple;
            return Utils.RandomOdds(ODDS_COMPLEX_TRAP_ON_CHEST)
                ? Chest.ChestTrapComplexity.Complex
                : Chest.ChestTrapComplexity.Simple;
        }

        public static bool GetNewChestLocked() => Utils.RandomOdds(ODDS_CHEST_LOCKED);

        /// <summary>
        ///     Gets the trap type for a new chest - this includes the NONE type indicating no trap
        /// </summary>
        /// <returns></returns>
        public static Chest.ChestTrapType GetNewChestTrapType() =>
            (ChestTrapsWeightedList[Utils.Ran.Next() % ChestTrapsWeighted.Count]);

        public static bool DoesChestTrapTrigger(PlayerCharacterRecord record,
            Chest.ChestTrapComplexity chestTrapComplexity)
        {
            return chestTrapComplexity switch
            {
                Chest.ChestTrapComplexity.None => false,
                Chest.ChestTrapComplexity.Simple => Utils.RandomOdds(Math.Max(
                    ODDS_BASE_SIMPLE_TRAP_EXPLODE_ON_SEARCH - (record.Stats.Dexterity * ODDS_DEX_ADJUST_TRAP_EXPLODE),
                    0)),
                Chest.ChestTrapComplexity.Complex => Utils.RandomOdds(Math.Max(
                    ODDS_BASE_COMPLEX_TRAP_EXPLODE_ON_SEARCH - (record.Stats.Dexterity * ODDS_DEX_ADJUST_TRAP_EXPLODE),
                    0)),
                _ => throw new ArgumentOutOfRangeException(nameof(chestTrapComplexity), chestTrapComplexity, null)
            };
        }
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