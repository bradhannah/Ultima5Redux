using System.Diagnostics.CodeAnalysis;

namespace Ultima5Redux.MapUnits.TurnResults
{
    public abstract class TurnResult
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")] [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public enum TurnResultType
        {
            ActionGetMoonstone, OverworldAvatarAttacking, EnemyAttacksOverworldOrSmallMap, ActionXitWhat,
            ActionXitSuccess, ActionYellSailsHoisted, ActionYellSailsFurl, ActionMovedCombatPlayerOnCombatMap,
            ActionMoveBlocked, OfferToExitScreen, ActionMoveUsedStairs, ActionMoveFell, ActionMoveShipBreakingUp,
            ActionMoveShipDestroyed, ActionMoveRoughSeas, MovedSelectionCursor, IgnoredMovement, Ignore, PassTurn,
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
            Combat_EnemyEscaped, Combat_CombatPlayerEscaped, OutputToConsole, Combat_EnemyToAttackRequiresInput,
            Combat_EnemyIsSleeping, Combat_EnemyIsFleeing, Combat_EnemyWantsToFleeButNoPath, Combat_EnemyMissedTarget,
            Combat_Result_EnemyGrazedTarget, Combat_Result_CombatPlayerGrazedTarget,
            Combat_Result_CombatPlayerMissedRangedAttack, Combat_Result_EnemyMissedRangedAttack,
            Combat_Result_EnemyMissedButHit, Combat_Result_CombatPlayerMissedButHit,
            Combat_CombatPlayerTriedToAttackSelf, Combat_CombatPlayerTriedToAttackNothing,
            Combat_CombatPlayerRangedAttackBlocked, Combat_EnemyMoved, Combat_CombatPlayerMoved,
            Combat_EnemyBeginsAttack, Combat_CombatPlayerBeginsAttack, Combat_Result_CombatPlayerReceivedDamage,
            Combat_LootDropped, Combat_Result_HitAndEnemyReceivedDamage, Combat_Result_CombatPlayerKilled,
            Combat_Result_EnemyKilled, Combat_Result_Missed_CombatPlayerMelee, Combat_Result_Missed_EnemyMelee,
            Combat_CombatPlayerAttackedTrigger, ActionBlockedRanIntoCactus, NPCAttemptingToArrest, ActionUseDrankPotion,
            ActionUseReadScroll, ActionUseHmsCapePlans, ActionShoppeKeeperInteraction, NoOneToTalkTo, CantTalkSleeping,
            ComeBackLater, NotTalkative, NpcTalkInteraction, DontHurtMeAfraid, AdvanceClockNoComputation,
            Combat_CombatPlayerMissedTarget, KarmaChanged, ProvisionQuantityChanged, NpcFreedFromManacles,
            NpcFreedFromStocks, GuardExtortion, NpcJoinedParty, GoToJail, GoToBlackthornDungeon, PoofHorse,
            OpenPortcullis, OverrideCombatMapTile, FoodStolenByEnemy, SnuckPastTrollBridge,
            FailedToSneakPastTrollUnderBridge, FallDownWaterfallVariant_Underworld, FallDownWaterfallVariant_Normal,
            PlayerTakesDamage, TeleportToNewLocation, LoadCombatMap, ExecuteCutScene, Script_PromptVirtueMeditate,
            Script_ExitBuilding, Script_CreateMapUnit, Script_MapUnitMove, Script_Pause, Script_SoundEffect,
            Script_Goto, Script_GotoIf, Script_NoOp, Script_PromptMantra, Script_OutputModalText,
            Script_ChangeShrineState
        }

        [SuppressMessage("ReSharper", "SwitchStatementMissingSomeEnumCasesNoDefault")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public bool IsSuccessfulMovement {
            get {
                switch (TheTurnResultType) {
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

        public TurnResultType TheTurnResultType { get; }

        public enum TurnResulActionType { ActionRequired, ActionAlreadyPerformed, Unsure }

        public TurnResulActionType TheTurnResulActionType { get; }

        protected TurnResult(TurnResultType theTurnResultType, TurnResulActionType theTurnResulActionType) {
            TheTurnResultType = theTurnResultType;
            TheTurnResulActionType = theTurnResulActionType;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public virtual string GetDebugString() =>
            $@"IsSuccessfulMovement: {IsSuccessfulMovement}
IsNewMapTurnResult: {IsNewMapTurnResult()}";
        // Combat_Category_Action_Who_Details

        [SuppressMessage("ReSharper", "SwitchStatementMissingSomeEnumCasesNoDefault")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public bool IsNewMapTurnResult() {
            switch (TheTurnResultType) {
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