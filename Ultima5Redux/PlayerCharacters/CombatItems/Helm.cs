namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Helm : Armour
    {
        public override CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            CharacterEquipped.EquippableSlot.Helm;

        public override bool HideQuantity => false;

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
    }
}