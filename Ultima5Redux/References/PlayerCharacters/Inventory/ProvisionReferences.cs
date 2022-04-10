using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    public class ProvisionReferences
    {
        private static readonly Dictionary<SpecificProvisionType, int> BundleQuantity =
            new()
            {
                { SpecificProvisionType.Torches, 5 }, { SpecificProvisionType.Keys, 3 },
                { SpecificProvisionType.Gems, 4 }
            };

        /// <summary>
        ///     the prices of provisions because I can't find it in the code!
        /// </summary>
        private static readonly int[,] Prices =
        {
            {
                320, 400, 22
            },
            {
                370, 450, 50
            },
            {
                380, 510, 24
            }
        };

        // the order of the provisions in the _prices array
        private static readonly Dictionary<SpecificProvisionType, int> ProvisionOrder =
            new()
            {
                { SpecificProvisionType.Keys, 0 }, { SpecificProvisionType.Gems, 1 },
                { SpecificProvisionType.Torches, 2 }
            };

        /// <summary>
        ///     Gets the base price which is later adjusted based on intelligence
        /// </summary>
        /// <param name="location"></param>
        /// <param name="specificProvisionType"></param>
        /// <returns>Greater than zero if it is sold, otherwise returns -1</returns>
        public int GetBasePrice(SmallMapReferences.SingleMapReference.Location location,
            SpecificProvisionType specificProvisionType)
        {
            int nIndex = 0;
            foreach (byte b in GameReferences.DataOvlRef
                         .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_TOWNES_PROVISIONS).GetAsByteList())
            {
                SmallMapReferences.SingleMapReference.Location potentialLocation =
                    (SmallMapReferences.SingleMapReference.Location)b;
                if (potentialLocation == location)
                    // they sell it, now we find it
                    return Prices[nIndex, ProvisionOrder[specificProvisionType]];
                nIndex++;
            }

            return -1;
        }

        /// <summary>
        ///     Gets the bundle quantity for the current provision
        /// </summary>
        /// <returns></returns>
        public int GetBundleQuantity(SpecificProvisionType specificProvisionType)
        {
            return BundleQuantity[specificProvisionType];
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpecificProvisionSpritesType
        {
            Torches = 269, Gems = 264, Keys = 263, SkullKeys = 263, Food = 271, Gold = 258
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpecificProvisionType
        {
            Torches = 0x208, Gems = 0x207, Keys = 0x206, SkullKeys = 0x20B, Food = 0x202, Gold = 0x204
        }
    }
}