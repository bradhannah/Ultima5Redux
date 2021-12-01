using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

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

        [DataMember] public ProvisionTypeEnum ProvisionType { get; private set; }
        [IgnoreDataMember] public override bool HideQuantity => false;

        [IgnoreDataMember] public override string InventoryReferenceString => ProvisionType.ToString();

        [IgnoreDataMember] public int BundleQuantity =>
            GameReferences.ProvisionReferences.GetBundleQuantity(ProvisionType);

        public override string FindDescription
        {
            get
            {
                string getProvStr(DataOvlReference.ThingsIFindStrings provType) =>
                    GameReferences.DataOvlRef.StringReferences.GetString(provType).Trim();

                return ProvisionType switch
                {
                    ProvisionTypeEnum.Torches => getProvStr(DataOvlReference.ThingsIFindStrings.SOME_TORCHES_BANG_N),
                    ProvisionTypeEnum.Gems => getProvStr(DataOvlReference.ThingsIFindStrings.A_GEM_BANG_N),
                    ProvisionTypeEnum.Keys => getProvStr(DataOvlReference.ThingsIFindStrings.A_RING_OF_KEYS_BANG_N),
                    ProvisionTypeEnum.SkullKeys => GameReferences.DataOvlRef.StringReferences
                        .GetString(DataOvlReference.GetThingsStrings.S_ODD_KEY).Trim(),
                    ProvisionTypeEnum.Food => GameReferences.DataOvlRef.StringReferences
                        .GetString(DataOvlReference.GetThingsStrings.S_FOOD).Trim(),
                    ProvisionTypeEnum.Gold => GameReferences.DataOvlRef.StringReferences
                        .GetString(DataOvlReference.GetThingsStrings.S_GOLD).Trim(),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        [JsonConstructor] private Provision()
        {
        }

        /// <summary>
        ///     Creates a provision
        /// </summary>
        /// <param name="provisionTypeEnum">what kind of provision</param>
        /// <param name="spriteNum"></param>
        public Provision(ProvisionTypeEnum provisionTypeEnum, int spriteNum) : base(0, spriteNum,
            InventoryReferences.InventoryReferenceType.Item)
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