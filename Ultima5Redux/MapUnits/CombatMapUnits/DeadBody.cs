using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public sealed class DeadBody : NonAttackingUnit
    {
        //GUTS_BANG_N, A_BLOOD_PULP_BANG_N,
        public override string FriendlyName => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.ThingsIFindStrings.A_BLOOD_PULP_BANG_N).TrimEnd();

        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;
        public override string Name => FriendlyName;

        public override TileReference KeyTileReference =>
            GameReferences.SpriteTileReferences.GetTileReferenceByName("DeadBody");
        //public  bool HasInnerItemStack => InnerItemStack is { AreStackableItems: true }; 

        // public ItemStack InnerItemStack { get; } = new();
        public TrapType Trap { get; set; }
        public override ItemStack InnerItemStack { get; protected set; }

        public override bool DoesTriggerTrap(PlayerCharacterRecord record) =>
            IsTrapped && OddsAndLogic.DoesChestTrapTrigger(record, TrapComplexity.Simple);

        public DeadBody()
        {
            Trap = OddsAndLogic.GetNewDeadBodyTrapType();
            if (OddsAndLogic.GetIsTreasureBloodSpatter())
            {
                InnerItemStack = new();
            }
        }
    }
}