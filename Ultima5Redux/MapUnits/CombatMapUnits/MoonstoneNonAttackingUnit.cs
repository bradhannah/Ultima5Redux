using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public sealed class MoonstoneNonAttackingUnit : NonAttackingUnit
    {
        public readonly Moonstone TheMoonstone;
        public override bool ExposeInnerItemsOnOpen => false;
        public override bool ExposeInnerItemsOnSearch => false;

        public override string FriendlyName => TheMoonstone.LongName;
        public override bool IsActive => true;
        public override bool IsAttackable => false;
        public override bool IsOpenable => false;
        public override bool IsSearchable => false;
        public override TileReference KeyTileReference { get; set; }
        public override string Name => TheMoonstone.Phase.ToString();
        public override string PluralName => TheMoonstone.LongName;
        public override string SingularName => TheMoonstone.LongName;

        public MoonstoneNonAttackingUnit(Moonstone moonstone, MapUnitPosition mapUnitPosition)
        {
            MapUnitPosition = mapUnitPosition;
            TheMoonstone = moonstone;
            KeyTileReference = GameReferences.SpriteTileReferences.GetTileReference(moonstone.InvRef.ItemSpriteExposed);
        }

        public override bool DoesTriggerTrap(PlayerCharacterRecord record)
        {
            return false;
        }
    }
}