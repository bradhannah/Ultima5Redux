namespace Ultima5Redux.MapUnits.TurnResults
{
    public class TurnResult
    {
        public TurnResultType TheTurnResultType { get; set; }

        public TurnResult(TurnResultType theTurnResultType)
        {
            TheTurnResultType = theTurnResultType;
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

        public enum TurnResultType
        {
            DamageOverTimePoisoned, DamageOverTimeBurning, DamageFromAcid, ActionXitWhat, ActionXitSuccess,
            ActionYellSailsHoisted, ActionYellSailsFurl, ActionMovedCombatPlayerOnCombatMap, ActionMoveBlocked,
            OfferToExitScreen, ActionMoveUsedStairs, ActionMoveFell, ActionMoveShipBreakingUp, ActionMoveShipDestroyed,
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
            Combat_EnemyIsFleeing, Combat_EnemyWantsToFleeButNoPath, Combat_EnemyMissedTarget, Combat_EnemyGrazedTarget,
            Combat_CombatPlayerGrazedTarget, Combat_MissedRangedAttack, Combat_EnemyMissedButHit,
            Combat_CombatPlayerMissedButHit, Combat_CombatPlayerTriedToAttackSelf,
            Combat_CombatPlayerTriedToAttackNothing, Combat_CombatPlayerRangedAttackBlocked, Combat_EnemyMoved,
            Combat_CombatPlayerMoved, Combat_EnemyAttacks, Combat_CombatPlayerAttacks,
            Combat_CombatPlayerReceivedDamage, Combat_CombatPlayerMissedTarget, Combat_LootDropped,
            Combat_EnemyReceivedDamage, Combat_CombatPlayerKilled, Combat_EnemyKilled
        }

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