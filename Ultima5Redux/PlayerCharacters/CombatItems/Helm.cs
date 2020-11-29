using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Helm : Armour
    {
        public enum HelmEnum { LeatherHelm = 0x21A, ChainCoif = 0x21B, IronHelm = 0x21C, SpikedHelm = 0x21D }

        private const int HELM_SPRITE = 265;

        public HelmEnum HelmType;

        public Helm(HelmEnum helmType, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef,
            int nQuantity)
            : base(equipment, dataOvlRef, nQuantity, (int) helmType, HELM_SPRITE)
        {
            HelmType = helmType;
        }

        public override bool HideQuantity => false;
    }
}