using System;
using System.Collections.Generic;

namespace Ultima5Redux.References
{
    public sealed class VirtueReference
    {
        public VirtueType Virtue { get; }
        public MantraType Mantra { get; }
        public WordOfPowerType WordOfPower { get; }
        
        public enum VirtueType
        {
            Honesty = 0, Compassion = 1, Valor = 2, Justice = 3, Sacrifice = 4, Honor = 5, Spirituality = 6,
            Humility = 7, None = -1
        }

        public enum MantraType { Ahm = 0, Mu = 1, Ra = 2, Beh = 3, Cah = 4, Summ = 5, Om = 6, Lumm = 7 }

        public enum WordOfPowerType
        {
            Fallax = 0, Vilis = 1, Inopia = 2, Malum = 3, Avidus = 4, Infama = 5, Ignavus = 6, Veramocor = 7
        }

        public VirtueReference(VirtueType virtueType) {
            Virtue = virtueType;
            Mantra = (MantraType)virtueType;
            WordOfPower = (WordOfPowerType)virtueType;
        }
    }

    public class VirtueReferences
    {
        //Honesty	Ahm	Fallax	Deceit
        // Compassion	Mu	Vilis	Despise
        // Valor	Ra	Inopia	Destard
        // Justice	Beh	Malum	Wrong
        // Sacrifice	Cah	Avidus	Covetous
        // Honor	Summ	Infama	Shame
        // Spirituality	Om	Ignavus	Hylothe
        // Humility	Lumm	Veramocor	Doom
        private readonly Dictionary<VirtueReference.VirtueType, VirtueReference> _virtues = new();

        public IEnumerable<VirtueReference> Virtues => _virtues.Values;

        public VirtueReference GetVirtue(VirtueReference.VirtueType virtueType) =>
            _virtues.ContainsKey(virtueType) ? _virtues[virtueType] : null;

        public VirtueReferences() {
            foreach (VirtueReference.VirtueType virtueType in Enum.GetValues(typeof(VirtueReference.VirtueType))) {
                if (virtueType == VirtueReference.VirtueType.None) continue;
                _virtues.Add(virtueType, new VirtueReference(virtueType));
            }

            _ = "";
        }
    }
}