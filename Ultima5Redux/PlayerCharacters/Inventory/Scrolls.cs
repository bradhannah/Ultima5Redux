using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Scrolls : InventoryItems<MagicReference.SpellWords, Scroll>
    {
        [DataMember]
        public override Dictionary<MagicReference.SpellWords, Scroll> Items { get; internal set; } = new(8);

        [JsonConstructor] private Scrolls()
        {
        }

        public Scrolls(ImportedGameState importedGameState)
        {
            void addScrollLegacy(MagicReference.SpellWords spellWord) =>
                AddScroll(spellWord, importedGameState.GetScrollQuantity(spellWord));

            addScrollLegacy(MagicReference.SpellWords.Vas_Lor);
            addScrollLegacy(MagicReference.SpellWords.Rel_Hur);
            addScrollLegacy(MagicReference.SpellWords.In_Sanct);
            addScrollLegacy(MagicReference.SpellWords.In_An);
            addScrollLegacy(MagicReference.SpellWords.In_Quas_Wis);
            addScrollLegacy(MagicReference.SpellWords.Kal_Xen_Corp);
            addScrollLegacy(MagicReference.SpellWords.In_Mani_Corp);
            addScrollLegacy(MagicReference.SpellWords.An_Tym);
        }

        private void AddScroll(MagicReference.SpellWords spellWord, int nQuantity)
        {
            Items[spellWord] = new Scroll(spellWord, nQuantity,
                GameReferences.MagicRefs.GetMagicReference(spellWord));
        }
    }
}