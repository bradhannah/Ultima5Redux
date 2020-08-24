using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Moonstones :  InventoryItems <MoonPhaseReferences.MoonPhases, Moonstone>
    {
        private Moongates _moongates;
        private MoonPhaseReferences _moonPhaseReferences;
        
        public Moonstones(DataOvlReference dataOvlRef, MoonPhaseReferences moonPhaseReferences, Moongates moongates) 
            : base(dataOvlRef, null)
        {
            _moongates = moongates;
            _moonPhaseReferences = moonPhaseReferences;
            
            // go through each of the moon phases one by one and create a moonstone
            foreach (MoonPhaseReferences.MoonPhases phase in Enum.GetValues(typeof(MoonPhaseReferences.MoonPhases)))
            {
                // there is no "no moon" moonstone
                if (phase == MoonPhaseReferences.MoonPhases.NoMoon) continue;
                Items[phase] = new Moonstone(phase,
                    dataOvlRef.StringReferences.GetString(DataOvlReference.ZstatsStrings.MOONSTONE_SPACE).TrimEnd(),
                    dataOvlRef.StringReferences.GetString(DataOvlReference.ZstatsStrings.MOONSTONE_SPACE).TrimEnd(),
                    dataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings.A_STRANGE_ROCK_BANG_N)
                        .TrimEnd(), moongates, null);
                //invRefs.GetInventoryReference(InventoryReferences.InventoryReferenceType.Item, phase.ToString()));
            }
        }

        public sealed override Dictionary<MoonPhaseReferences.MoonPhases, Moonstone> Items { get; } = 
            new Dictionary<MoonPhaseReferences.MoonPhases, Moonstone>();
    }
}