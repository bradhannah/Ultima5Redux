using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class SpellCastingDetails
    {
        public PlayerCharacterRecord Record { get; set; }
        public Point2D SourcePosition { get; set; }
        public Point2D DestinationPosition { get; set; }
        public Point2D.Direction Direction { get; set; }
    }
}