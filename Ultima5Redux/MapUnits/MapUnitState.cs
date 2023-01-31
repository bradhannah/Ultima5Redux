using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    //    http://wiki.ultimacodex.com/wiki/Ultima_V_Internal_Formats#SAVED.GAM
    //    Enemy Format
    //offset length  purpose
    //0	1	tile
    //1	1	tile
    //2	1	x coordinate
    //3	1	y coordinate
    //4	1	z coordinate(level)
    //5	1	depends on object type
    //6	1	depends on object type
    //7	1	depends on object type

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")] public class MapUnitState
    {
        public const int NBYTES = 0x8;

        // ReSharper disable once NotAccessedField.Local
        private readonly byte[] _stateBytes;

        private int _tile1;
        private int _tile2;

        internal byte Depends1 { get; set; }
        internal byte Depends3 { get; set; }
        private byte Depends2 { get; set; }
        public byte Floor { get; set; }
        public TileReference Tile1Ref { get; internal set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public TileReference Tile2Ref { get; internal set; }

        public byte X { get; set; }
        public byte Y { get; set; }

        internal MapUnitState()
        {
        }

        public MapUnitState(byte[] stateBytes)
        {
            Debug.Assert(stateBytes.Length == 0x8);
            _stateBytes = stateBytes;
            //
            _tile1 = stateBytes[0] + 0x100;
            _tile2 = stateBytes[1] + 0x100;
            Tile1Ref = GameReferences.Instance.SpriteTileReferences.GetTileReference(_tile1);
            Tile2Ref = GameReferences.Instance.SpriteTileReferences.GetTileReference(_tile2);

            X = stateBytes[2];
            Y = stateBytes[3];
            Floor = stateBytes[4];
            Depends1 = stateBytes[5];
            Depends2 = stateBytes[6];
            Depends3 = stateBytes[7];
        }

        public MapUnitState(NonPlayerCharacterReference npcRef)
        {
            Tile1Ref = GameReferences.Instance.SpriteTileReferences.GetTileReference(npcRef.NPCKeySprite);
            Tile2Ref = GameReferences.Instance.SpriteTileReferences.GetTileReference(npcRef.NPCKeySprite);
        }

        public static MapUnitState CreateAvatar(MapUnitPosition avatarPosition, MapUnitState mapUnitState = null)
        {
            // if a null map unit state is passed in then we are the default Avatar sprite
            // otherwise we may be on a horse, ship etc.
            MapUnitState theAvatar;
            TileReference avatarTileRef;

            if (mapUnitState == null)
            {
                theAvatar = new MapUnitState();
                avatarTileRef = GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("BasicAvatar");
            }
            else
            {
                theAvatar = mapUnitState;
                avatarTileRef = mapUnitState.Tile1Ref;
            }

            theAvatar._tile1 = avatarTileRef.Index;
            theAvatar._tile2 = avatarTileRef.Index;
            theAvatar.Tile1Ref = GameReferences.Instance.SpriteTileReferences.GetTileReference(theAvatar._tile1);
            theAvatar.Tile2Ref = theAvatar.Tile1Ref;

            theAvatar.X = (byte)avatarPosition.X;
            theAvatar.Y = (byte)avatarPosition.Y;
            theAvatar.Floor = (byte)avatarPosition.Floor;
            return theAvatar;
        }

        public static MapUnitState CreateCombatPlayer(TileReferences tileReferences, PlayerCharacterRecord record,
            MapUnitPosition combatPlayerPosition)
        {
            MapUnitState combatPlayer = new();

            TileReference combatPlayerTileReference = tileReferences.GetTileReference(record.PrimarySpriteIndex);
            Debug.Assert(combatPlayerTileReference != null);

            combatPlayer._tile1 = combatPlayerTileReference.Index;
            combatPlayer._tile2 = combatPlayerTileReference.Index;
            combatPlayer.Tile1Ref = tileReferences.GetTileReference(combatPlayer._tile1);
            combatPlayer.Tile2Ref = combatPlayer.Tile1Ref;

            combatPlayer.X = (byte)combatPlayerPosition.X;
            combatPlayer.Y = (byte)combatPlayerPosition.Y;
            combatPlayer.Floor = (byte)combatPlayerPosition.Floor;
            return combatPlayer;
        }

        public static MapUnitState CreateMapUnitState(TileReferences tileReferences,
            MapUnitPosition mapUnitStatePosition, int nSprite)
        {
            MapUnitState mapUnitState = new();

            TileReference mapUnitStateTileReference = tileReferences.GetTileReference(nSprite);
            Debug.Assert(mapUnitStateTileReference != null);

            mapUnitState._tile1 = mapUnitStateTileReference.Index;
            mapUnitState._tile2 = mapUnitStateTileReference.Index;
            mapUnitState.Tile1Ref = tileReferences.GetTileReference(mapUnitState._tile1);
            mapUnitState.Tile2Ref = mapUnitState.Tile1Ref;

            mapUnitState.X = (byte)mapUnitStatePosition.X;
            mapUnitState.Y = (byte)mapUnitStatePosition.Y;
            mapUnitState.Floor = (byte)mapUnitStatePosition.Floor;
            return mapUnitState;
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

        public void SetTileReference(TileReference tileReference)
        {
            Tile1Ref = tileReference;
            Tile2Ref = tileReference;
        }
    }
}