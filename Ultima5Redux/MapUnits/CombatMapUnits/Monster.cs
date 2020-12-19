using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.Monsters
{
    public class Monster : CombatMapUnit
    {
        public Monster()
        {
            
        }
        
        public Monster(MapUnitState mapUnitState,
            MapUnitMovement mapUnitMovement,
            PlayerCharacterRecords playerCharacterRecords, TileReferences tileReferences,
            SmallMapReferences.SingleMapReference.Location location, DataOvlReference dataOvlReference) : base()
        {
        }

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; }

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; }

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        public override string BoardXitName => "Hostile creates don't not like to be boarded!";
        public override TileReference NonBoardedTileReference => TheMapUnitState.Tile1Ref;
        public override bool IsActive => true;
    }
}