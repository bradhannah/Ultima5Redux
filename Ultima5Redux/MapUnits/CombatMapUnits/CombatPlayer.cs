using System.Collections.Generic;
using System.Security.Policy;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    public class CombatPlayer : CombatMapUnit
    {
        private readonly PlayerCharacterRecord _record;

        public CombatPlayer(PlayerCharacterRecord record, TileReferences tileReferences, Point2D xy)
        {
            _record = record;
            TileReferences = tileReferences;
            TheMapUnitState = MapUnitState.CreateCombatPlayer(TileReferences, _record, 
                new MapUnitPosition(xy.X, xy.Y, 0));
        }
        
        // public CombatPlayer(MapUnitState mapUnitState,
        //     SmallMapCharacterState smallMapTheSmallMapCharacterState, 
        //     TileReferences tileReferences,
        //     SmallMapReferences.SingleMapReference.Location location, DataOvlReference dataOvlRef,
        //     Point2D.Direction direction, PlayerCharacterRecord record) : 
        //     // base(null, mapUnitState, smallMapTheSmallMapCharacterState,
        //     // null, null, tileReferences, location, dataOvlRef, direction)
        // {
        //     _record = record;
        //     TheMapUnitState.Tile1Ref = tileReferences.GetTileReferenceOfKeyIndex(record.PrimarySpriteIndex);
        //     //MapUnitPosition.XY = xy;
        // }

        public CombatPlayer()
        {
            
        }

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; }
        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; }
        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        public override string BoardXitName => "GET OFF ME YOU BRUTE!";
        public override bool IsActive => true;


    }
}