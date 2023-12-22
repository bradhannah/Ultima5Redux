using System;
using System.Collections.Generic;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.References.Maps {
    public class ShrineReference {
        // SHRINE_X_COORDS
        // SHRINE_Y_COORDS
        public Point2D Position { get; }
        public VirtueReference VirtueRef { get; private set; }

        public class StatAttributeBoosts {
            public int Strength { get; internal set; }
            public int Dexterity { get; internal set; }
            public int Intelligence { get; internal set; }

            public string GetStatBoostString() =>
                ((Strength > 0 ? "Strength +" + Strength + "\n" : string.Empty)
                 + (Dexterity > 0 ? "Dexterity +" + Dexterity + "\n" : string.Empty)
                 + (Intelligence > 0 ? "Intelligence +" + Intelligence : string.Empty)).TrimEnd();
        }

        private readonly Dictionary<VirtueReference.VirtueType, string> _shrineGoToCodexStrings = new() {
            { VirtueReference.VirtueType.Honesty, "failing of Dishonesty!" },
            { VirtueReference.VirtueType.Compassion, "heart of the cruel soul!" },
            { VirtueReference.VirtueType.Valor, "failing of a life without Valour!" },
            { VirtueReference.VirtueType.Justice, "weakness of the Unjust!" },
            { VirtueReference.VirtueType.Sacrifice, "failing of unwilling Sacrifice!" },
            { VirtueReference.VirtueType.Honor, "darkness of Dishonor!" },
            { VirtueReference.VirtueType.Spirituality, "neglect of one's Spirit!" },
            { VirtueReference.VirtueType.Humility, "weakness of a life consumed by Pride!" }
        };


        private readonly Dictionary<VirtueReference.VirtueType, string> _codexWisdoms = new() {
            {
                VirtueReference.VirtueType.Honesty,
                "A dishonest life brings unto thee a temporary gain, but forsakes the permanent."
            },
            { VirtueReference.VirtueType.Compassion, "Only a detested life owes its pleasures to another's pain." }, {
                VirtueReference.VirtueType.Valor,
                "Those who fear to try, know not their limits and thus know not themselves."
            }, {
                VirtueReference.VirtueType.Justice,
                "Those who inflict injustice upon others, cannot expect fair treatment unto themselves."
            }, {
                VirtueReference.VirtueType.Sacrifice,
                "None live alone, save they who will not share their fortune with those around them."
            },
            { VirtueReference.VirtueType.Honor, "It is the guilt, not the guillotine, that constitutes the shame." }, {
                VirtueReference.VirtueType.Spirituality,
                "To forsake one's inner being is to abandon thy hopes for thyself and thy world."
            }, {
                VirtueReference.VirtueType.Humility,
                "Pride is a vice, which Pride itself inclines one to find in others, and overlook in oneself."
            }
        };

        public string GetGoToCodexInstruction() =>
            "'Tis now thy sacred Quest to go unto the Codex and learn of the " +
            _shrineGoToCodexStrings[VirtueRef.Virtue];

        public string GetCodexWisdom() => _codexWisdoms[VirtueRef.Virtue];


        public StatAttributeBoosts TheStatAttributeBoosts { get; } = new();

        internal ShrineReference(VirtueReference virtueRef, Point2D position) {
            VirtueRef = virtueRef;
            Position = position;

            switch (virtueRef.Virtue) {
                case VirtueReference.VirtueType.Honesty:
                    TheStatAttributeBoosts.Intelligence = 1;
                    break;
                case VirtueReference.VirtueType.Compassion:
                    TheStatAttributeBoosts.Dexterity = 1;
                    break;
                case VirtueReference.VirtueType.Valor:
                    TheStatAttributeBoosts.Strength = 1;
                    break;
                case VirtueReference.VirtueType.Justice:
                    TheStatAttributeBoosts.Dexterity = 1;
                    TheStatAttributeBoosts.Intelligence = 1;
                    break;
                case VirtueReference.VirtueType.Sacrifice:
                    TheStatAttributeBoosts.Dexterity = 1;
                    TheStatAttributeBoosts.Strength = 1;
                    break;
                case VirtueReference.VirtueType.Honor:
                    TheStatAttributeBoosts.Strength = 1;
                    TheStatAttributeBoosts.Intelligence = 1;
                    break;
                case VirtueReference.VirtueType.Spirituality:
                    TheStatAttributeBoosts.Dexterity = 1;
                    TheStatAttributeBoosts.Intelligence = 1;
                    TheStatAttributeBoosts.Strength = 1;
                    break;
                case VirtueReference.VirtueType.Humility:
                    // no boost - thou art far too humble to accept a boost
                    break;
                case VirtueReference.VirtueType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void BoostStats(CharacterStats stats) {
            stats.Dexterity += TheStatAttributeBoosts.Dexterity;
            stats.Intelligence += TheStatAttributeBoosts.Intelligence;
            stats.Strength += TheStatAttributeBoosts.Strength;
        }
    }
}