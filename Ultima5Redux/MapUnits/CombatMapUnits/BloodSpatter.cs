﻿using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    [DataContract]
    public sealed class BloodSpatter : NonAttackingUnit
    {
        public override bool CanStackMapUnitsOnTop => true;

        public override bool ExposeInnerItemsOnOpen => false;

        public override bool ExposeInnerItemsOnSearch => true;

        //GUTS_BANG_N, A_BLOOD_PULP_BANG_N,
        public override string FriendlyName => GameReferences.Instance.DataOvlRef.StringReferences
            .GetString(DataOvlReference.ThingsIFindStrings.GUTS_BANG_N).TrimEnd();

        public override bool HasInnerItemStack => InnerItemStack is { HasStackableItems: true };

        public override bool IsOpenable => false;
        public override bool IsSearchable => true;

        public override TileReference KeyTileReference =>
            GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("Splat");

        public override string Name => FriendlyName;
        public override bool NonAttackUnitTypeCanBeTrapped => true;

        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;


        [JsonConstructor]
        public BloodSpatter()
        {
        }

        public BloodSpatter(MapUnitPosition mapUnitPosition)
        {
            Trap = OddsAndLogic.GetNewBloodSpatterTrapType();
            if (OddsAndLogic.GetIsTreasureBloodSpatter())
            {
                GenerateItemStack(mapUnitPosition);
            }

            MapUnitPosition = mapUnitPosition;
        }

        [OnDeserialized]
        private void PostDeserialize(StreamingContext context)
        {
            GenerateItemStack(MapUnitPosition);
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

            InnerItemStack.PushStackableItem(NonAttackingUnitFactory.CreateStackableItem(258));
            InnerItemStack.PushStackableItem(NonAttackingUnitFactory.CreateStackableItem(271));
            InnerItemStack.PushStackableItem(NonAttackingUnitFactory.CreateStackableItem(269));
        }

        public override bool DoesTriggerTrap(PlayerCharacterRecord record) =>
            (IsTrapped && OddsAndLogic.DoesChestTrapTrigger(record, TrapComplexity.Simple)) ||
            OddsAndLogic.AGGRESSIVE_TRAPS;
    }
}