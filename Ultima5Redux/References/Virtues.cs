using System;
using System.Collections.Generic;

namespace Ultima5Redux.References
{
    public sealed class VirtueReference
    {
        public VirtueType Virtue { get; }
        public MantraType Mantra => (MantraType)Virtue;
        public WordOfPowerType WordOfPower => (WordOfPowerType)Virtue;
        
        public enum VirtueType
        {
            Honesty = 0, Compassion = 1, Valor = 2, Justice = 3, Sacrifice = 4, Honor = 5, Spirituality = 6,
            Humility = 7, None = -1
        }

        public enum MantraType { Ahm = 0, Mu = 1, Ra = 2, Beh = 3, Cah = 4, Summ = 5, Om = 6, Lumm = 7, None = -1 }

        public enum WordOfPowerType
        {
            Fallax = 0, Vilis = 1, Inopia = 2, Malum = 3, Avidus = 4, Infama = 5, Ignavus = 6, Veramocor = 7
        }

        public string CodexWisdom =>
            Virtue switch {
                VirtueType.Honesty =>
                    "A dishonest life brings unto thee temporary gain, but forsakes the permanent.\n",
                VirtueType.Compassion => "Only a detested life owes its pleasures to another's pain.\n",
                VirtueType.Valor => "Those who fear to try know not their limits, and thus know not themselves.\n",
                VirtueType.Justice =>
                    "Those who inflict injustice upon others cannot expect fair treatment unto themselves.\n",
                VirtueType.Sacrifice =>
                    "None live alone, save they who will not share their fortune with those around them.\n",
                VirtueType.Honor => "It is the guilt, not the guillotine, that constitutes the shame.\n",
                VirtueType.Spirituality =>
                    "To forsake one's inner being is to abandon thy hopes for thyself and thy world.\n",
                VirtueType.Humility =>
                    "Pride is a vice which pride itself inclines one to find in others, and overlook in oneself.\n",
                VirtueType.None => "",
                _ => throw new ArgumentOutOfRangeException()
            };


        public VirtueReference(VirtueType virtueType) {
            Virtue = virtueType;
        }

        public bool IsCorrectMantra(string userInput) =>
            string.Equals(userInput, Mantra.ToString(), StringComparison.CurrentCultureIgnoreCase);

        public bool DoesVirtueMatch(string userInput) =>
            string.Equals(userInput, Virtue.ToString(), StringComparison.CurrentCultureIgnoreCase);
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