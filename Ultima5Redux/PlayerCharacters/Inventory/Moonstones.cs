using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class Moonstones : InventoryItems<MoonPhaseReferences.MoonPhases, Moonstone>
    {
        [DataMember]
        public override Dictionary<MoonPhaseReferences.MoonPhases, Moonstone> Items { get; internal set; } =
            new();

        [JsonConstructor] public Moonstones()
        {
            // if Items is already loaded then we likely built from JSON Deserialize
            if (Items.Count > 0) return;

            // go through each of the moon phases one by one and create a moonstone
            foreach (MoonPhaseReferences.MoonPhases phase in Enum.GetValues(typeof(MoonPhaseReferences.MoonPhases)))
            {
                // there is no "no moon" moonstone
                if (phase == MoonPhaseReferences.MoonPhases.NoMoon) continue;
                Items[phase] = new Moonstone(phase);
            }
        }
    }
}