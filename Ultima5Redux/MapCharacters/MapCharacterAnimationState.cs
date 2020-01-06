using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace Ultima5Redux
{
    //    http://wiki.ultimacodex.com/wiki/Ultima_V_Internal_Formats#SAVED.GAM
    //    Monster Format
    //offset length  purpose
    //0	1	tile
    //1	1	tile
    //2	1	x coordinate
    //3	1	y coordinate
    //4	1	z coordinate(level)
    //5	1	depends on object type
    //6	1	depends on object type
    //7	1	depends on object type

    public class MapCharacterAnimationState
    {
        public const int NBYTES = 0x8;


        #region Private Fields
        private int Tile1;
        private int Tile2;
        #endregion

        #region Public Properties
        public byte X { get; }
        public byte Y { get; }
        public byte Floor { get; }
        public byte Depends1 { get; }
        public byte Depends2 { get; }
        public byte Depends3 { get; }
        public TileReference Tile1Ref { get; }
        public TileReference Tile2Ref { get; }
        #endregion

        public MapCharacterAnimationState(TileReferences tileReferences, byte[] stateBytes)
        {
            Debug.Assert(stateBytes.Length == 0x8);
            //
            Tile1 = stateBytes[0] + 0x100;
            Tile2 = stateBytes[1] + 0x100;
            Tile1Ref = tileReferences.GetTileReference(Tile1);
            Tile2Ref = tileReferences.GetTileReference(Tile2);

            X = stateBytes[2];
            Y = stateBytes[3];
            Floor = stateBytes[4];
            Depends1 = stateBytes[5];
            Depends2 = stateBytes[6];
            Depends3 = stateBytes[7];
        }


    }
}
