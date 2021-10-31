using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Amulet : Armour
    {

        //private const int AMULET_SPRITE = 268;
        //public AmuletEnum AmuletType;

        // public Amulet(AmuletEnum amuletType, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef,
        //     int nQuantity)
        //     : base(equipment, dataOvlRef, nQuantity, (int)amuletType, AMULET_SPRITE)
        // {
        //     AmuletType = amuletType;
        // }

        public Amulet(CombatItemReference combatItemReference, int nQuantity) :
            base(combatItemReference, nQuantity)
        {
            
        }        

        public override bool HideQuantity => false;

        public override PlayerCharacterRecord.CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            PlayerCharacterRecord.CharacterEquipped.EquippableSlot.Amulet;
    }
}