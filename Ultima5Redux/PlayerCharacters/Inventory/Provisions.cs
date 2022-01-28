using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Provisions : InventoryItems<Provision.SpecificProvisionType, Provision>
    {
        [DataMember]
        public sealed override Dictionary<Provision.SpecificProvisionType, Provision> Items { get; internal set; } =
            new();

        [IgnoreDataMember]
        public ushort Food
        {
            get => (ushort)Items[Provision.SpecificProvisionType.Food].Quantity;
            set => Items[Provision.SpecificProvisionType.Food].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Gems
        {
            get => (ushort)Items[Provision.SpecificProvisionType.Gems].Quantity;
            set => Items[Provision.SpecificProvisionType.Gems].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Gold
        {
            get => (ushort)Items[Provision.SpecificProvisionType.Gold].Quantity;
            set => Items[Provision.SpecificProvisionType.Gold].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Keys
        {
            get => (ushort)Items[Provision.SpecificProvisionType.Keys].Quantity;
            set => Items[Provision.SpecificProvisionType.Keys].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort SkullKeys
        {
            get => (ushort)Items[Provision.SpecificProvisionType.SkullKeys].Quantity;
            set => Items[Provision.SpecificProvisionType.SkullKeys].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Torches
        {
            get => (ushort)Items[Provision.SpecificProvisionType.Torches].Quantity;
            set => Items[Provision.SpecificProvisionType.Torches].Quantity = value;
        }

        [JsonConstructor] private Provisions()
        {
        }

        public Provisions(ImportedGameState importedGameState)
        {
            if (Items.Count > 0) return;

            Items.Add(Provision.SpecificProvisionType.Torches,
                new Provision(Provision.SpecificProvisionType.Torches,
                    (int)Provision.SpecificProvisionSpritesType.Torches));
            Items.Add(Provision.SpecificProvisionType.Gems,
                new Provision(Provision.SpecificProvisionType.Gems,
                    (int)Provision.SpecificProvisionSpritesType.Gems));
            Items.Add(Provision.SpecificProvisionType.Keys,
                new Provision(Provision.SpecificProvisionType.Keys,
                    (int)Provision.SpecificProvisionSpritesType.Keys));
            Items.Add(Provision.SpecificProvisionType.SkullKeys,
                new Provision(Provision.SpecificProvisionType.SkullKeys,
                    (int)Provision.SpecificProvisionSpritesType.Torches));
            Items.Add(Provision.SpecificProvisionType.Food,
                new Provision(Provision.SpecificProvisionType.Food, (int)Provision.SpecificProvisionSpritesType.Food));
            Items.Add(Provision.SpecificProvisionType.Gold,
                new Provision(Provision.SpecificProvisionType.Gold, (int)Provision.SpecificProvisionSpritesType.Gold));

            Items[Provision.SpecificProvisionType.Food].Quantity = importedGameState.Food;
            Items[Provision.SpecificProvisionType.Gems].Quantity = importedGameState.Gems;
            Items[Provision.SpecificProvisionType.Gold].Quantity = importedGameState.Gold;
            Items[Provision.SpecificProvisionType.Keys].Quantity = importedGameState.Keys;
            Items[Provision.SpecificProvisionType.Torches].Quantity = importedGameState.Torches;
            Items[Provision.SpecificProvisionType.SkullKeys].Quantity = importedGameState.SkullKeys;
        }
    }
}