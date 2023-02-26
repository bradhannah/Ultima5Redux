using Ultima5Redux.MapUnits;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.Maps
{
    public partial class VirtualMap
    {
        public class AggressiveMapUnitInfo
        {
            public enum DecidedAction
            {
                Unset = -1,
                MoveUnit = 0,
                RangedAttack,
                MeleeOverworldAttack,
                Stay,
                EnemyAttackCombatMap,
                AttemptToArrest,
                WantsToChat,
                Begging,
                HalfYourGoldExtortion,
                GenericGuardExtortion,
                StraightToBlackthornDungeon,
                BlackthornGuardPasswordCheck
            }

            private bool _bForceDecidedAction;

            private DecidedAction _decidedAction = DecidedAction.Unset;
            private DecidedAction _forcedDecidedAction;
            public MapUnit AttackingMapUnit { get; }
            public CombatItemReference.MissileType AttackingMissileType { get; internal set; }
            public SingleCombatMapReference CombatMapReference { get; internal set; }

            public AggressiveMapUnitInfo(MapUnit attackingMapUnit,
                CombatItemReference.MissileType attackingMissileType = CombatItemReference.MissileType.None,
                SingleCombatMapReference combatMapReference = null)
            {
                AttackingMapUnit = attackingMapUnit;
                AttackingMissileType = attackingMissileType;
                CombatMapReference = combatMapReference;
            }

            public void ForceDecidedAction(DecidedAction decidedAction)
            {
                _forcedDecidedAction = decidedAction;
                _bForceDecidedAction = true;
            }

            public DecidedAction GetDecidedAction()
            {
                // we are able to override it in case some outside logic made the decision
                if (_bForceDecidedAction) return _forcedDecidedAction;

                if (_decidedAction != DecidedAction.Unset) return _decidedAction;
                // if they have a combat map - then they are next to them and could go into combat
                // if they have a missile type then they are within range and will attack with that
                // if they have a Arrow missile type, then they will attack them melee in the overworld
                if (CombatMapReference != null)
                    _decidedAction = DecidedAction.EnemyAttackCombatMap;
                else if (AttackingMissileType == CombatItemReference.MissileType.Arrow)
                    _decidedAction = DecidedAction.MeleeOverworldAttack;
                else if (AttackingMissileType != CombatItemReference.MissileType.None)
                    // we will not ALWAYS range attack, sometimes they will try to get closer to the avatar
                    _decidedAction = Utils.OneInXOdds(2) ? DecidedAction.RangedAttack : DecidedAction.MoveUnit;
                else
                    _decidedAction = DecidedAction.MoveUnit;

                return _decidedAction;
            }
        }
    }
}