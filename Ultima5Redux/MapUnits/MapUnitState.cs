using System.Diagnostics;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits
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

    public class MapUnitState
    {
        public const int NBYTES = 0x8;
        // ReSharper disable once NotAccessedField.Local
        private readonly byte[] _stateBytes;

        private int _tile1;
        private int _tile2;

        public MapUnitState(TileReferences tileReferences, byte[] stateBytes)
        {
            Debug.Assert(stateBytes.Length == 0x8);
            _stateBytes = stateBytes;
            //
            _tile1 = stateBytes[0] + 0x100;
            _tile2 = stateBytes[1] + 0x100;
            Tile1Ref = tileReferences.GetTileReference(_tile1);
            Tile2Ref = tileReferences.GetTileReference(_tile2);

            X = stateBytes[2];
            Y = stateBytes[3];
            Floor = stateBytes[4];
            Depends1 = stateBytes[5];
            Depends2 = stateBytes[6];
            Depends3 = stateBytes[7];
        }


        public MapUnitState(TileReferences tileReferences, NonPlayerCharacterReference npcRef)
        {
            Tile1Ref = tileReferences.GetTileReference(npcRef.NPCKeySprite);
            Tile2Ref = tileReferences.GetTileReference(npcRef.NPCKeySprite);
        }

        internal MapUnitState()
        {
        }

        public byte X { get; set; }
        public byte Y { get; set; }
        public byte Floor { get; set; }
        private byte Depends1 { get; set; }
        private byte Depends2 { get; set; }
        internal byte Depends3 { get; set; }
        public TileReference Tile1Ref { get; internal set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public TileReference Tile2Ref { get; internal set; }

        public void SetTileReference(TileReference tileReference)
        {
            Tile1Ref = tileReference;
            Tile2Ref = tileReference;
        }

        public void CopyTo(TileReferences tileReferences, MapUnitState mapUnitState)
        {
            mapUnitState._tile1 = _tile1;
            mapUnitState._tile2 = _tile2;
            mapUnitState.Depends1 = Depends1;
            mapUnitState.Depends2 = Depends2;
            mapUnitState.Depends3 = Depends3;
            mapUnitState.Floor = Floor;
            mapUnitState.X = X;
            mapUnitState.Y = Y;
            mapUnitState.SetTileReference(tileReferences.GetTileReference(_tile1));
        }

        public static MapUnitState CreateAvatar(TileReferences tileReferences, MapUnitPosition avatarPosition,
            MapUnitState mapUnitState = null)
        {
            // if a null map unit state is passed in then we are the default Avatar sprite
            // otherwise we may be on a horse, ship etc.
            bool bDefaultState = mapUnitState != null;
            MapUnitState theAvatar;
            TileReference avatarTileRef;
            // MapUnitState theAvatar = mapUnitState ?? new MapUnitState();
            // TileReference avatarTileRef = bDefaultState ? mapUnitState.Tile1Ref :
            //     tileReferences.GetTileReferenceByName("BasicAvatar");

            if (mapUnitState == null)
            {
                theAvatar = new MapUnitState();
                avatarTileRef = tileReferences.GetTileReferenceByName("BasicAvatar");
            }
            else
            {
                theAvatar = mapUnitState;
                avatarTileRef = mapUnitState.Tile1Ref;
            }

            theAvatar._tile1 = avatarTileRef.Index;
            theAvatar._tile2 = avatarTileRef.Index;
            theAvatar.Tile1Ref = tileReferences.GetTileReference(theAvatar._tile1);
            theAvatar.Tile2Ref = theAvatar.Tile1Ref;

            theAvatar.X = (byte) avatarPosition.X;
            theAvatar.Y = (byte) avatarPosition.Y;
            theAvatar.Floor = (byte) avatarPosition.Floor;
            return theAvatar;
        }
    }
}