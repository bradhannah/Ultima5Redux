using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Helm : Armour
    {

        //private const int HELM_SPRITE = 265;

        //public HelmEnum HelmType;

        // public Helm(HelmEnum helmType, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef,
        //     int nQuantity)
        //     : base(equipment, dataOvlRef, nQuantity, (int)helmType, HELM_SPRITE)
        // {
        //     HelmType = helmType;
        // }
        
        public Helm(CombatItemReference combatItemReference, int nQuantity) :
            base(combatItemReference, nQuantity)
        {
            
        }        
        
        public override PlayerCharacterRecord.CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            PlayerCharacterRecord.CharacterEquipped.EquippableSlot.Helm;

        public override bool HideQuantity => false;
    }
}