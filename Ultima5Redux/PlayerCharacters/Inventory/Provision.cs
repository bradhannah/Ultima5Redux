using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.Maps;
using Ultima5Redux.References;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class Provision : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum ProvisionSpritesTypeEnum
        {
            Torches = 269, Gems = 264, Keys = 263, SkullKeys = 263, Food = 271, Gold = 258
        }

        [JsonConverter(typeof(StringEnumConverter))] public enum ProvisionTypeEnum
        {
            Torches = 0x208, Gems = 0x207, Keys = 0x206, SkullKeys = 0x20B, Food = 0x202, Gold = 0x204
        }

        [IgnoreDataMember] public override bool HideQuantity => false;

        [IgnoreDataMember] public override string InventoryReferenceString => ProvisionType.ToString();

        [IgnoreDataMember] public int BundleQuantity =>
            GameReferences.ProvisionReferences.GetBundleQuantity(ProvisionType);
        
        [DataMember] public ProvisionTypeEnum ProvisionType { get; private set; }

        [JsonConstructor] private Provision()
        {
        }

        /// <summary>
        ///     Creates a provision
        /// </summary>
        /// <param name="provisionTypeEnum">what kind of provision</param>
        /// <param name="findDescription"></param>
        /// <param name="spriteNum"></param>
        public Provision(ProvisionTypeEnum provisionTypeEnum, string findDescription, int spriteNum) : base(0,
            findDescription, spriteNum)
        {
            ProvisionType = provisionTypeEnum;
        }

        /// <summary>
        ///     Gets the cost of the provision based on the avatar's intelligence rating
        /// </summary>
        /// <param name="records"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public override int GetAdjustedBuyPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location)
        {
            int nBasePrice = GameReferences.ProvisionReferences.GetBasePrice(location, ProvisionType);
            if (nBasePrice == -1)
                throw new Ultima5ReduxException("Requested provision " + LongName + " from " + location +
                                                " which is not sold here");

            // The 'base price' is what a character with a theoretical Intelligence of 0 would get. Every additional Intelligence point
            // deducts 1.5% from the item's cost. The scale is reversed for selling, where every point increases gold received by 1.5%,
            // up to a theoretical 66.6 Intelligence points. Therefore, only the 'base prices' need be listed here.
            // Note that buying prices are rounded down, while selling prices are rounded up.
            // http://infinitron.nullneuron.net/u5eco.html
            int nAdjustedPrice = nBasePrice - records.AvatarRecord.Stats.Intelligence * (int)(nBasePrice * 0.015f);
            //_state.CharacterRecords.AvatarRecord.Stats.Intelligence * (int)(nBasePrice * 0.015f);
            return nAdjustedPrice;
        }

    }
}