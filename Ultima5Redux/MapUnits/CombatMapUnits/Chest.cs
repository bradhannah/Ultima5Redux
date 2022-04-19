using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public sealed class Chest : NonAttackingUnit
    {
        public override string FriendlyName => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.Vision2Strings.A_WOODEN_CHEST_DOT_N).TrimEnd();

        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;
        public override string Name => FriendlyName;

        public override TileReference KeyTileReference =>
            GameReferences.SpriteTileReferences.GetTileReferenceByName("Chest");

        private Chest()
        {
        }

        private void GenerateItemStack(MapUnitPosition mapUnitPosition)
        {
            InnerItemStack = new ItemStack(mapUnitPosition);
            // 258,ItemMoney,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 259,ItemPotion,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 260,ItemScroll,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 261,ItemWeapon,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 262,ItemShield,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 263,ItemKey,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 264,ItemGem,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 265,ItemHelm,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 266,ItemRing,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 267,ItemArmour,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 268,ItemAnkh,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None
            // 269,ItemTorch,,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,1,FALSE,0,TRUE,-3,Item,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,FALSE,None

            for (int i = 258; i <= 269; i++)
                InnerItemStack.PushStackableItem(NonAttackingUnitFactory.CreateStackableItem(i));

            InnerItemStack.PushStackableItem(NonAttackingUnitFactory.CreateStackableItem(271));
        }

        /// <summary>
        ///     Special constructor that may have special instructions based on where it was created on the map
        /// </summary>
        /// <param name="mapUnitPosition"></param>
        public Chest(MapUnitPosition mapUnitPosition)
        {
            if (mapUnitPosition != null) MapUnitPosition = mapUnitPosition;
            CurrentTrapComplexity = OddsAndLogic.GetNewChestTrappedComplexity();
            IsLocked = OddsAndLogic.GetIsNewChestLocked();
            Trap = OddsAndLogic.GetNewChestTrapType();
            GenerateItemStack(mapUnitPosition);
        }

        public Chest(TrapComplexity trapComplexity, bool bIsLocked, TrapType trap, MapUnitPosition mapUnitPosition)
        {
            CurrentTrapComplexity = trapComplexity;
            Trap = trap;
            IsLocked = bIsLocked;
            GenerateItemStack(mapUnitPosition);
        }

        public Chest(bool bRandomTrapComplexity, bool bRandomLocked, bool bRandomTrapType,
            MapUnitPosition mapUnitPosition)
        {
            if (bRandomTrapComplexity) CurrentTrapComplexity = OddsAndLogic.GetNewChestTrappedComplexity();
            if (bRandomLocked) IsLocked = OddsAndLogic.GetIsNewChestLocked();
            if (bRandomTrapType) Trap = OddsAndLogic.GetNewChestTrapType();
            GenerateItemStack(mapUnitPosition);
        }

        public override bool IsOpenable => true;
        public override bool IsSearchable => true;
        public override bool ExposeInnerItemsOnSearch => false;
        public override bool ExposeInnerItemsOnOpen => true;

        public override bool DoesTriggerTrap(PlayerCharacterRecord record)
        {
            return IsTrapped && OddsAndLogic.DoesChestTrapTrigger(record, CurrentTrapComplexity);
        }
    }
}