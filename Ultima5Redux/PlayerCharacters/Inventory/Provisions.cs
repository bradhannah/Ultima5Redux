using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.References;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Provisions : InventoryItems<Provision.ProvisionTypeEnum, Provision>
    {
        public sealed override Dictionary<Provision.ProvisionTypeEnum, Provision> Items { get; } =
            new Dictionary<Provision.ProvisionTypeEnum, Provision>();

        public ushort Food
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.Food].Quantity;
            set => Items[Provision.ProvisionTypeEnum.Food].Quantity = value;
        }

        public ushort Gems
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.Gems].Quantity;
            set => Items[Provision.ProvisionTypeEnum.Gems].Quantity = value;
        }

        public ushort Gold
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.Gold].Quantity;
            set => Items[Provision.ProvisionTypeEnum.Gold].Quantity = value;
        }

        public ushort Keys
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.Keys].Quantity;
            set => Items[Provision.ProvisionTypeEnum.Keys].Quantity = value;
        }

        public ushort SkullKeys
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.SkullKeys].Quantity;
            set => Items[Provision.ProvisionTypeEnum.SkullKeys].Quantity = value;
        }

        public ushort Torches
        {
            get => (ushort)Items[Provision.ProvisionTypeEnum.Torches].Quantity;
            set => Items[Provision.ProvisionTypeEnum.Torches].Quantity = value;
        }

        public Provisions(GameState state) : base(null)
        {
            Items.Add(Provision.ProvisionTypeEnum.Torches,
                new Provision(Provision.ProvisionTypeEnum.Torches,
                    GameReferences.DataOvlRef.StringReferences
                        .GetString(DataOvlReference.ThingsIFindStrings.SOME_TORCHES_BANG_N)
                        .Trim(),
                    (int)Provision.ProvisionSpritesTypeEnum.Torches, state));
            Items.Add(Provision.ProvisionTypeEnum.Gems,
                new Provision(Provision.ProvisionTypeEnum.Gems,
                    GameReferences.DataOvlRef.StringReferences
                        .GetString(DataOvlReference.ThingsIFindStrings.A_GEM_BANG_N).Trim(),
                    (int)Provision.ProvisionSpritesTypeEnum.Gems, state));
            Items.Add(Provision.ProvisionTypeEnum.Keys,
                new Provision(Provision.ProvisionTypeEnum.Keys,
                    GameReferences.DataOvlRef.StringReferences
                        .GetString(DataOvlReference.ThingsIFindStrings.A_RING_OF_KEYS_BANG_N)
                        .Trim(),
                    (int)Provision.ProvisionSpritesTypeEnum.Keys, state));
            Items.Add(Provision.ProvisionTypeEnum.SkullKeys,
                new Provision(Provision.ProvisionTypeEnum.SkullKeys,
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.S_ODD_KEY)
                        .Trim(),
                    (int)Provision.ProvisionSpritesTypeEnum.Torches, state));
            Items.Add(Provision.ProvisionTypeEnum.Food,
                new Provision(Provision.ProvisionTypeEnum.Food,
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.S_FOOD)
                        .Trim(),
                    (int)Provision.ProvisionSpritesTypeEnum.Food, state));
            Items.Add(Provision.ProvisionTypeEnum.Gold,
                new Provision(Provision.ProvisionTypeEnum.Gold,
                    GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.GetThingsStrings.S_GOLD)
                        .Trim(),
                    (int)Provision.ProvisionSpritesTypeEnum.Gold, state));
        }
    }
}