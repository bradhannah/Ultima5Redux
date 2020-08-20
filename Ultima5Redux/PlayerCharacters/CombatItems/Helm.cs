using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters
{
    public class Helm : Armour
    {
        public enum HelmEnum { LeatherHelm = 0x21A, ChainCoif = 0x21B, IronHelm = 0x21C, SpikedHelm = 0x21D }

        public HelmEnum HelmType;

        private const int HELM_SPRITE = 265;

        public override bool HideQuantity => false;
        public Helm(HelmEnum helmType, DataOvlReference.Equipment equipment, DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
            : base(equipment, dataOvlRef, gameStateByteArray, (int)helmType, HELM_SPRITE)
        {
            HelmType = helmType;
        }
    }
}