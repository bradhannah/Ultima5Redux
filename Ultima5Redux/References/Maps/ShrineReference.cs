using System;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.References.Maps
{
    public class ShrineReference
    {
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