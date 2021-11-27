using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters
{
    public class MemorizedSpells
    {
        private readonly Dictionary<MagicReference.SpellWords, bool> _knownSpells =
            new Dictionary<MagicReference.SpellWords, bool>();

        [JsonConstructor] public MemorizedSpells()
        {
            // start with a basic list of spells no matter what
            LearnSpell(MagicReference.SpellWords.An_Nox);
            LearnSpell(MagicReference.SpellWords.Mani);
            LearnSpell(MagicReference.SpellWords.An_Zu);
            LearnSpell(MagicReference.SpellWords.Grav_Por);
            LearnSpell(MagicReference.SpellWords.In_Lor);
        }

        public void LearnSpell(MagicReference.SpellWords spell)
        {
            if (_knownSpells.ContainsKey(spell))
            {
                Debug.Assert(_knownSpells[spell]);
                return;
            }

            _knownSpells.Add(spell, true);
        }

        public bool IsSpellKnown(MagicReference.SpellWords spellWord) => _knownSpells.ContainsKey(spellWord);

        public List<MagicReference.SpellWords> ListOfKnownSpells => _knownSpells.Keys.ToList();

    }
}