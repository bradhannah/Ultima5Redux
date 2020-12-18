namespace Ultima5Redux.Maps
{
    public class CombatMap : Map
    {
        private readonly SingleCombatMapReference _singleCombatMapReference;

        public SingleCombatMapReference TheMapReference => _singleCombatMapReference;

        public CombatMap(SingleCombatMapReference singleCombatMapReference) : base(null, null)
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