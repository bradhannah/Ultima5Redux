using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Amulet : Armour
    {
        public enum AmuletEnum { AmuletTurning = 0x247, SpikeCollar = 0x248, Ankh = 0x249 }

        private const int AMULET_SPRITE = 268;
        public AmuletEnum AmuletType;

        public Amulet(AmuletEnum amuletType, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef,
            int nQuantity)
            : base(equipment, dataOvlRef, nQuantity, (int)amuletType, AMULET_SPRITE)
        {
            AmuletType = amuletType;
        }

        public override bool HideQuantity => false;

        public override PlayerCharacterRecord.CharacterEquipped.EquippableSlot EquippableSlot { get; } =
            PlayerCharacterRecord.CharacterEquipped.EquippableSlot.Amulet;
    }
}