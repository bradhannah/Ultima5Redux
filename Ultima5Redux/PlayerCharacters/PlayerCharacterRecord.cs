﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters
{

    public class PlayerCharacterRecord
    {
        private const int NAME_LENGTH = 8;
        protected internal const byte CHARACTER_RECORD_BYTE_ARRAY_SIZE = 0x20;

        //private enum CharacterInnOrParty { InParty = 0x00, HasntJoined = 0xFF, PermanentlyKilled = 0x7F };
        public enum CharacterGender { Male = 0x0B, Female = 0x0C };
        public enum CharacterClass { Avatar = 'A', Bard = 'B', Fighter = 'F', Mage = 'M'};
        public enum CharacterStatus { Good = 'G', Poisioned = 'P', Charmed = 'C', Asleep = 'S', Dead = 'D'};
        // if your InnorParty value is included in the list, then it's clear - if not then they are at the location referenced by the number
        //public int TotalDefense
        //{
        //    get
        //    {
        //        //return Equipped.Amulet;
        //        return 30;
        //    }
        //}

        //public int TotalAttack
        //{
        //    get
        //    {
        //        return 25;
        //    }
        //}
        public enum CharacterPartyStatus { InParty = 0x00, HasntJoinedYet = 0xFF, KilledPermanently = 0x7F}; // otherwise it is at an inn at Settlement # in byte value

        private byte Unknown1 { get; set; }
        private byte Unknown2 { get; set; }

        private byte InnOrParty { get; set; }

        public string Name { get; set; }
        public CharacterGender Gender { get; set; }
        public CharacterClass Class { get; set; }
        public CharacterStatus Status { get; set; }
        public CharacterPartyStatus PartyStatus
        {
            get
            {
                return (CharacterPartyStatus)InnOrParty;
            }
            set
            {
                InnOrParty = (byte)value;
            }
        }
        public CharacterEquipped Equipped = new CharacterEquipped();
        public CharacterStats Stats = new CharacterStats();

        private enum DataChunkName { Unused };
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

        private enum CharacterRecordOffsets { Name = 0x00, Gender = 0x09, Class = 0x0A, Status = 0x0B, Strength = 0x0C, Dexterity = 0x0D, Intelligence = 0x0E,
        CurrentMP = 0x0F, CurrentHP = 0x10, MaximimumHP = 0x12, ExperiencePoints = 0x14, Level = 0x16, Helmet = 0x19, Armor = 0x1A, Weapon = 0x1B, Shield  = 0x1C,
        Ring = 0x1D, Amulet = 0x1E, InnParty = 0x1F, Unknown1 = 0x17, Unknown2 = 0x18 };
        
        /// <summary>
        /// Creates a character record from a raw record that begins at offset 0
        /// </summary>
        /// <param name="rawRecord"></param>
        public PlayerCharacterRecord(byte[] rawRecord)
        {
            Debug.Assert(rawRecord.Length == (int)CHARACTER_RECORD_BYTE_ARRAY_SIZE);
            List<byte> rawRecordByteList = new List<byte>(rawRecord);

            Name = DataChunk.CreateDataChunk(DataChunk.DataFormatType.SimpleString, "Character Name", rawRecordByteList, (int)CharacterRecordOffsets.Name, 9).GetChunkAsString();
            Gender = (CharacterGender)rawRecordByteList[(int)CharacterRecordOffsets.Gender];                
            Class = (CharacterClass)rawRecordByteList[(int)CharacterRecordOffsets.Class];
            Status = (CharacterStatus)rawRecordByteList[(int)CharacterRecordOffsets.Status];
            Stats.Strength = rawRecordByteList[(int)CharacterRecordOffsets.Strength];
            Stats.Dexterity = rawRecordByteList[(int)CharacterRecordOffsets.Dexterity];
            Stats.Intelligence = rawRecordByteList[(int)CharacterRecordOffsets.Intelligence];
            Stats.CurrentMp = rawRecordByteList[(int)CharacterRecordOffsets.CurrentMP];
            Stats.CurrentHp = DataChunk.CreateDataChunk(DataChunk.DataFormatType.UINT16List, "Current hit points", rawRecordByteList, (int)CharacterRecordOffsets.CurrentHP, sizeof(UInt16)).GetChunkAsUint16List()[0];
            Stats.MaximumHp = DataChunk.CreateDataChunk(DataChunk.DataFormatType.UINT16List, "Maximum hit points", rawRecordByteList, (int)CharacterRecordOffsets.MaximimumHP, sizeof(UInt16)).GetChunkAsUint16List()[0];
            Stats.ExperiencePoints = DataChunk.CreateDataChunk(DataChunk.DataFormatType.UINT16List, "Maximum hit points", rawRecordByteList, (int)CharacterRecordOffsets.ExperiencePoints, sizeof(UInt16)).GetChunkAsUint16List()[0];
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
            if ((int)Equipped.LeftHand <= (int)DataOvlReference.Equipment.JewelShield && (int)Equipped.LeftHand >= (int)DataOvlReference.Equipment.Dagger)
            {
                DataOvlReference.Equipment shieldEquip = Equipped.RightHand;
                Equipped.RightHand = Equipped.LeftHand;
                Equipped.LeftHand = shieldEquip;
            }

       
            InnOrParty = rawRecordByteList[(int)CharacterRecordOffsets.InnParty];

            Unknown1 = rawRecordByteList[(int)CharacterRecordOffsets.Unknown1];
            Unknown2 = rawRecordByteList[(int)CharacterRecordOffsets.Unknown2];
        }

        public class CharacterEquipped
        {
            public bool IsEquipped(DataOvlReference.Equipment equipment)
            {
                return (Helmet == equipment || Armor == equipment || LeftHand == equipment || RightHand == equipment || Ring == equipment || Amulet == equipment);
            }

            public CharacterEquipped()
            {
                Helmet = DataOvlReference.Equipment.Nothing;
                Armor = DataOvlReference.Equipment.Nothing;
                LeftHand = DataOvlReference.Equipment.Nothing;
                RightHand = DataOvlReference.Equipment.Nothing;
                Ring = DataOvlReference.Equipment.Nothing;
                Amulet = DataOvlReference.Equipment.Nothing;
            }

            public DataOvlReference.Equipment Helmet { get; set; }
            public DataOvlReference.Equipment Armor { get; set; }
            public DataOvlReference.Equipment LeftHand { get; set; }
            public DataOvlReference.Equipment RightHand { get; set; }
            public DataOvlReference.Equipment Ring { get; set; }
            public DataOvlReference.Equipment Amulet { get; set; }
        }


        public class CharacterStats
        {
            public uint Strength { get; set; }
            public uint Dexterity { get; set; }
            public uint Intelligence { get; set; }
            public uint CurrentMp { get; set; }
            public UInt16 CurrentHp { get; set; }
            public uint MaximumHp { get; set; }
            public uint ExperiencePoints { get; set; }
            public uint Level { get; set; }
        }

    }
    
}
