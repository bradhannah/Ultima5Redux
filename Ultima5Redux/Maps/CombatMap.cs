namespace Ultima5Redux.Maps
{
    public class CombatMap : Map
    {
        private readonly SingleCombatMapReference _singleCombatMapReference;

        public CombatMap(TileOverrides tileOverrides, SingleCombatMapReference singleCombatMapReference) : base(tileOverrides, null)
        {
            _singleCombatMapReference = singleCombatMapReference;
        }

        public override int NumOfXTiles => SingleCombatMapReference.XTILES;
        public override int NumOfYTiles => SingleCombatMapReference.YTILES;

        public override byte[][] TheMap {
            get => _singleCombatMapReference.TheMap;
            protected set
            {
                
            }
        }
        
        protected override float GetAStarWeight(TileReferences spriteTileReferences, Point2D xy)
        {
            return 1.0f;
        }
    }
}