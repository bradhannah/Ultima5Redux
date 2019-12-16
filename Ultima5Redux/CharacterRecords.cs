using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class CharacterRecords
    {
        public const int AVATAR_RECORD = 0x00;

        public CharacterRecord[] Records = new CharacterRecord[TOTAL_CHARACTER_RECORDS];

        private const int TOTAL_CHARACTER_RECORDS = 16;
        private const byte CHARACTER_OFFSET = 0x20;
        public const int MAX_PARTY_MEMBERS = 6;


        public CharacterRecords(List<byte> rawByteList)
        {
            for (int nRecord = 0; nRecord < 16; nRecord++)
            {
                byte[] characterArray = new byte[CHARACTER_OFFSET];
                // start at 0x02 byte because the first two characters are 0x0000
                rawByteList.CopyTo(nRecord * CHARACTER_OFFSET , characterArray, 0x00, CharacterRecord.CHARACTER_RECORD_BYTE_ARRAY_SIZE);
                Records[nRecord] = new CharacterRecord(characterArray);
            }
        }


        public void SwapPositions(int nFirstPos, int nSecondPos)
        {
            int nActiveRecs = this.TotalPartyMembers();
            if (nFirstPos > nActiveRecs || nSecondPos > nActiveRecs) throw new Exception("Asked to swap a party member who doesn't exist First:#" + nFirstPos.ToString()
                + " Second:#" + nSecondPos.ToString());

            CharacterRecord tempRec = Records[nFirstPos];
            Records[nFirstPos] = Records[nSecondPos];
            Records[nSecondPos] = tempRec;
        }

        public CharacterRecord GetCharacterRecordByNPC(NonPlayerCharacters.NonPlayerCharacter npc)
        {
            foreach (CharacterRecord record in Records)
            {
                if (record.Name == npc.Name)
                {
                    return record;
                }
            }
            throw new Exception("Was unable to match CharacterRecord with NPC: " + npc.Name);
        }

        public int TotalPartyMembers()
        {
            int nPartyMembers = 0;
            foreach (CharacterRecord characterRecord in Records)
            {
                if (characterRecord.PartyStatus == CharacterRecord.CharacterPartyStatus.InParty)
                    nPartyMembers++;
            }
            Debug.Assert(nPartyMembers > 0 && nPartyMembers <= 6);
            return nPartyMembers;
        }
    }
    
}
