using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Ring : Armour
    {

        // private const int RING_SPRITE = 266;
        // public RingEnum RingType;

        // public Ring(RingEnum ringType, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef,
        //     int nQuantity)
        //     : base(equipment, dataOvlRef, nQuantity, (int)ringType, RING_SPRITE)
        // {
        //     RingType = ringType;
        // }
        
        public Ring(CombatItemReference combatItemReference, int nQuantity) :
            base(combatItemReference, nQuantity)
        {
            
        }        
        public override PlayerCharacterRecord.CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            PlayerCharacterRecord.CharacterEquipped.EquippableSlot.Ring;

        public override bool HideQuantity => false;
    }
}