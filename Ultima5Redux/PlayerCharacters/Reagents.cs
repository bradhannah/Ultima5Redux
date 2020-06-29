﻿using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.PlayerCharacters
{
    public class Reagents : InventoryItems<Reagent.ReagentTypeEnum, Reagent>
    {
        /// <summary>
        /// the exact locations and specific order of the shoppe keeper locations
        /// </summary>
        private readonly List<SmallMapReferences.SingleMapReference.Location> _reagentShoppeKeeperLocations =
            new List<SmallMapReferences.SingleMapReference.Location>()
            {
                SmallMapReferences.SingleMapReference.Location.Moonglow,
                SmallMapReferences.SingleMapReference.Location.Yew,
                SmallMapReferences.SingleMapReference.Location.Skara_Brae,
                SmallMapReferences.SingleMapReference.Location.Cove,
                SmallMapReferences.SingleMapReference.Location.Lycaeum
            };
        
        public Reagents(DataOvlReference dataOvlRef, List<byte> gameStateByteArray, GameState state) : base(dataOvlRef, gameStateByteArray)
        {
            int nIndex = 0;
            foreach (Reagent.ReagentTypeEnum reagent in Enum.GetValues(typeof(Reagent.ReagentTypeEnum)))
            {
                AddReagent(reagent, (DataOvlReference.ReagentStrings)nIndex++, dataOvlRef, state);
            }
        }

        private void AddReagent(Reagent.ReagentTypeEnum reagentType, DataOvlReference.ReagentStrings reagentStrRef,
            DataOvlReference dataOvlRef, GameState state)
        {
            Reagent reagent = new Reagent(reagentType,
                GameStateByteArray[(int)reagentType],
                DataOvlRef.StringReferences.GetString(reagentStrRef),
                DataOvlRef.StringReferences.GetString(reagentStrRef), dataOvlRef, state, 
                _reagentShoppeKeeperLocations);
            Items[reagentType] = reagent;
        }

        public bool DoesLocationSellReagents(SmallMapReferences.SingleMapReference.Location location)
        {
            return _reagentShoppeKeeperLocations.Contains(location);
        }
        
        public List<Reagent> GetReagentsForSale(SmallMapReferences.SingleMapReference.Location location)
        {
            if (!DoesLocationSellReagents(location))
            {
                throw new Ultima5ReduxException("Asked for reagents in a location that doesn't sell them: "+location);
            }

            List<Reagent> items = new List<Reagent>();
            foreach (Reagent reagent in Items.Values)
            {
                if (!reagent.IsReagentForSale(location)) continue;
                items.Add(reagent);
            }

            return items;
        }
        
        public override Dictionary<Reagent.ReagentTypeEnum, Reagent> Items { get; } = new Dictionary<Reagent.ReagentTypeEnum, Reagent>();
    }
}