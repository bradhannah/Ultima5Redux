using System;
using System.Collections.Generic;
using System.Globalization;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters
{
    public class Spells : InventoryItems<Spell.SpellWords, Spell>
    {
        private static readonly TextInfo _ti = new CultureInfo("en-US", false).TextInfo;

        private static readonly Dictionary<string, string> _literalTranslationDictionary =
            new Dictionary<string, string>
            {
                {"An", "Negate"},
                {"Bet", "Small"},
                {"Corp", "Death"},
                {"Des", "Down"},
                {"Ex", "Freedom"},
                {"Flam", "Flame"},
                {"Grav", "Energy"},
                {"Hur", "Wind"},
                {"In", "Create"},
                {"Kal", "Invoke"},
                {"Lor", "Light"},
                {"Mani", "Life"},
                {"Nox", "Poison"},
                {"Por", "Movement"},
                {"Quas", "Illusion"},
                {"Rel", "Change"},
                {"Sanct", "Protection"},
                {"Tym", "Time"},
                {"Uus", "Up"},
                {"Vas", "Great"},
                {"Wis", "Knowledge"},
                {"Xen", "Creature"},
                {"Ylem", "Matter"},
                {"Zu", "Sleep"},
            };

        public Spells(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            int nIndex = 0;
            foreach (Spell.SpellWords spell in Enum.GetValues(typeof(Spell.SpellWords)))
            {
                AddSpell(spell, (DataOvlReference.SpellStrings) nIndex++);
            }
        }

        public override Dictionary<Spell.SpellWords, Spell> Items { get; } = new Dictionary<Spell.SpellWords, Spell>();

        private void AddSpell(Spell.SpellWords spellWord, DataOvlReference.SpellStrings spellStr)
        {
            if (spellWord == Spell.SpellWords.Nox)
            {
                Items[spellWord] = new Spell(spellWord, 0, "Nox", "Nox");
                return;
            }
            Items[spellWord] = new Spell(spellWord, GameStateByteArray[(int) spellWord],
                DataOvlRef.StringReferences.GetString(spellStr),
                DataOvlRef.StringReferences.GetString(spellStr));
        }

        public static string GetLiteralTranslation(string syllable)
        {
            return _literalTranslationDictionary[_ti.ToTitleCase(syllable)];
        }
    }
}