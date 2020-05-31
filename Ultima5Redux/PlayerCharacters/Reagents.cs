using System;
using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters
{
    public class Reagents : InventoryItems<Reagent.ReagentTypeEnum, Reagent>
    {
        public Reagents(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            int nIndex = 0;
            foreach (Reagent.ReagentTypeEnum reagent in Enum.GetValues(typeof(Reagent.ReagentTypeEnum)))
            {
                AddReagent(reagent, (DataOvlReference.ReagentStrings)nIndex++);
            }
        }

        private void AddReagent(Reagent.ReagentTypeEnum reagentType, DataOvlReference.ReagentStrings reagentStrRef )
        {
            Reagent reagent = new Reagent(reagentType,
                GameStateByteArray[(int)reagentType],
                DataOvlRef.StringReferences.GetString(reagentStrRef),
                DataOvlRef.StringReferences.GetString(reagentStrRef));
            Items[reagentType] = reagent;
        }

        //public override Dictionary<Potion.PotionColor, Potion> Items { get; } = new Dictionary<Potion.PotionColor, Potion>(8);

        public override Dictionary<Reagent.ReagentTypeEnum, Reagent> Items { get; } = new Dictionary<Reagent.ReagentTypeEnum, Reagent>();
    }
}