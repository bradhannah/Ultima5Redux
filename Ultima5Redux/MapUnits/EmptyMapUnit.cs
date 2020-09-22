using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    public class EmptyMapUnit : MapUnit
    {
        public override TileReference GetTileReferenceWithAvatarOnTile(VirtualMap.Direction direction)
        {
            throw new System.NotImplementedException();
        }

        public override string BoardXitName => "EMPTY";

        public override bool IsActive => false;

        public EmptyMapUnit() : base()
        {
            
        }
    }
}