using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.References;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Moonstones : InventoryItems<MoonPhaseReferences.MoonPhases, Moonstone>
    {
        public sealed override Dictionary<MoonPhaseReferences.MoonPhases, Moonstone> Items { get; } =
            new Dictionary<MoonPhaseReferences.MoonPhases, Moonstone>();

        public Moonstones(Moongates moongates) : base(null)
        {
            // go through each of the moon phases one by one and create a moonstone
            foreach (MoonPhaseReferences.MoonPhases phase in Enum.GetValues(typeof(MoonPhaseReferences.MoonPhases)))
            {
                // there is no "no moon" moonstone
                if (phase == MoonPhaseReferences.MoonPhases.NoMoon) continue;
                Items[phase] = new Moonstone(phase,
                    GameReferences.DataOvlRef.StringReferences
                        .GetString(DataOvlReference.ThingsIFindStrings.A_STRANGE_ROCK_BANG_N).TrimEnd(), moongates);
            }
        }
    }
}