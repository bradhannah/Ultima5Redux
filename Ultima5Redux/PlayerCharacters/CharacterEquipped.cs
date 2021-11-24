using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References;

namespace Ultima5Redux.PlayerCharacters
{
    [DataContract] public class CharacterEquipped
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum EquippableSlot
        {
            None, Helm, Amulet, LeftHand, RightHand, Ring, Armour
        }

        [DataMember] public DataOvlReference.Equipment Amulet { get; set; }
        [DataMember] public DataOvlReference.Equipment Armour { get; set; }
        [DataMember] public DataOvlReference.Equipment Helmet { get; set; }
        [DataMember] public DataOvlReference.Equipment LeftHand { get; set; }
        [DataMember] public DataOvlReference.Equipment RightHand { get; set; }
        [DataMember] public DataOvlReference.Equipment Ring { get; set; }

        public CharacterEquipped()
        {
            Helmet = DataOvlReference.Equipment.Nothing;
            Armour = DataOvlReference.Equipment.Nothing;
            LeftHand = DataOvlReference.Equipment.Nothing;
            RightHand = DataOvlReference.Equipment.Nothing;
            Ring = DataOvlReference.Equipment.Nothing;
            Amulet = DataOvlReference.Equipment.Nothing;
        }

        public DataOvlReference.Equipment GetEquippedEquipment(EquippableSlot equippableSlot)
        {
            return equippableSlot switch
            {
                EquippableSlot.None => DataOvlReference.Equipment.Nothing,
                EquippableSlot.Helm => Helmet,
                EquippableSlot.Amulet => Amulet,
                EquippableSlot.LeftHand => LeftHand,
                EquippableSlot.RightHand => RightHand,
                EquippableSlot.Ring => Ring,
                EquippableSlot.Armour => Armour,
                _ => throw new ArgumentOutOfRangeException(nameof(equippableSlot), equippableSlot, null)
            };
        }

        public bool IsEquipped(EquippableSlot equippableSlot) =>
            GetEquippedEquipment(equippableSlot) != DataOvlReference.Equipment.Nothing;

        public bool IsEquipped(DataOvlReference.Equipment equipment)
        {
            return Helmet == equipment || Armour == equipment || LeftHand == equipment || RightHand == equipment ||
                   Ring == equipment || Amulet == equipment;
        }

        internal void SetEquippableSlot(EquippableSlot equippableSlot, DataOvlReference.Equipment equipment)
        {
            switch (equippableSlot)
            {
                case EquippableSlot.None:
                    break;
                case EquippableSlot.Helm:
                    Helmet = equipment;
                    break;
                case EquippableSlot.Amulet:
                    Amulet = equipment;
                    break;
                case EquippableSlot.LeftHand:
                    LeftHand = equipment;
                    break;
                case EquippableSlot.RightHand:
                    RightHand = equipment;
                    break;
                case EquippableSlot.Ring:
                    Ring = equipment;
                    break;
                case EquippableSlot.Armour:
                    Armour = equipment;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(equippableSlot), equippableSlot, null);
            }
        }

        internal void UnequipEquippableSlot(EquippableSlot equippableSlot)
        {
            SetEquippableSlot(equippableSlot, DataOvlReference.Equipment.Nothing);
        }
    }
}