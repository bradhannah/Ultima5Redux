using System;
using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters
{
    public class Spells : InventoryItems<MagicReference.SpellWords, Spell>
    {
        private readonly MagicReferences _magicReferences;

        private static readonly Dictionary<string, string> LiteralTranslationDictionary =
            new Dictionary<string, string>
            {
                { "An", "Negate" },
                { "Bet", "Small" },
                { "Corp", "Death" },
                { "Des", "Down" },
                { "Ex", "Freedom" },
                { "Flam", "Flame" },
                { "Grav", "Energy" },
                { "Hur", "Wind" },
                { "In", "Create" },
                { "Kal", "Invoke" },
                { "Lor", "Light" },
                { "Mani", "Life" },
                { "Nox", "Poison" },
                { "Por", "Movement" },
                { "Quas", "Illusion" },
                { "Rel", "Change" },
                { "Sanct", "Protection" },
                { "Tym", "Time" },
                { "Uus", "Up" },
                { "Vas", "Great" },
                { "Wis", "Knowledge" },
                { "Xen", "Creature" },
                { "Ylem", "Matter" },
                { "Zu", "Sleep" },
            };

        public Spells(List<byte> gameStateByteArray, MagicReferences magicReferences) : base(gameStateByteArray)
        {
            _magicReferences = magicReferences;
            foreach (MagicReference.SpellWords spell in Enum.GetValues(typeof(MagicReference.SpellWords)))
            {
                AddSpell(spell, _magicReferences.GetMagicReference(spell));
            }
        }

        public override Dictionary<MagicReference.SpellWords, Spell> Items { get; } = new Dictionary<MagicReference.SpellWords, Spell>();

        public static string GetSpellWordByChar(string spellCharacter)
        {
            var hey = LiteralTranslationDictionary.Where(sp => sp.Key.StartsWith(spellCharacter.ToUpper()));
            return hey.FirstOrDefault().Key;
        }
        
        private void AddSpell(MagicReference.SpellWords spellWord, MagicReference magicReference)
        {
            if (spellWord == MagicReference.SpellWords.Nox)
            {
                Items[spellWord] = new Spell(spellWord, 0, 
                    //"Nox", "Nox",
                    magicReference);
                return;
            }

            //string longSpellStr = _ti.ToTitleCase(spellStr.ToString().Replace("_", " ").ToLower());

            Items[spellWord] = new Spell(spellWord, 
                GameStateByteArray[(int)spellWord],
                //longSpellStr, longSpellStr, 
                _magicReferences.GetMagicReference(spellWord));
        }

        public static string GetLiteralTranslation(string syllable)
        {
            return LiteralTranslationDictionary[Utils.EnTextInfo.ToTitleCase(syllable)];
        }
    }
}