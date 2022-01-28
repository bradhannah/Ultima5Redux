using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Ultima5Redux.Properties;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    public class MagicReferences
    {
        private readonly Dictionary<MagicReference.SpellWords, MagicReference> _magicReferences;

        public MagicReferences()
        {
            _magicReferences =
                JsonConvert.DeserializeObject<Dictionary<MagicReference.SpellWords, MagicReference>>(Resources
                    .MagicDefinitions);
            if (_magicReferences == null)
                throw new Ultima5ReduxException("Magic references were not loaded properly");

            foreach (MagicReference.SpellWords spellWords in _magicReferences.Keys)
            {
                _magicReferences[spellWords].SpellEnum = spellWords;
            }
        }

        public MagicReference GetMagicReference(MagicReference.SpellWords spellWords)
        {
            if (!_magicReferences.ContainsKey(spellWords))
                throw new Ultima5ReduxException("Bad spell words: " + spellWords);

            return _magicReferences[spellWords];
        }

        public MagicReference GetMagicReference(string spellWords)
        {
            string cleanSpellWords = spellWords.ToLower().Trim().Replace("_", " ");
            return _magicReferences.Values.FirstOrDefault(magicReference =>
                magicReference.Spell.Trim().ToLower()
                == cleanSpellWords);
        }
    }
}