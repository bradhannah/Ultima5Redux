using System.Collections.Generic;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    public class ProvisionReferences
    {
        private static readonly Dictionary<Provision.ProvisionTypeEnum, int> BundleQuantity =
            new Dictionary<Provision.ProvisionTypeEnum, int>
            {
                { Provision.ProvisionTypeEnum.Torches, 5 }, { Provision.ProvisionTypeEnum.Keys, 3 },
                { Provision.ProvisionTypeEnum.Gems, 4 }
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
        private static readonly Dictionary<Provision.ProvisionTypeEnum, int> ProvisionOrder =
            new Dictionary<Provision.ProvisionTypeEnum, int>
            {
                { Provision.ProvisionTypeEnum.Keys, 0 }, { Provision.ProvisionTypeEnum.Gems, 1 },
                { Provision.ProvisionTypeEnum.Torches, 2 }
            };

        /// <summary>
        ///     Gets the base price which is later adjusted based on intelligence
        /// </summary>
        /// <param name="location"></param>
        /// <param name="provisionType"></param>
        /// <returns>Greater than zero if it is sold, otherwise returns -1</returns>
        public int GetBasePrice(SmallMapReferences.SingleMapReference.Location location,
            Provision.ProvisionTypeEnum provisionType)
        {
            int nIndex = 0;
            foreach (byte b in GameReferences.DataOvlRef
                .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_TOWNES_PROVISIONS).GetAsByteList())
            {
                SmallMapReferences.SingleMapReference.Location potentialLocation =
                    (SmallMapReferences.SingleMapReference.Location)b;
                if (potentialLocation == location)
                    // they sell it, now we find it
                    return Prices[nIndex, ProvisionOrder[provisionType]];
                nIndex++;
            }

            return -1;
        }

        /// <summary>
        ///     Gets the bundle quantity for the current provision
        /// </summary>
        /// <returns></returns>
        public int GetBundleQuantity(Provision.ProvisionTypeEnum provisionType)
        {
            return BundleQuantity[provisionType];
        }
    }
}