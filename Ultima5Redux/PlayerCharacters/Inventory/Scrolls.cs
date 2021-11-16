using System;
using System.Collections.Generic;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Scrolls : InventoryItems<MagicReference.SpellWords, Scroll>
    {
        private readonly MagicReferences _magicReferences;

        public Scrolls(List<byte> gameStateByteArray, MagicReferences magicReferences) : base(gameStateByteArray)
        {
            _magicReferences = magicReferences;
            AddScroll(MagicReference.SpellWords.Vas_Lor);
            AddScroll(MagicReference.SpellWords.Rel_Hur);
            AddScroll(MagicReference.SpellWords.In_Sanct);
            AddScroll(MagicReference.SpellWords.In_An);
            AddScroll(MagicReference.SpellWords.In_Quas_Wis);
            AddScroll(MagicReference.SpellWords.Kal_Xen_Corp);
            AddScroll(MagicReference.SpellWords.In_Mani_Corp);
            AddScroll(MagicReference.SpellWords.An_Tym);
        }

        public override Dictionary<MagicReference.SpellWords, Scroll> Items { get; } =
            new Dictionary<MagicReference.SpellWords, Scroll>(8);

        private void AddScroll(MagicReference.SpellWords spellWord)
        {
            Scroll.ScrollSpells scrollSpell =
                (Scroll.ScrollSpells)Enum.Parse(typeof(Scroll.ScrollSpells), spellWord.ToString());

            
            
            int nIndex = 0x27A + (int)scrollSpell;
            Items[spellWord] = new Scroll(spellWord, GameStateByteArray[nIndex], 
                _magicReferences.GetMagicReference(spellWord));
        }
    }
}