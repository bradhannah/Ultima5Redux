using System;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class ElementalField : NonAttackingUnit
    {
        public ElementalField(FieldType theFieldType, MapUnitPosition mapUnitPosition)
        {
            TheFieldType = theFieldType;
            MapUnitPosition = mapUnitPosition;
        }

        public enum FieldType { Poison = 488, Sleep = 489, Fire = 490, Electric = 491 }

        public override bool IsActive => true;

        public FieldType TheFieldType { get; }

        public override string FriendlyName => TheFieldType + " field";
        public override string PluralName => TheFieldType + " fields";
        public override string SingularName => FriendlyName;
        public override string Name => TheFieldType.ToString();
        public override bool IsOpenable => false;
        public override bool IsSearchable => false;
        public override bool ExposeInnerItemsOnSearch => false;
        public override bool ExposeInnerItemsOnOpen => false;

        public override TrapType Trap
        {
            get
            {
                return TheFieldType switch
                {
                    FieldType.Poison => TrapType.POISON_ALL,
                    FieldType.Sleep => TrapType.SLEEP_ALL,
                    FieldType.Fire => TrapType.BOMB,
                    FieldType.Electric => TrapType.ELECTRIC_ALL,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            set => throw new NotImplementedException("Can't set the trap type Elemental Field");
        }

        public override TileReference KeyTileReference
        {
            get => GameReferences.SpriteTileReferences.GetTileReference((int)TheFieldType);
            set => throw new NotImplementedException("Can't set the key sprite in Elemental Field");
        }

        public override bool DoesTriggerTrap(PlayerCharacterRecord record) => true;
    }
}