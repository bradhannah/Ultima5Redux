using System;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class Whirlpool : NonAttackingUnit
    {
        public override bool ExposeInnerItemsOnOpen => false;
        public override bool ExposeInnerItemsOnSearch => false;

        public override string FriendlyName => "Whirlpool";

        public override bool IsActive => true;
        public override bool IsOpenable => false;
        public override bool IsSearchable => false;

        public override TileReference KeyTileReference
        {
            get => GameReferences.Instance.SpriteTileReferences.GetTileReference(492);
            set => throw new NotImplementedException();
        }

        public override string Name => FriendlyName;
        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;

        public Whirlpool(MapUnitPosition mapUnitPosition) => MapUnitPosition = mapUnitPosition;

        public override bool DoesTriggerTrap(PlayerCharacterRecord record) => false;
    }
}