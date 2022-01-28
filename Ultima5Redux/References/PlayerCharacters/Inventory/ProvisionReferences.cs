using System.Collections.Generic;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    public class ProvisionReferences
    {
        private static readonly Dictionary<Provision.SpecificProvisionType, int> BundleQuantity =
            new()
            {
                { Provision.SpecificProvisionType.Torches, 5 }, { Provision.SpecificProvisionType.Keys, 3 },
                { Provision.SpecificProvisionType.Gems, 4 }
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
        private static readonly Dictionary<Provision.SpecificProvisionType, int> ProvisionOrder =
            new()
            {
                { Provision.SpecificProvisionType.Keys, 0 }, { Provision.SpecificProvisionType.Gems, 1 },
                { Provision.SpecificProvisionType.Torches, 2 }
            };

        /// <summary>
        ///     Gets the base price which is later adjusted based on intelligence
        /// </summary>
        /// <param name="location"></param>
        /// <param name="specificProvisionTypem>
        /// <returns>Greater than zero if it is sold, otherwise returns -1</returns>
        public int GetBasePrice(SmallMapReferences.SingleMapReference.Location location,
            Provision.SpecificProvisionType specificProvisionType)
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
        public int GetBundleQuantity(Provision.SpecificProvisionType specificProvisionType)
        {
            return BundleQuantity[specificProvisionType];
        }
    }
}