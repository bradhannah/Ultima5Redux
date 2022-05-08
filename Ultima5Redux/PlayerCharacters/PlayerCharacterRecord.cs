using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.PlayerCharacters
{
    public sealed class PlayerCharacterRecord
    {
        private enum CharacterRecordOffsets
        {
            Name = 0x00, Gender = 0x09, Class = 0x0A, Status = 0x0B, Strength = 0x0C, Dexterity = 0x0D,
            Intelligence = 0x0E, CurrentMP = 0x0F, CurrentHP = 0x10, MaximumHP = 0x12, ExperiencePoints = 0x14,
            Level = 0x16, MonthsSinceStayingAtInn = 0x17, Unknown2 = 0x18, Helmet = 0x19, Armor = 0x1A, Weapon = 0x1B,
            Shield = 0x1C, Ring = 0x1D, Amulet = 0x1E, InnParty = 0x1F
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum CharacterClass { Avatar = 'A', Bard = 'B', Fighter = 'F', Mage = 'M' }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum CharacterGender { Male = 0x0B, Female = 0x0C }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum CharacterPartyStatus
        {
            InTheParty = 0x00, HasntJoinedYet = 0xFF, AtTheInn = 0x01
        } // otherwise it is at an inn at Settlement # in byte value

        [JsonConverter(typeof(StringEnumConverter))]
        public enum CharacterStatus { Good = 'G', Poisoned = 'P', Charmed = 'C', Asleep = 'S', Dead = 'D' }

        public enum EquipResult { Success, SuccessUnequipRight, SuccessUnequipLeft, TooHeavy, Error }

        internal const byte CHARACTER_RECORD_BYTE_ARRAY_SIZE = 0x20;
        [DataMember] private byte InnOrParty { get; set; }

        [DataMember] private byte Unknown2 { get; set; }

        [DataMember] public CharacterClass Class { get; set; }

        [DataMember] public CharacterEquipped Equipped { get; private set; } = new();
        [DataMember] public CharacterGender Gender { get; set; }
        [DataMember] public bool IsInvisible { get; private set; }
        [DataMember] public bool IsRat { get; private set; }

        [DataMember]
        public byte MonthsSinceStayingAtInn
        {
            get => _monthsSinceStayingAtInn;
            set => _monthsSinceStayingAtInn = Math.Min(value, byte.MaxValue);
        }

        [DataMember] public string Name { get; set; }

        [DataMember]
        public CharacterPartyStatus PartyStatus
        {
            get =>
                InnOrParty switch
                {
                    0x00 => CharacterPartyStatus.InTheParty,
                    0xFF => CharacterPartyStatus.HasntJoinedYet,
                    _ => CharacterPartyStatus.AtTheInn
                };
            set => InnOrParty = (byte)value;
        }

        [DataMember] public CharacterStats Stats { get; internal set; } = new();

        [IgnoreDataMember] private byte _monthsSinceStayingAtInn;

        [IgnoreDataMember]
        public SmallMapReferences.SingleMapReference.Location CurrentInnLocation =>
            (SmallMapReferences.SingleMapReference.Location)InnOrParty;

        [IgnoreDataMember]
        public int PrimarySpriteIndex =>
            Class switch
            {
                CharacterClass.Avatar => 284,
                CharacterClass.Bard => 324,
                CharacterClass.Fighter => 328,
                CharacterClass.Mage => 320,
                _ => throw new InvalidEnumArgumentException(((int)Class).ToString())
            };

        [JsonConstructor] private PlayerCharacterRecord()
        {
        }

        /// <summary>
        ///     Creates a character record from a raw record that begins at offset 0
        /// </summary>
        /// <param name="rawRecord"></param>
        public PlayerCharacterRecord(IReadOnlyCollection<byte> rawRecord)
        {
            Debug.Assert(rawRecord.Count == CHARACTER_RECORD_BYTE_ARRAY_SIZE);
            List<byte> rawRecordByteList = new(rawRecord);

            Name = DataChunk.CreateDataChunk(DataChunk.DataFormatType.SimpleString, "Character Name", rawRecordByteList,
                (int)CharacterRecordOffsets.Name, 9).GetChunkAsString();
            Gender = (CharacterGender)rawRecordByteList[(int)CharacterRecordOffsets.Gender];
            Class = (CharacterClass)rawRecordByteList[(int)CharacterRecordOffsets.Class];
            _monthsSinceStayingAtInn = rawRecordByteList[(int)CharacterRecordOffsets.MonthsSinceStayingAtInn];
            Stats.Status = (CharacterStatus)rawRecordByteList[(int)CharacterRecordOffsets.Status];
            Stats.Strength = rawRecordByteList[(int)CharacterRecordOffsets.Strength];
            Stats.Dexterity = rawRecordByteList[(int)CharacterRecordOffsets.Dexterity];
            Stats.Intelligence = rawRecordByteList[(int)CharacterRecordOffsets.Intelligence];
            Stats.CurrentMp = rawRecordByteList[(int)CharacterRecordOffsets.CurrentMP];
            Stats.CurrentHp = DataChunk.CreateDataChunk(DataChunk.DataFormatType.UINT16List, "Current hit points",
                rawRecordByteList, (int)CharacterRecordOffsets.CurrentHP, sizeof(ushort)).GetChunkAsUint16List()[0];
            Stats.MaximumHp = DataChunk.CreateDataChunk(DataChunk.DataFormatType.UINT16List, "Maximum hit points",
                rawRecordByteList, (int)CharacterRecordOffsets.MaximumHP, sizeof(ushort)).GetChunkAsUint16List()[0];
            Stats.ExperiencePoints = DataChunk.CreateDataChunk(DataChunk.DataFormatType.UINT16List,
                    "Maximum hit points", rawRecordByteList, (int)CharacterRecordOffsets.ExperiencePoints,
                    sizeof(ushort))
                .GetChunkAsUint16List()[0];
            Stats.Level = rawRecordByteList[(int)CharacterRecordOffsets.Level];

            // this approach is necessary because I have found circumstances where shields and weapons were swapped in the save file
            // I couldn't guarantee that other items wouldn't do the same so instead we allow each of the equipment save
            // slots can be "whatever" in "whatever" order. When I save them back to disk, I will save them in the correct order
            // Also confirmed that Ultima 5 can handle these equipment saves out of order as well
            Equipped.Helmet = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Helmet];
            Equipped.Armour = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Armor];
            Equipped.LeftHand = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Weapon];
            Equipped.RightHand = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Shield];
            Equipped.Ring = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Ring];
            Equipped.Amulet = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Amulet];

            // sometimes U5 swaps the shield and weapon, so we are going to be careful and just swap them back
            if ((int)Equipped.LeftHand <= (int)DataOvlReference.Equipment.JewelShield &&
                (int)Equipped.LeftHand >= (int)DataOvlReference.Equipment.Dagger)
            {
                (Equipped.RightHand, Equipped.LeftHand) = (Equipped.LeftHand, Equipped.RightHand);
            }

            InnOrParty = rawRecordByteList[(int)CharacterRecordOffsets.InnParty];

            Unknown2 = rawRecordByteList[(int)CharacterRecordOffsets.Unknown2];
        }

        private CharacterEquipped.EquippableSlot GetEquippableSlot(CombatItem combatItem)
        {
            return combatItem switch
            {
                Helm _ => CharacterEquipped.EquippableSlot.Helm,
                Amulet _ => CharacterEquipped.EquippableSlot.Amulet,
                Ring _ => CharacterEquipped.EquippableSlot.Ring,
                Armour _ => CharacterEquipped.EquippableSlot.Armour,
                Weapon weapon => weapon.TheCombatItemReference.IsShield
                    ? CharacterEquipped.EquippableSlot.RightHand
                    : CharacterEquipped.EquippableSlot.LeftHand,
                _ => throw new Ultima5ReduxException("Tried to get equippable slot for unsupported item: " +
                                                     combatItem.LongName)
            };
        }

        public int CastSpellMani()
        {
            if (Utils.Ran.Next() % 20 == 0)
            {
                return -1;
            }

            int nHealPoints = Utils.GetNumberFromAndTo(5, 25);
            Stats.CurrentHp = Math.Min(Stats.MaximumHp, Stats.CurrentHp + nHealPoints);
            return nHealPoints;
        }

        public bool Cure()
        {
            if (Stats.Status != CharacterStatus.Poisoned) return false;
            Stats.Status = CharacterStatus.Good;
            return true;
        }

        public EquipResult EquipEquipment(Inventory.Inventory inventory, DataOvlReference.Equipment newEquipment)
        {
            // detect the equipable slot
            CharacterEquipped.EquippableSlot equippableSlot =
                GetEquippableSlot(inventory.GetItemFromEquipment(newEquipment));

            // get the thing that is already equipped
            DataOvlReference.Equipment oldEquippedEquipment = Equipped.GetEquippedEquipment(equippableSlot);

            // put the old one back in your inventory
            if (oldEquippedEquipment != DataOvlReference.Equipment.Nothing)
            {
                CombatItem oldEquippedCombatItem = inventory.GetItemFromEquipment(oldEquippedEquipment);
                oldEquippedCombatItem.Quantity--;
            }

            // there should be at least one in your inventory to do this
            CombatItem newEquippedCombatItem = inventory.GetItemFromEquipment(newEquipment);

            if (newEquippedCombatItem == null)
                throw new Ultima5ReduxException($"Tried to get {newEquipment} but couldn't find it in inventory");
            Debug.Assert(newEquippedCombatItem.Quantity > 0);

            // let's make sure they have enough strength to wield/wear the Equipment
            if (Stats.Strength < newEquippedCombatItem.TheCombatItemReference.RequiredStrength)
                return EquipResult.TooHeavy;

            Equipped.SetEquippableSlot(equippableSlot, newEquipment);

            if (!(newEquippedCombatItem is Weapon weapon)) return EquipResult.Success;

            if (weapon.TheCombatItemReference.IsTwoHanded)
            {
                bool bUnequipped = UnequipEquipment(CharacterEquipped.EquippableSlot.RightHand, inventory);
                return bUnequipped ? EquipResult.SuccessUnequipRight : EquipResult.Success;
            }

            if (weapon.TheCombatItemReference.IsShield)
            {
                bool bUnequippedLeft = false;

                DataOvlReference.Equipment leftHandEquippedEquipment =
                    Equipped.GetEquippedEquipment(CharacterEquipped.EquippableSlot.LeftHand);

                if (inventory.GetItemFromEquipment(leftHandEquippedEquipment) is Weapon leftHandWeapon
                    && leftHandWeapon.TheCombatItemReference.IsTwoHanded)
                {
                    // if the left hand weapon is 2 handed then we unequip it
                    bUnequippedLeft = UnequipEquipment(CharacterEquipped.EquippableSlot.LeftHand, inventory);
                }

                return bUnequippedLeft ? EquipResult.SuccessUnequipLeft : EquipResult.Success;
            }

            return EquipResult.Success;
        }

        public string GetPlayerSelectedMessage(DataOvlReference dataOvlReference, bool bPlayerEscaped,
            out bool bIsSelectable)
        {
            bIsSelectable = false;
            if (bPlayerEscaped) return "Invalid!";

            switch (Stats.Status)
            {
                case CharacterStatus.Good:
                case CharacterStatus.Poisoned:
                    bIsSelectable = true;
                    return Name;
                case CharacterStatus.Charmed:
                    return $"Invalid!\n {Name} is charmed!";
                case CharacterStatus.Asleep:
                    return $"Invalid!\n {Name} is asleep!";
                case CharacterStatus.Dead:
                    return $"Invalid!\n {Name} is a late player character! He is no longer!";
                default:
                    throw new InvalidEnumArgumentException(((int)Stats.Status).ToString());
            }
        }

        public void SendCharacterToInn(SmallMapReferences.SingleMapReference.Location location)
        {
            InnOrParty = (byte)location;
            MonthsSinceStayingAtInn = 0;
            // if the character goes to the Inn while poisoned then they die there immediately
            if (Stats.Status == CharacterStatus.Poisoned)
            {
                Stats.CurrentHp = 0;
                Stats.Status = CharacterStatus.Dead;
            }
        }

        public void TurnIntoNotARat()
        {
            Debug.Assert(IsRat);
            IsRat = false;
        }

        public void TurnIntoRat()
        {
            IsRat = true;
        }

        public bool TurnInvisible()
        {
            //285 Apparition
            IsInvisible = true;
            return true;
        }

        public void TurnVisible()
        {
            IsInvisible = false;
        }

        /// <summary>
        ///     Unequips and item from the user and
        /// </summary>
        /// <param name="equippableSlot"></param>
        /// <param name="inventory"></param>
        public bool UnequipEquipment(CharacterEquipped.EquippableSlot equippableSlot, Inventory.Inventory inventory)
        {
            if (!Equipped.IsEquipped(equippableSlot)) return false;

            DataOvlReference.Equipment equippedEquipment = Equipped.GetEquippedEquipment(equippableSlot);
            CombatItem combatItem = inventory.GetItemFromEquipment(equippedEquipment);
            if (combatItem == null)
                throw new Ultima5ReduxException(
                    $"Tried to get {equippedEquipment} but couldn't find it in inventory (unequip)");

            combatItem.Quantity++;
            Equipped.UnequipEquippableSlot(equippableSlot);
            return true;
        }

        public bool WakeUp()
        {
            if (Stats.Status == CharacterStatus.Dead || Stats.Status == CharacterStatus.Poisoned) return false;
            Stats.Status = CharacterStatus.Good;
            return true;
        }

        //        offset length      purpose range
        //0           9           character name      zero-terminated string
        //                                            (length = 8+1)
        //9           1           gender              0xB = male, 0xC = female
        //0xA         1           class               'A'vatar, 'B'ard, 'F'ighter,
        //                                            'M'age
        //0xB         1           status              'G'ood, etc.
        //0xC         1           strength            1-50
        //0xD         1           dexterity           1-50
        //0xE         1           intelligence        1-50
        //0xF         1           current mp          0-50
        //0x10        2           current hp          1-240
        //0x12        2           maximum hp          1-240
        //0x14        2           exp points          0-9999
        //0x16        1           level               1-8
        //0x17        1           ?                   ?
        //0x18        1           ?                   ?
        //0x19        1           helmet              0-0x2F,0xFF
        //0x1A        1           armor               0-0x2F,0xFF
        //0x1B        1           weapon              0-0x2F,0xFF
        //0x1C        1           shield              0-0x2F,0xFF
        //0x1D        1           ring                0-0x2F,0xFF
        //0x1E        1           amulet              0-0x2F,0xFF
        //0x1F        1           inn/party n/a
    }
}