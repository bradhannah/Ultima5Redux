using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Maps;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    /// <summary>
    ///     All reagents collection
    /// </summary>
    [DataContract] public class Reagents : InventoryItems<Reagent.ReagentTypeEnum, Reagent>
    {
        [DataMember] public override Dictionary<Reagent.ReagentTypeEnum, Reagent> Items { get; } =
            new Dictionary<Reagent.ReagentTypeEnum, Reagent>();

        [JsonConstructor] private Reagents()
        {
        }
        
        public Reagents(List<byte> gameStateByteArray, GameState state) : base(gameStateByteArray)
        {
            foreach (Reagent.ReagentTypeEnum reagent in Enum.GetValues(typeof(Reagent.ReagentTypeEnum)))
            {
                AddReagent(reagent, state);
            }
        }

        private void AddReagent(Reagent.ReagentTypeEnum reagentType, GameState state)
        {
            Reagent reagent = new Reagent(reagentType, GameStateByteArray[(int)reagentType], state);
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