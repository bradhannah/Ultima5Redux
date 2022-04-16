using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class Chest : NonAttackingUnit
    {
        public override string FriendlyName => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.Vision2Strings.A_WOODEN_CHEST_DOT_N).TrimEnd();

        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;
        public override string Name => FriendlyName;

        public override TileReference KeyTileReference =>
            GameReferences.SpriteTileReferences.GetTileReferenceByName("Chest");

        public enum ChestTrapType { NONE, ACID, SLEEP, POISON, BOMB }

        public ItemStack InnerItemStack { get; }

        public bool IsTrapped => CurrentChestTrapComplexity == ChestTrapComplexity.None;
        public bool IsIsLocked { get; set; }

        public ChestTrapComplexity CurrentChestTrapComplexity { get; set; } = ChestTrapComplexity.None;
        public ChestTrapType CurrentChestTrapType { get; set; } = ChestTrapType.NONE;

        public enum ChestTrapComplexity { None, Simple, Complex }

        private Chest()
        {
        }

        /// <summary>
        ///     Special constructor that may have special instructions based on where it was created on the map
        /// </summary>
        /// <param name="mapUnitPosition"></param>
        public Chest(MapUnitPosition mapUnitPosition)
        {
            CurrentChestTrapComplexity = OddsAndLogic.GetNewChestTrappedComplexity();
            IsIsLocked = OddsAndLogic.GetNewChestLocked();
            CurrentChestTrapType = OddsAndLogic.GetNewChestTrapType();
            InnerItemStack = new ItemStack();
        }

        public Chest(ChestTrapComplexity trapComplexity, bool bIsLocked, ChestTrapType currentChestTrapType)
        {
            CurrentChestTrapComplexity = trapComplexity;
            CurrentChestTrapType = currentChestTrapType;
            IsIsLocked = bIsLocked;
            InnerItemStack = new ItemStack();
        }

        public Chest(bool bRandomTrapComplexity, bool bRandomLocked, bool bRandomTrapType)
        {
            if (bRandomTrapComplexity) CurrentChestTrapComplexity = OddsAndLogic.GetNewChestTrappedComplexity();
            if (bRandomLocked) IsIsLocked = OddsAndLogic.GetNewChestLocked();
            if (bRandomTrapType) CurrentChestTrapType = OddsAndLogic.GetNewChestTrapType();
            InnerItemStack = new ItemStack();
        }
    }
}