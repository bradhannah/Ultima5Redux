using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    public class EmptyMapUnit : MapUnit
    {
        public override bool IsActive => false;

        public EmptyMapUnit() : base()
        {
            
        }
    }
}