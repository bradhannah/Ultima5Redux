using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.PlayerCharacters
{
    public class PlayerCharacterRecord
    {
        public enum CharacterClass { Avatar = 'A', Bard = 'B', Fighter = 'F', Mage = 'M' }

        public enum CharacterGender { Male = 0x0B, Female = 0x0C }

        public enum CharacterPartyStatus
        {
            InTheParty = 0x00, HasntJoinedYet = 0xFF, AtTheInn = 0x01
        } // otherwise it is at an inn at Settlement # in byte value

        public enum CharacterStatus { Good = 'G', Poisoned = 'P', Charmed = 'C', Asleep = 'S', Dead = 'D' }

        private const int NAME_LENGTH = 8;
        protected internal const byte CHARACTER_RECORD_BYTE_ARRAY_SIZE = 0x20;

        private static readonly Random _random = new Random();

        public readonly CharacterEquipped Equipped = new CharacterEquipped();
        public readonly CharacterStats Stats = new CharacterStats();
        private byte _monthsSinceStayingAtInn;

        /// <summary>
        ///     Creates a character record from a raw record that begins at offset 0
        /// </summary>
        /// <param name="rawRecord"></param>
        public PlayerCharacterRecord(IReadOnlyCollection<byte> rawRecord)
        {
            Debug.Assert(rawRecord.Count == CHARACTER_RECORD_BYTE_ARRAY_SIZE);
            List<byte> rawRecordByteList = new List<byte>(rawRecord);

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
            List<DataOvlReference.Equipment> allEquipment = new List<DataOvlReference.Equipment>(6);
            allEquipment.Add((DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Helmet]);
            allEquipment.Add((DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Armor]);
            allEquipment.Add((DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Weapon]);
            allEquipment.Add((DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Shield]);
            allEquipment.Add((DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Ring]);
            allEquipment.Add((DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Amulet]);

            Equipped.Helmet = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Helmet];
            Equipped.Armor = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Armor];
            Equipped.LeftHand = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Weapon];
            Equipped.RightHand = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Shield];
            Equipped.Ring = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Ring];
            Equipped.Amulet = (DataOvlReference.Equipment)rawRecordByteList[(int)CharacterRecordOffsets.Amulet];

            // sometimes U5 swaps the shield and weapon, so we are going to be careful and just swap them back
            if ((int)Equipped.LeftHand <= (int)DataOvlReference.Equipment.JewelShield &&
                (int)Equipped.LeftHand >= (int)DataOvlReference.Equipment.Dagger)
            {
                DataOvlReference.Equipment shieldEquip = Equipped.RightHand;
                Equipped.RightHand = Equipped.LeftHand;
                Equipped.LeftHand = shieldEquip;
            }

            InnOrParty = rawRecordByteList[(int)CharacterRecordOffsets.InnParty];

            Unknown2 = rawRecordByteList[(int)CharacterRecordOffsets.Unknown2];
        }

        private byte InnOrParty { get; set; }

        //, KilledPermanently = 0x7F
        private byte Unknown2 { get; }
        public bool IsInvisible { get; private set; }

        public bool IsRat { get; private set; }

        public byte MonthsSinceStayingAtInn
        {
            get => _monthsSinceStayingAtInn;
            set => _monthsSinceStayingAtInn = (byte)(value % byte.MaxValue);
        }

        public CharacterClass Class { get; set; }
        public CharacterGender Gender { get; set; }

        public CharacterPartyStatus PartyStatus
        {
            get
            {
                if (InnOrParty == 0x00) return CharacterPartyStatus.InTheParty;
                if (InnOrParty == 0xFF) return CharacterPartyStatus.HasntJoinedYet;
                return CharacterPartyStatus.AtTheInn;
            }
            set => InnOrParty = (byte)value;
        }

        public int PrimarySpriteIndex =>
            Class switch
            {
                CharacterClass.Avatar => 284,
                CharacterClass.Bard => 324,
                CharacterClass.Fighter => 328,
                CharacterClass.Mage => 320,
                _ => throw new ArgumentOutOfRangeException()
            };

        public SmallMapReferences.SingleMapReference.Location CurrentInnLocation =>
            (SmallMapReferences.SingleMapReference.Location)InnOrParty;

        public string Name { get; }

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

        public int CastSpellMani()
        {
            if (_random.Next() % 20 == 0)
            {
                return -1;
            }

            int nHealPoints = Utils.GetNumberBetween(5, 25);
            Stats.CurrentHp = Math.Min(Stats.MaximumHp, Stats.CurrentHp + nHealPoints);
            return nHealPoints;
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

        public int Heal()
        {
            int nCurrentHp = Stats.CurrentHp;
            Stats.CurrentHp = Stats.MaximumHp;
            return Stats.MaximumHp - nCurrentHp;
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

        public bool Cure()
        {
            if (Stats.Status != CharacterStatus.Poisoned) return false;
            Stats.Status = CharacterStatus.Good;
            return true;
        }

        public bool Resurrect()
        {
            if (Stats.Status != CharacterStatus.Dead) return false;
            Stats.Status = CharacterStatus.Good;
            Stats.CurrentHp = Stats.MaximumHp;
            return true;
        }

        public bool Poison()
        {
            if (Stats.Status == CharacterStatus.Dead) return false;
            Debug.Assert(Stats.Status == CharacterStatus.Good || Stats.Status == CharacterStatus.Poisoned);
            Stats.Status = CharacterStatus.Poisoned;
            return true;
        }

        public bool Sleep()
        {
            if (Stats.Status == CharacterStatus.Dead || Stats.Status == CharacterStatus.Poisoned) return false;
            Stats.Status = CharacterStatus.Asleep;
            return true;
        }

        public bool WakeUp()
        {
            if (Stats.Status == CharacterStatus.Dead || Stats.Status == CharacterStatus.Poisoned) return false;
            Stats.Status = CharacterStatus.Good;
            return true;
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
                    throw new ArgumentOutOfRangeException();
            }
        }

        private enum DataChunkName { Unused }
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

        private enum CharacterRecordOffsets
        {
            Name = 0x00, Gender = 0x09, Class = 0x0A, Status = 0x0B, Strength = 0x0C, Dexterity = 0x0D,
            Intelligence = 0x0E, CurrentMP = 0x0F, CurrentHP = 0x10, MaximumHP = 0x12, ExperiencePoints = 0x14,
            Level = 0x16, MonthsSinceStayingAtInn = 0x17, Unknown2 = 0x18, Helmet = 0x19, Armor = 0x1A, Weapon = 0x1B,
            Shield = 0x1C, Ring = 0x1D, Amulet = 0x1E, InnParty = 0x1F
        }

        public class CharacterEquipped
        {
            public CharacterEquipped()
            {
                Helmet = DataOvlReference.Equipment.Nothing;
                Armor = DataOvlReference.Equipment.Nothing;
                LeftHand = DataOvlReference.Equipment.Nothing;
                RightHand = DataOvlReference.Equipment.Nothing;
                Ring = DataOvlReference.Equipment.Nothing;
                Amulet = DataOvlReference.Equipment.Nothing;
            }

            public DataOvlReference.Equipment Amulet { get; set; }
            public DataOvlReference.Equipment Armor { get; set; }

            public DataOvlReference.Equipment Helmet { get; set; }
            public DataOvlReference.Equipment LeftHand { get; set; }
            public DataOvlReference.Equipment RightHand { get; set; }
            public DataOvlReference.Equipment Ring { get; set; }

            public bool IsEquipped(DataOvlReference.Equipment equipment)
            {
                return Helmet == equipment || Armor == equipment || LeftHand == equipment || RightHand == equipment ||
                       Ring == equipment || Amulet == equipment;
            }
        }
    }
}