using Ultima5Redux.MapUnits.CombatMapUnits;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class LootDropped : TurnResult, ILoot
    {
        public LootDropped(NonAttackingUnit loot) : base(TurnResultType.Combat_LootDropped,
            TurnResulActionType.ActionAlreadyPerformed) => Loot = loot;

        public override string GetDebugString() =>
            $@"LootDropped: {Loot.FriendlyName}
LootPosition: {Loot.MapUnitPosition.XY.GetFriendlyString()}";

        public NonAttackingUnit Loot { get; }
    }
}