namespace Ultima5Redux.MapUnits.TurnResults
{
    public abstract class TurnResult
    {
        public enum TurnResultType
        {
            ActionGetMoonstone, OverworldAvatarAttacking, EnemyAttacksOverworldOrSmallMap, DamageOverTimePoisoned,
            DamageOverTimeBurning, DamageFromAcid, ActionXitWhat, ActionXitSuccess, ActionYellSailsHoisted,
            ActionYellSailsFurl, ActionMovedCombatPlayerOnCombatMap, ActionMoveBlocked, OfferToExitScreen,
            ActionMoveUsedStairs, ActionMoveFell, ActionMoveShipBreakingUp, ActionMoveShipDestroyed,
            ActionMoveRoughSeas, MovedSelectionCursor, IgnoredMovement, Ignore, CombatMapLoaded, PassTurn,
            ActionOpenDoorOpened, ActionOpenDoorLocked, ActionOpenDoorNothingToOpen, ActionMoveChangeFrigateDirection,
            ActionMoveFrigateSailsIgnoreMovement, ActionMoveRegular, ActionMoveCarpet, ActionMoveHorse,
            ActionMoveFrigateRowing, ActionMoveFrigateWindSails, ActionMoveFrigateWindSailsChangeDirection,
            ActionMoveSkiffRowing, ActionKlimbWithWhat, ActionKlimbRequiresDirection, ActionKlimbDown, ActionKlimbUp,
            ActionKlimbDirectionMovedFell, ActionKlimbDirectionImpassable, ActionKlimbDirectionUnKlimable,
            ActionKlimbWhat, ActionKlimbDirectionSuccess, ActionFireCannon, ActionFireWhat, ActionFireNotHere,
            ActionFireBroadsideOnly, ActionFireEnemyKilled, ActionFireHitNothing, ActionBoardCarpet, ActionBoardHorse,
            ActionBoardFrigate, ActionBoardSkiff, ActionBoardWhat, ActionBoardNoOnFoot, ActionEnterDungeon,
            ActionEnterTowne, ActionEnterWhat, ActionIgniteTorch, ActionIgniteTorchNoTorch, ActionLook, ActionPush,
            ActionPushWontBudge, ActionOpened, ActionOpenedTrapped, ActionOpenedLocked, ActionTalk,
            ActionAttackBrokeMirror, ActionAttackNothingToAttack, ActionAttackOnlyOnFoot, ActionAttackMurder,
            ActionAttackCombatMapNpc, ActionAttackCombatMapEnemy, ActionGetBorrowed, ActionGetMagicCarpet,
            ActionGetExposedItem, ActionGetStackableItem, ActionGetNothingToGet, ActionJimmyNoLock, ActionJimmyUnlocked,
            ActionJimmyKeyBroke, ActionSearchRemoveComplex, ActionSearchRemoveSimple, ActionSearchTriggerComplexTrap,
            ActionSearchTriggerSimpleTrap, ActionSearchNoTrap, ActionSearchThingDisappears, PlayerCharacterPoisoned,
            Combat_EnemyEscaped, OutputToConsole, Combat_EnemyToAttackRequiresInput, Combat_EnemyIsSleeping,
            Combat_EnemyIsFleeing, Combat_EnemyWantsToFleeButNoPath, Combat_EnemyMissedTarget,
            Combat_Result_EnemyGrazedTarget, Combat_Result_CombatPlayerGrazedTarget,
            Combat_Result_CombatPlayerMissedRangedAttack, Combat_Result_EnemyMissedRangedAttack,
            Combat_Result_EnemyMissedButHit, Combat_Result_CombatPlayerMissedButHit,
            Combat_CombatPlayerTriedToAttackSelf, Combat_CombatPlayerTriedToAttackNothing,
            Combat_CombatPlayerRangedAttackBlocked, Combat_EnemyMoved, Combat_CombatPlayerMoved,
            Combat_EnemyBeginsAttack, Combat_CombatPlayerBeginsAttack, Combat_Result_CombatPlayerReceivedDamage,
            Combat_LootDropped, Combat_Result_HitAndEnemyReceivedDamage, Combat_Result_CombatPlayerKilled,
            Combat_Result_EnemyKilled, Combat_Result_Missed_CombatPlayerMelee, Combat_Result_Missed_EnemyMelee,
            ActionBlockedRanIntoCactus, NPCAttemptingToArrest, ActionUseDrankPotion, ActionUseReadScroll,
            ActionShoppeKeeperInteraction, NoOneToTalkTo, CantTalkSleeping, ComeBackLater, NotTalkative,
            NpcTalkInteraction, DontHurtMeAfraid, AdvanceClockNoComputation
        }

        public bool IsSuccessfulMovement
        {
            get
            {
                switch (TheTurnResultType)
                {
                    case TurnResultType.ActionMoveRegular:
                    case TurnResultType.ActionMoveCarpet:
                    case TurnResultType.ActionMoveHorse:
                    case TurnResultType.ActionMoveFrigateRowing:
                    case TurnResultType.ActionMoveFrigateWindSails:
                    case TurnResultType.ActionMoveFrigateWindSailsChangeDirection:
                    case TurnResultType.ActionMoveSkiffRowing:
                    case TurnResultType.ActionKlimbDirectionMovedFell:
                    case TurnResultType.ActionKlimbDirectionSuccess:
                        return true;
                }

                return false;
            }
        }

        public TurnResultType TheTurnResultType { get; set; }

        public TurnResult(TurnResultType theTurnResultType)
        {
            TheTurnResultType = theTurnResultType;
        }

        public virtual string GetDebugString()
        {
            return $@"IsSuccessfulMovement: {IsSuccessfulMovement}
IsNewMapTurnResult: {IsNewMapTurnResult()}";
        }
        // Combat_Category_Action_Who_Details

        public bool IsNewMapTurnResult()
        {
            switch (TheTurnResultType)
            {
                case TurnResultType.ActionKlimbDown:
                case TurnResultType.ActionKlimbUp:
                case TurnResultType.ActionEnterDungeon:
                case TurnResultType.ActionEnterTowne:
                    return true;
            }

            return false;
        }
    }
}