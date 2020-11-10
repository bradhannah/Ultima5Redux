using System;
using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Scrolls : InventoryItems<Spell.SpellWords, Scroll>
    {
        // we apply an offset into the save game file by the number of spells since scrolls come immediatelly after
        // I wouldn't normally like to use offsets like this, but I want spells and scrolls to be linkable by the same enum
        private readonly int _nQuantityIndexAdjust = Enum.GetValues(typeof(Spell.SpellWords)).Length;

        public Scrolls(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef,
            gameStateByteArray)
        {
            AddScroll(Spell.SpellWords.Vas_Lor, DataOvlReference.SpellStrings.VAS_LOR);
            AddScroll(Spell.SpellWords.Rel_Hur, DataOvlReference.SpellStrings.REL_HUR);
            AddScroll(Spell.SpellWords.In_Sanct, DataOvlReference.SpellStrings.IN_SANCT);
            AddScroll(Spell.SpellWords.In_An, DataOvlReference.SpellStrings.IN_AN);
            AddScroll(Spell.SpellWords.In_Quas_Wis, DataOvlReference.SpellStrings.IN_QUAS_WIS);
            AddScroll(Spell.SpellWords.Kal_Xen_Corp, DataOvlReference.SpellStrings.KAL_XEN_CORP);
            AddScroll(Spell.SpellWords.In_Mani_Corp, DataOvlReference.SpellStrings.IN_MANI_CORP);
            AddScroll(Spell.SpellWords.An_Tym, DataOvlReference.SpellStrings.AN_TYM);
        }

        public override Dictionary<Spell.SpellWords, Scroll> Items { get; } =
            new Dictionary<Spell.SpellWords, Scroll>(8);

        private void AddScroll(Spell.SpellWords spellWord, DataOvlReference.SpellStrings spellStr)
        {
            Scroll.ScrollSpells scrollSpell =
                (Scroll.ScrollSpells) Enum.Parse(typeof(Scroll.ScrollSpells), spellWord.ToString());

            int nIndex = 0x27A + (int) scrollSpell;
            Items[spellWord] = new Scroll(spellWord, GameStateByteArray[nIndex],
                DataOvlRef.StringReferences.GetString(spellStr),
                DataOvlRef.StringReferences.GetString(spellStr));
        }
    }
}