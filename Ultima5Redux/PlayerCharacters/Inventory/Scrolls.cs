using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Scrolls : InventoryItems<MagicReference.SpellWords, Scroll>
    {
        [DataMember] public override Dictionary<MagicReference.SpellWords, Scroll> Items { get; internal set; } =
            new Dictionary<MagicReference.SpellWords, Scroll>(8);

        [JsonConstructor] private Scrolls()
        {
        }
        
        public Scrolls(List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
            AddScroll(MagicReference.SpellWords.Vas_Lor);
            AddScroll(MagicReference.SpellWords.Rel_Hur);
            AddScroll(MagicReference.SpellWords.In_Sanct);
            AddScroll(MagicReference.SpellWords.In_An);
            AddScroll(MagicReference.SpellWords.In_Quas_Wis);
            AddScroll(MagicReference.SpellWords.Kal_Xen_Corp);
            AddScroll(MagicReference.SpellWords.In_Mani_Corp);
            AddScroll(MagicReference.SpellWords.An_Tym);
        }

        private void AddScroll(MagicReference.SpellWords spellWord)
        {
            Scroll.ScrollSpells scrollSpell =
                (Scroll.ScrollSpells)Enum.Parse(typeof(Scroll.ScrollSpells), spellWord.ToString());

            int nIndex = 0x27A + (int)scrollSpell;
            Items[spellWord] = new Scroll(spellWord, GameStateByteArray[nIndex],
                GameReferences.MagicRefs.GetMagicReference(spellWord));
        }
    }
}