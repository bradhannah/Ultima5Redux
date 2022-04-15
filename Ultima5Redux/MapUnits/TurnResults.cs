using System.Collections.Generic;

namespace Ultima5Redux.MapUnits
{
    public class TurnResults
    {
        private readonly Queue<TurnResult> _turnResults = new();

        public enum TurnResult
        {
            DamageOverTimePoisoned, DamageOverTimeBurning, ActionXitWhat, ActionXitSuccess, ActionYellSailsHoisted,
            ActionYellSailsFurl, ActionMovedCombatPlayerOnCombatMap, ActionMoveBlocked, OfferToExitScreen,
            ActionMoveUsedStairs, ActionMoveFell, ActionMoveShipBreakingUp, ActionMoveShipDestroyed,
            ActionMoveRoughSeas, MovedSelectionCursor, IgnoredMovement, Ignore, CombatMapLoaded, PassTurn, Opened,
            ActionMoveChangeFrigateDirection, ActionMoveFrigateSailsIgnoreMovement, ActionMoveRegular, ActionMoveCarpet,
            ActionMoveHorse, ActionMoveFrigateRowing, ActionMoveFrigateWindSails,
            ActionMoveFrigateWindSailsChangeDirection, ActionMoveSkiffRowing, ActionKlimbWithWhat,
            ActionKlimbRequiresDirection, ActionKlimbDown, ActionKlimbUp, ActionKlimbDirectionMovedFell,
            ActionKlimbDirectionImpassable, ActionKlimbDirectionUnKlimable, ActionKlimbWhat,
            ActionKlimbDirectionSuccess, ActionFireCannon, ActionFireWhat, ActionFireNotHere, ActionFireBroadsideOnly,
            ActionFireEnemyKilled, ActionFireHitNothing, ActionBoardCarpet, ActionBoardHorse, ActionBoardFrigate,
            ActionBoardSkiff, ActionBoardWhat, ActionBoardNoOnFoot, ActionEnterDungeon, ActionEnterTowne,
            ActionEnterWhat, ActionIgniteTorch, ActionIgniteTorchNoTorch, ActionLook, ActionPush, ActionPushWontBudge,
            ActionOpened, ActionOpenedTrapped, ActionOpenedLocked, ActionTalk, ActionAttackBrokeMirror,
            ActionAttackNothingToAttack, ActionAttackOnlyOnFoot, ActionAttackMurder, ActionAttackCombatMapNpc,
            ActionAttackCombatMapEnemy, ActionGetBorrowed, ActionGetMagicCarpet, ActionGetExposedItem,
            ActionGetNothingToGet, ActionJimmyNoLock, ActionJimmyUnlocked, ActionJimmyKeyBroke
        }

        public static bool IsNewMapTurnResult(TurnResult turnResult)
        {
            switch (turnResult)
            {
                case TurnResult.ActionKlimbDown:
                case TurnResult.ActionKlimbUp:
                case TurnResult.ActionEnterDungeon:
                case TurnResult.ActionEnterTowne:
                    return true;
            }

            return false;
        }

        public static bool IsSuccessfulMovement(TurnResult turnResult)
        {
            switch (turnResult)
            {
                case TurnResult.ActionMoveRegular:
                case TurnResult.ActionMoveCarpet:
                case TurnResult.ActionMoveHorse:
                case TurnResult.ActionMoveFrigateRowing:
                case TurnResult.ActionMoveFrigateWindSails:
                case TurnResult.ActionMoveFrigateWindSailsChangeDirection:
                case TurnResult.ActionMoveSkiffRowing:
                case TurnResult.ActionKlimbDirectionMovedFell:
                case TurnResult.ActionKlimbDirectionSuccess:
                    return true;
            }

            return false;
        }

        public void PushTurnResult(TurnResult turnResult)
        {
            PeekLastTurnResult = turnResult;
            _turnResults.Enqueue(turnResult);
        }

        public TurnResult PopTurnResult() => _turnResults.Dequeue();

        public bool HasTurnResult => _turnResults.Count > 0;
        public TurnResult PeekTurnResult => _turnResults.Peek();
        public TurnResult PeekLastTurnResult { get; private set; } = TurnResult.Ignore;
    }
}