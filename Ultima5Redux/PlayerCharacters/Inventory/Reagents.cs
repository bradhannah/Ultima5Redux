using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    /// <summary>
    ///     All reagents collection
    /// </summary>
    [DataContract] public class Reagents : InventoryItems<Reagent.SpecificReagentType, Reagent>
    {
        [DataMember]
        public override Dictionary<Reagent.SpecificReagentType, Reagent> Items { get; internal set; } = new();

        [JsonConstructor] private Reagents()
        {
        }

        public Reagents(ImportedGameState importedGameState)
        {
            void addReagentLegacy(Reagent.SpecificReagentType reagentType)
            {
                AddReagent(reagentType, importedGameState.GetReagentQuantity(reagentType));
            }

            foreach (Reagent.SpecificReagentType reagent in Enum.GetValues(typeof(Reagent.SpecificReagentType)))
            {
                addReagentLegacy(reagent);
            }
        }

        private void AddReagent(Reagent.SpecificReagentType specificReagentType, int nQuantity)
        {
            Reagent reagent = new(specificReagentType, nQuantity);
            Items[specificReagentType] = reagent;
        }

        public List<Reagent> GetReagentsForSale(SmallMapReferences.SingleMapReference.Location location)
        {
            return Items.Values.Where(reagent => reagent.IsReagentForSale(location)).ToList();
        }
    }
}