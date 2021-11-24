using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Provisions : InventoryItems<Provision.ProvisionTypeEnum, Provision>
    {
        [DataMember]
        public sealed override Dictionary<Provision.ProvisionTypeEnum, Provision> Items { get; internal set; } =
            new Dictionary<Provision.ProvisionTypeEnum, Provision>();

        [IgnoreDataMember] public ushort Food
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.Food].Quantity;
            set => Items[Provision.ProvisionTypeEnum.Food].Quantity = value;
        }

        [IgnoreDataMember] public ushort Gems
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.Gems].Quantity;
            set => Items[Provision.ProvisionTypeEnum.Gems].Quantity = value;
        }

        [IgnoreDataMember] public ushort Gold
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.Gold].Quantity;
            set => Items[Provision.ProvisionTypeEnum.Gold].Quantity = value;
        }

        [IgnoreDataMember] public ushort Keys
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.Keys].Quantity;
            set => Items[Provision.ProvisionTypeEnum.Keys].Quantity = value;
        }

        [IgnoreDataMember] public ushort SkullKeys
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.SkullKeys].Quantity;
            set => Items[Provision.ProvisionTypeEnum.SkullKeys].Quantity = value;
        }

        [IgnoreDataMember] public ushort Torches
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.Torches].Quantity;
            set => Items[Provision.ProvisionTypeEnum.Torches].Quantity = value;
        }

        // [JsonConstructor] private Provisions() { }

        [JsonConstructor] public Provisions() : base(null)
        {
            Items.Add(Provision.ProvisionTypeEnum.Torches,
                new Provision(Provision.ProvisionTypeEnum.Torches,
                    (int)Provision.ProvisionSpritesTypeEnum.Torches));
            Items.Add(Provision.ProvisionTypeEnum.Gems,
                new Provision(Provision.ProvisionTypeEnum.Gems,
                    (int)Provision.ProvisionSpritesTypeEnum.Gems));
            Items.Add(Provision.ProvisionTypeEnum.Keys,
                new Provision(Provision.ProvisionTypeEnum.Keys,
                    (int)Provision.ProvisionSpritesTypeEnum.Keys));
            Items.Add(Provision.ProvisionTypeEnum.SkullKeys,
                new Provision(Provision.ProvisionTypeEnum.SkullKeys, (int)Provision.ProvisionSpritesTypeEnum.Torches));
            Items.Add(Provision.ProvisionTypeEnum.Food,
                new Provision(Provision.ProvisionTypeEnum.Food, (int)Provision.ProvisionSpritesTypeEnum.Food));
            Items.Add(Provision.ProvisionTypeEnum.Gold,
                new Provision(Provision.ProvisionTypeEnum.Gold, (int)Provision.ProvisionSpritesTypeEnum.Gold));
        }
    }
}