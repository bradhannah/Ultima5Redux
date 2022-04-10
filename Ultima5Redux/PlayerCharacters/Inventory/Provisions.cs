using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Provisions : InventoryItems<ProvisionReferences.SpecificProvisionType, Provision>
    {
        [DataMember]
        public sealed override Dictionary<ProvisionReferences.SpecificProvisionType, Provision> Items
        {
            get;
            internal set;
        } =
            new();

        [IgnoreDataMember]
        public ushort Food
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.Food].Quantity;
            set => Items[ProvisionReferences.SpecificProvisionType.Food].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Gems
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.Gems].Quantity;
            set => Items[ProvisionReferences.SpecificProvisionType.Gems].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Gold
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.Gold].Quantity;
            set => Items[ProvisionReferences.SpecificProvisionType.Gold].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Keys
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.Keys].Quantity;
            set => Items[ProvisionReferences.SpecificProvisionType.Keys].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort SkullKeys
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.SkullKeys].Quantity;
            set => Items[ProvisionReferences.SpecificProvisionType.SkullKeys].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Torches
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.Torches].Quantity;
            set => Items[ProvisionReferences.SpecificProvisionType.Torches].Quantity = value;
        }

        [JsonConstructor] private Provisions()
        {
        }

        public Provisions(ImportedGameState importedGameState)
        {
            if (Items.Count > 0) return;

            Items.Add(ProvisionReferences.SpecificProvisionType.Torches,
                new Provision(ProvisionReferences.SpecificProvisionType.Torches,
                    (int)ProvisionReferences.SpecificProvisionSpritesType.Torches));
            Items.Add(ProvisionReferences.SpecificProvisionType.Gems,
                new Provision(ProvisionReferences.SpecificProvisionType.Gems,
                    (int)ProvisionReferences.SpecificProvisionSpritesType.Gems));
            Items.Add(ProvisionReferences.SpecificProvisionType.Keys,
                new Provision(ProvisionReferences.SpecificProvisionType.Keys,
                    (int)ProvisionReferences.SpecificProvisionSpritesType.Keys));
            Items.Add(ProvisionReferences.SpecificProvisionType.SkullKeys,
                new Provision(ProvisionReferences.SpecificProvisionType.SkullKeys,
                    (int)ProvisionReferences.SpecificProvisionSpritesType.Torches));
            Items.Add(ProvisionReferences.SpecificProvisionType.Food,
                new Provision(ProvisionReferences.SpecificProvisionType.Food,
                    (int)ProvisionReferences.SpecificProvisionSpritesType.Food));
            Items.Add(ProvisionReferences.SpecificProvisionType.Gold,
                new Provision(ProvisionReferences.SpecificProvisionType.Gold,
                    (int)ProvisionReferences.SpecificProvisionSpritesType.Gold));

            Items[ProvisionReferences.SpecificProvisionType.Food].Quantity = importedGameState.Food;
            Items[ProvisionReferences.SpecificProvisionType.Gems].Quantity = importedGameState.Gems;
            Items[ProvisionReferences.SpecificProvisionType.Gold].Quantity = importedGameState.Gold;
            Items[ProvisionReferences.SpecificProvisionType.Keys].Quantity = importedGameState.Keys;
            Items[ProvisionReferences.SpecificProvisionType.Torches].Quantity = importedGameState.Torches;
            Items[ProvisionReferences.SpecificProvisionType.SkullKeys].Quantity = importedGameState.SkullKeys;
        }
    }
}