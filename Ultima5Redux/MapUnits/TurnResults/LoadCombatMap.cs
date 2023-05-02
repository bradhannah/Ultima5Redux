using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults
{
    public class LoadCombatMap : TurnResult
    {
        public SingleCombatMapReference SingleCombatMapReference { get; }
        public EnemyReference EnemyReference { get; }
        public NonPlayerCharacterReference NpcRef { get; }

        public LoadCombatMap(SingleCombatMapReference singleCombatMapReference,
            NonPlayerCharacterReference npcRef) : base(
            TurnResultType.LoadCombatMap,
            TurnResulActionType.ActionRequired)
        {
            SingleCombatMapReference = singleCombatMapReference;
            NpcRef = npcRef;
        }

        public LoadCombatMap(SingleCombatMapReference singleCombatMapReference, EnemyReference enemyReference) : base(
            TurnResultType.LoadCombatMap, TurnResulActionType.ActionRequired)
        {
            SingleCombatMapReference = singleCombatMapReference;
            EnemyReference = enemyReference;
        }
    }
}