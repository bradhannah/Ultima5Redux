using Ultima5Redux.MapUnits.CombatMapUnits;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class LootDropped : TurnResult, ILoot
    {
        public LootDropped(NonAttackingUnit loot) : base(TurnResultType.Combat_LootDropped)
        {
            Loot = loot;
        }

        public NonAttackingUnit Loot { get; }
    }
}