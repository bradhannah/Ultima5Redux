using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public abstract class CombatMapUnit : MapUnit
    {
        protected CombatMapUnit()
        {
            
        }
        
        protected CombatMapUnit(NonPlayerCharacterReference npcRef, MapUnitState mapUnitState,
            SmallMapCharacterState smallMapTheSmallMapCharacterState, MapUnitMovement mapUnitMovement,
            PlayerCharacterRecords playerCharacterRecords, TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location, DataOvlReference dataOvlReference) : base(npcRef,
            mapUnitState,
            smallMapTheSmallMapCharacterState, mapUnitMovement, playerCharacterRecords, tileReferences,
            location, dataOvlReference, Point2D.Direction.None)
        {
            
        }
    }
}