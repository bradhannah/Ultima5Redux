using System;
using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Scrolls : InventoryItems<MagicReference.SpellWords, Scroll>
    {
        private readonly MagicReferences _magicReferences;

        // we apply an offset into the save game file by the number of spells since scrolls come immediately after
        // I wouldn't normally like to use offsets like this, but I want spells and scrolls to be linkable by the same enum
        private readonly int _nQuantityIndexAdjust = Enum.GetValues(typeof(MagicReference.SpellWords)).Length;

        public Scrolls(List<byte> gameStateByteArray, MagicReferences magicReferences) : base(gameStateByteArray)
        {
            _magicReferences = magicReferences;
            AddScroll(MagicReference.SpellWords.Vas_Lor, DataOvlReference.SpellStrings.VAS_LOR);
            AddScroll(MagicReference.SpellWords.Rel_Hur, DataOvlReference.SpellStrings.REL_HUR);
            AddScroll(MagicReference.SpellWords.In_Sanct, DataOvlReference.SpellStrings.IN_SANCT);
            AddScroll(MagicReference.SpellWords.In_An, DataOvlReference.SpellStrings.IN_AN);
            AddScroll(MagicReference.SpellWords.In_Quas_Wis, DataOvlReference.SpellStrings.IN_QUAS_WIS);
            AddScroll(MagicReference.SpellWords.Kal_Xen_Corp, DataOvlReference.SpellStrings.KAL_XEN_CORP);
            AddScroll(MagicReference.SpellWords.In_Mani_Corp, DataOvlReference.SpellStrings.IN_MANI_CORP);
            AddScroll(MagicReference.SpellWords.An_Tym, DataOvlReference.SpellStrings.AN_TYM);
        }

        public override Dictionary<MagicReference.SpellWords, Scroll> Items { get; } =
            new Dictionary<MagicReference.SpellWords, Scroll>(8);

        private void AddScroll(MagicReference.SpellWords spellWord, DataOvlReference.SpellStrings spellStr)
        {
            Scroll.ScrollSpells scrollSpell =
                (Scroll.ScrollSpells)Enum.Parse(typeof(Scroll.ScrollSpells), spellWord.ToString());

            
            
            int nIndex = 0x27A + (int)scrollSpell;
            Items[spellWord] = new Scroll(spellWord, GameStateByteArray[nIndex],
                _magicReferences.GetMagicReference(spellWord).Spell,
                _magicReferences.GetMagicReference(spellWord).Spell, 
                _magicReferences.GetMagicReference(spellWord));
        }
    }
}