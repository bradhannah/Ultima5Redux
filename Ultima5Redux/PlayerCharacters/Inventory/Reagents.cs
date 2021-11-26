using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    /// <summary>
    ///     All reagents collection
    /// </summary>
    [DataContract] public class Reagents : InventoryItems<Reagent.ReagentTypeEnum, Reagent>
    {
        [DataMember] public override Dictionary<Reagent.ReagentTypeEnum, Reagent> Items { get; internal set; } =
            new Dictionary<Reagent.ReagentTypeEnum, Reagent>();

        [JsonConstructor] private Reagents()
        {
        }

        public Reagents(ImportedGameState importedGameState)
        {
            void addReagentLegacy(Reagent.ReagentTypeEnum reagentType) =>
                AddReagent(reagentType, importedGameState.GetReagentQuantity(reagentType));
            
            foreach (Reagent.ReagentTypeEnum reagent in Enum.GetValues(typeof(Reagent.ReagentTypeEnum)))
            {
                addReagentLegacy(reagent);
            }
        }

        private void AddReagent(Reagent.ReagentTypeEnum reagentType, int nQuantity)
        {
            Reagent reagent = new Reagent(reagentType, nQuantity);
            Items[reagentType] = reagent;
        }

        public List<Reagent> GetReagentsForSale(SmallMapReferences.SingleMapReference.Location location)
        {
            List<Reagent> items = new List<Reagent>();
            foreach (Reagent reagent in Items.Values)
            {
                if (!reagent.IsReagentForSale(location)) continue;
                items.Add(reagent);
            }

            return items;
        }
    }
}