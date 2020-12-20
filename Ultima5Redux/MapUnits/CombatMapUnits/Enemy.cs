using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.Monsters
{
    public class Enemy : CombatMapUnit
    {
        
        public EnemyReference EnemyReference { get; }
        
        public Enemy(TileReferences tileReferences, Point2D xy, EnemyReference enemyReference) 
        {
            TileReferences = tileReferences;
            EnemyReference = enemyReference;
            TheMapUnitState = MapUnitState.CreateMonster(TileReferences, new MapUnitPosition(xy.X, xy.Y, 0), 
                enemyReference.KeyTileReference.Index);   
        }
        
        public Enemy(MapUnitState mapUnitState, MapUnitMovement mapUnitMovement, TileReferences tileReferences,  
            EnemyReference enemyReference, SmallMapReferences.SingleMapReference.Location location,
            DataOvlReference dataOvlReference)
        : base(null, mapUnitState, null, mapUnitMovement, null, 
            tileReferences, location, dataOvlReference)
        {
            //TileReferences = tileReferences;
            EnemyReference = enemyReference;
            // TheMapUnitState = MapUnitState.CreateMonster(TileReferences, new MapUnitPosition(xy.X, xy.Y, 0), 
            //     enemyReference.KeyTileReference.Index);   
        }

        
        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; }

        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; }

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        public override string BoardXitName => "Hostile creates don't not like to be boarded!";
        public override TileReference NonBoardedTileReference => TheMapUnitState.Tile1Ref;
        public override bool IsActive => true;
    }
}