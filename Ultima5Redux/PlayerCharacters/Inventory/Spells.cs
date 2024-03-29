﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Spells : InventoryItems<MagicReference.SpellWords, Spell>
    {
        [IgnoreDataMember] private static readonly Dictionary<string, string> LiteralTranslationDictionary = new()
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
            { "Zu", "Sleep" }
        };

        [DataMember] public override Dictionary<MagicReference.SpellWords, Spell> Items { get; internal set; } = new();

        [IgnoreDataMember]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public IEnumerable<Spell> SortedSpellsByCircleAndAlpha =>
            Items.Values.OrderBy(s => s.MinCircle).ThenBy(s => s.LongName);

        [JsonConstructor] private Spells()
        {
        }

        public Spells(ImportedGameState importedGameState)
        {
            foreach (MagicReference.SpellWords spell in Enum.GetValues(typeof(MagicReference.SpellWords)))
            {
                AddSpell(spell, importedGameState.GetSpellQuantity(spell));
            }
        }

        private void AddSpell(MagicReference.SpellWords spellWord, int nQuantity)
        {
            if (spellWord == MagicReference.SpellWords.Nox)
            {
                Items[spellWord] = new Spell(spellWord, 0);
                return;
            }

            Items[spellWord] = new Spell(spellWord, nQuantity);
        }

        public static string GetLiteralTranslation(string syllable) =>
            LiteralTranslationDictionary[Utils.EnTextInfo.ToTitleCase(syllable)];

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public static string GetSpellWordByChar(string spellCharacter)
        {
            IEnumerable<KeyValuePair<string, string>> hey =
                LiteralTranslationDictionary.Where(sp => sp.Key.StartsWith(spellCharacter.ToUpper()));
            return hey.FirstOrDefault().Key;
        }
    }
}