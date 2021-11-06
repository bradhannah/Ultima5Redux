using Newtonsoft.Json;
using Ultima5Redux.MapUnits;

namespace Ultima5Redux.Maps
{
    /// <summary>
    ///     A single overriden tile
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)] public class TileOverrideReference
    {
        [JsonProperty] public int MapNumber;

        public bool IsOverworld => MapNumber == 0 && Z == 0;

        public bool IsSmallMap => MapNumber != 0;

        public bool IsUnderworld => MapNumber == 0 && Z == -1;

        [JsonProperty] public int SpriteNum { get; set; }

        [JsonProperty] public int X { get; set; }

        [JsonProperty] public int Y { get; set; }

        [JsonProperty] public int Z { get; set; }

        public MapUnitPosition Position => new MapUnitPosition(X, Y, Z);

        [JsonProperty] public string Comment { get; set; }

        [JsonProperty] public string SpriteName { get; set; }
    }
}