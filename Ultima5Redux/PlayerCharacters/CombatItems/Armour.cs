using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
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

        public ArmourReference GetArmourRef()
        {
            if (TheCombatItemReference is ArmourReference)
            {
                return GameReferences.Instance.CombatItemRefs.GetArmourReferenceFromEquipment(SpecificEquipment);
            }

            throw new Ultima5ReduxException("Tried to get armour reference with equipment " + SpecificEquipment);
        }
    }
}