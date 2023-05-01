using System.Runtime.Serialization;

namespace Ultima5Redux.References.Maps
{
    /// <summary>
    ///     A single overriden tile
    /// </summary>
    [DataContract]
    public class TileOverrideReference
    {
        public enum TileType
        {
            Flat = 1,
            Primary = 2
        }

        [DataMember] public string Comment { get; set; }
        [DataMember] public int MapNumber { get; private set; }

        [DataMember] public string SpriteName { get; set; }

        [DataMember] public int SpriteNum { get; set; }

        [DataMember] public TileType TheTileType { get; set; }

        [DataMember] public int X { get; set; }

        [DataMember] public int Y { get; set; }

        [DataMember] public int Z { get; set; }
    }
}