using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;
using Ultima5Redux.References;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;
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
        } = new();

        [IgnoreDataMember]
        public ushort Food
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.Food].Quantity;
            internal set => Items[ProvisionReferences.SpecificProvisionType.Food].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Gems
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.Gems].Quantity;
            internal set => Items[ProvisionReferences.SpecificProvisionType.Gems].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Gold
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.Gold].Quantity;
            internal set => Items[ProvisionReferences.SpecificProvisionType.Gold].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Keys
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.Keys].Quantity;
            internal set => Items[ProvisionReferences.SpecificProvisionType.Keys].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort SkullKeys
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.SkullKeys].Quantity;
            internal set => Items[ProvisionReferences.SpecificProvisionType.SkullKeys].Quantity = value;
        }

        [IgnoreDataMember]
        public ushort Torches
        {
            get => (ushort)Items[ProvisionReferences.SpecificProvisionType.Torches].Quantity;
            internal set => Items[ProvisionReferences.SpecificProvisionType.Torches].Quantity = value;
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

        public void FoodStolen(TurnResults turnResults, EnemyReference enemyReference, int nAmountStolen)
        {
            turnResults.PushTurnResult(new BasicResult(TurnResult.TurnResultType.FoodStolenByEnemy));
            turnResults.PushOutputToConsole(
                $"{enemyReference.MixedCaseSingularName}{GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.ResponseToKeystroke._STOLE_SOME_FOOD_BANG_N)}");
            AddOrRemoveProvisionQuantity(ProvisionReferences.SpecificProvisionType.Food, -nAmountStolen, turnResults);
        }


        public void SetProvisionQuantity(ProvisionReferences.SpecificProvisionType specificProvisionType,
            int nNewQuantity, TurnResults turnResults)
        {
            ushort nPreviousQuantity;
            switch (specificProvisionType)
            {
                case ProvisionReferences.SpecificProvisionType.Torches:
                    nPreviousQuantity = Torches;
                    Torches = (ushort)nNewQuantity;
                    break;
                case ProvisionReferences.SpecificProvisionType.Gems:
                    nPreviousQuantity = Gems;
                    Gems = (ushort)nNewQuantity;
                    break;
                case ProvisionReferences.SpecificProvisionType.Keys:
                    nPreviousQuantity = Keys;
                    Keys = (ushort)nNewQuantity;
                    break;
                case ProvisionReferences.SpecificProvisionType.SkullKeys:
                    nPreviousQuantity = SkullKeys;
                    SkullKeys = (ushort)nNewQuantity;
                    break;
                case ProvisionReferences.SpecificProvisionType.Food:
                    nPreviousQuantity = Food;
                    Food = (ushort)nNewQuantity;
                    break;
                case ProvisionReferences.SpecificProvisionType.Gold:
                    nPreviousQuantity = Gold;
                    Gold = (ushort)nNewQuantity;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(specificProvisionType), specificProvisionType, null);
            }

            turnResults.PushTurnResult(new ProvisionQuantityChanged(nNewQuantity - nPreviousQuantity, nNewQuantity,
                specificProvisionType));
        }

        public void AddOrRemoveProvisionQuantity(ProvisionReferences.SpecificProvisionType specificProvisionType,
            int nAdjustBy, TurnResults turnResults)
        {
            switch (specificProvisionType)
            {
                case ProvisionReferences.SpecificProvisionType.Torches:
                    SetProvisionQuantity(specificProvisionType, nAdjustBy + Torches, turnResults);
                    break;
                case ProvisionReferences.SpecificProvisionType.Gems:
                    SetProvisionQuantity(specificProvisionType, nAdjustBy + Gems, turnResults);
                    break;
                case ProvisionReferences.SpecificProvisionType.Keys:
                    SetProvisionQuantity(specificProvisionType, nAdjustBy + Keys, turnResults);
                    break;
                case ProvisionReferences.SpecificProvisionType.SkullKeys:
                    SetProvisionQuantity(specificProvisionType, nAdjustBy + SkullKeys, turnResults);
                    break;
                case ProvisionReferences.SpecificProvisionType.Food:
                    SetProvisionQuantity(specificProvisionType, nAdjustBy + Food, turnResults);
                    break;
                case ProvisionReferences.SpecificProvisionType.Gold:
                    SetProvisionQuantity(specificProvisionType, nAdjustBy + Gold, turnResults);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(specificProvisionType), specificProvisionType, null);
            }
        }
    }
}