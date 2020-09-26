using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.SeaFaringVessels
{
    public static class SeaFaringVesselReference
    {
        /// <summary>
        /// Gets the direction that a sprite is pointed
        /// </summary>
        /// <param name="tileRefs"></param>
        /// <param name="nSprite"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        // public static VirtualMap.Direction GetDirectionBySprite(TileReferences tileRefs, int nSprite)
        // {
        //     TileReference tileRef = tileRefs.GetTileReference(nSprite);
        //     if (tileRef.Name.EndsWith("Down")) return VirtualMap.Direction.Down;
        //     if (tileRef.Name.EndsWith("Up")) return VirtualMap.Direction.Up;
        //     if (tileRef.Name.EndsWith("Left")) return VirtualMap.Direction.Left;
        //     if (tileRef.Name.EndsWith("Right")) return VirtualMap.Direction.Right;
        //     throw new Ultima5ReduxException("Chose a sprite that doesn't have a declared direction");
        // }
    }
}