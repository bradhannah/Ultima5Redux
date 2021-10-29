using System.Collections.Generic;
using Newtonsoft.Json;
using Ultima5Redux.Properties;

namespace Ultima5Redux.PlayerCharacters
{
    public class MagicReferences
    {
        private readonly Dictionary<MagicReference.SpellWords, MagicReference> _magicReferences;
        
        public MagicReferences()
        {
            _magicReferences =
                JsonConvert.DeserializeObject<Dictionary<MagicReference.SpellWords, MagicReference>>(Resources.MagicDefinitions);
        }

        public MagicReference GetMagicReference(MagicReference.SpellWords spellWord)
        {
            return _magicReferences[spellWord];
        }
    }
}