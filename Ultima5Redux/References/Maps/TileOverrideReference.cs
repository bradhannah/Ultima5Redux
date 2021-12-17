using Newtonsoft.Json;
using Ultima5Redux.MapUnits;

namespace Ultima5Redux.References.Maps
{
    /// <summary>
    ///     A single overriden tile
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)] public class TileOverrideReference
    {
        [JsonProperty] public int MapNumber;

        [JsonProperty] public string Comment { get; set; }
        public bool IsOverworld => MapNumber == 0 && Z == 0;

        public bool IsSmallMap => MapNumber != 0;

        public bool IsUnderworld => MapNumber == 0 && Z == -1;

        public MapUnitPosition Position => new(X, Y, Z);

        [JsonProperty] public string SpriteName { get; set; }

        [JsonProperty] public int SpriteNum { get; set; }

        [JsonProperty] public int X { get; set; }

        [JsonProperty] public int Y { get; set; }

        [JsonProperty] public int Z { get; set; }
    }
}