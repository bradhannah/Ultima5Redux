using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public abstract class Armour : CombatItem
    {

        [JsonConstructor] protected Armour()
        {
        }

        protected Armour(CombatItemReference theCombatItemReference, int nQuantity) : base(theCombatItemReference,
            nQuantity)
        {
        }
    }
}