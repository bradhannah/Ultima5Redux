using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    class CharacterRecords
    {
        public CharacterRecord[] Records = new CharacterRecord[TOTAL_CHARACTER_RECORDS];

        private const int TOTAL_CHARACTER_RECORDS = 16;
        private const byte CHARACTER_OFFSET = 0x20;

        public CharacterRecords(List<byte> rawByteList)
        {
            for (int nRecord = 0; nRecord < 16; nRecord++)
            {
                byte[] characterArray = new byte[CHARACTER_OFFSET];
                rawByteList.CopyTo(nRecord * CHARACTER_OFFSET, characterArray, 0, CharacterRecord.CHARACTER_RECORD_BYTE_ARRAY_SIZE);
                Records[nRecord] = new CharacterRecord(characterArray);
            }
        }
    }


    class CharacterRecord
    {
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
        private const int NAME_LENGTH = 8;
        protected internal const byte CHARACTER_RECORD_BYTE_ARRAY_SIZE = 0x20;

        public enum CharacterGender { Male = 0x0B, Female = 0x0C };
        public enum CharacterClass { Avatar = 'A', Bard = 'B', Fighter = 'F', Mage = 'M'};
        public enum CharacterStatus { Good = 'G', Poisioned = 'P'};
        // if your InnorParty value is included in the list, then it's clear - if not then they are at the location referenced by the number
        public enum CharacterPartyStatus { InParty = 0x00, HasntJoinedYet = 0xFF, KilledPermanently = 0x7F};

        private byte Unknown1 { get; set; }
        private byte Unknown2 { get; set; }
        public byte InnOrParty { get; set; }

        public string Name { get; set; }
        public CharacterGender Gender { get; set; }
        public CharacterClass Class { get; set; }
        public CharacterStatus Status { get; set; }
        public CharacterPartyStatus PartyStatus { get; set; }
        public CharacterEquipped Equipped = new CharacterEquipped();
        public CharacterStats Stats = new CharacterStats();

        /// <summary>
        /// Creates a character record from a raw record that begins at offset 0
        /// </summary>
        /// <param name="rawRecord"></param>
        public CharacterRecord(byte[] rawRecord)
        {
            Debug.Assert(rawRecord.Length == (int)CHARACTER_RECORD_BYTE_ARRAY_SIZE);

            //DataChunks<>
        }

        public class CharacterEquipped
        {
            byte Helmet { get; set; }
            byte Armor { get; set; }
            byte Weapon { get; set; }
            byte Shield { get; set; }
            byte Ring { get; set; }
            byte Amulet { get; set; }
        }


        public class CharacterStats
        {
            uint Strength { get; set; }
            uint Dexterity { get; set; }
            uint Intelligence { get; set; }
            uint CurrentMP { get; set; }
            uint CurrentHP { get; set; }
            uint MaximumHP { get; set; }
            uint ExperiencePoints { get; set; }
            uint Level { get; set; }
        }

    }
    
}
