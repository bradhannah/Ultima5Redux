using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public sealed class BloodSpatter : NonAttackingUnit
    {
        //GUTS_BANG_N, A_BLOOD_PULP_BANG_N,
        public override string FriendlyName => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.ThingsIFindStrings.GUTS_BANG_N).TrimEnd();

        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;
        public override string Name => FriendlyName;

        public override TileReference KeyTileReference =>
            GameReferences.SpriteTileReferences.GetTileReferenceByName("Splat");

        public override bool IsOpenable => false;
        public override bool IsSearchable => true;
        public override bool ExposeInnerItemsOnSearch => true;
        public override bool ExposeInnerItemsOnOpen => false;
        public override bool HasInnerItemStack => InnerItemStack is { AreStackableItems: true };

        public override bool DoesTriggerTrap(PlayerCharacterRecord record) =>
            IsTrapped && OddsAndLogic.DoesChestTrapTrigger(record, TrapComplexity.Simple);

        public BloodSpatter()
        {
            Trap = OddsAndLogic.GetNewBloodSpatterTrapType();
            if (OddsAndLogic.GetIsTreasureBloodSpatter())
            {
                InnerItemStack = new ItemStack();
            }
        }
    }
}