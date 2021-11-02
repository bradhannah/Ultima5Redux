using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    /// <summary>
    ///     All reagents collection
    /// </summary>
    public class Reagents : InventoryItems<Reagent.ReagentTypeEnum, Reagent>
    {
        public Reagents(DataOvlReference dataOvlReference, List<byte> gameStateByteArray, GameState state) : base(gameStateByteArray)
        {
            int nIndex = 0;
            foreach (Reagent.ReagentTypeEnum reagent in Enum.GetValues(typeof(Reagent.ReagentTypeEnum)))
            {
                AddReagent(reagent, (DataOvlReference.ReagentStrings)nIndex++, dataOvlReference, state);
            }
        }

        public override Dictionary<Reagent.ReagentTypeEnum, Reagent> Items { get; } =
            new Dictionary<Reagent.ReagentTypeEnum, Reagent>();

        private void AddReagent(Reagent.ReagentTypeEnum reagentType, DataOvlReference.ReagentStrings reagentStrRef, 
            DataOvlReference dataOvlReference, GameState state)
        {
            Reagent reagent = new Reagent(reagentType,
                GameStateByteArray[(int)reagentType],
                dataOvlReference.StringReferences.GetString(reagentStrRef),
                dataOvlReference.StringReferences.GetString(reagentStrRef), dataOvlReference, state);
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