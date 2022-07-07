using System;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class Whirlpool : NonAttackingUnit
    {
        public Whirlpool(MapUnitPosition mapUnitPosition)
        {
            MapUnitPosition = mapUnitPosition;
        }

        public override string FriendlyName => "Whirlpool";
        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;
        public override string Name => FriendlyName;
        public override bool IsOpenable => false;
        public override bool IsSearchable => false;
        public override bool ExposeInnerItemsOnSearch => false;
        public override bool ExposeInnerItemsOnOpen => false;

        public override bool IsActive => true;

        public override bool DoesTriggerTrap(PlayerCharacterRecord record)
        {
            return false;
        }

        public override TileReference KeyTileReference
        {
            get => GameReferences.SpriteTileReferences.GetTileReference(492);
            set => throw new NotImplementedException();
        }

        protected override bool CanMoveToDumb(VirtualMap virtualMap, Point2D mapUnitPosition)
        {
            return base.CanMoveToDumb(virtualMap, mapUnitPosition);
        }
    }
}