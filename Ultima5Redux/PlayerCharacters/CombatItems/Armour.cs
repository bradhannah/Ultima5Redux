using System.Runtime.Serialization;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public abstract class Armour : CombatItem
    {
        protected Armour(CombatItemReference theCombatItemReference, int nQuantity) : base(theCombatItemReference,
            nQuantity)
        {
        }
    }
}