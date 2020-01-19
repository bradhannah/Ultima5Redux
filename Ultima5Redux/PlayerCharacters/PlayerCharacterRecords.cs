using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class PlayerCharacterRecords
    {
        public const int AVATAR_RECORD = 0x00;

        public PlayerCharacterRecord[] Records = new PlayerCharacterRecord[TOTAL_CHARACTER_RECORDS];

        private const int TOTAL_CHARACTER_RECORDS = 16;
        private const byte CHARACTER_OFFSET = 0x20;
        public const int MAX_PARTY_MEMBERS = 6;


        public PlayerCharacterRecords(List<byte> rawByteList)
        {
            for (int nRecord = 0; nRecord < TOTAL_CHARACTER_RECORDS; nRecord++)
            {
                byte[] characterArray = new byte[CHARACTER_OFFSET];
                // start at 0x02 byte because the first two characters are 0x0000
                rawByteList.CopyTo(nRecord * CHARACTER_OFFSET , characterArray, 0x00, PlayerCharacterRecord.CHARACTER_RECORD_BYTE_ARRAY_SIZE);
                Records[nRecord] = new PlayerCharacterRecord(characterArray);
            }
        }

        public bool IsEquipmentEquipped(DataOvlReference.EQUIPMENT equipment)
        {
            foreach (PlayerCharacterRecord record in Records)
            {
                if (record.Equipped.IsEquipped(equipment))
                    return true;
            }
            return false;
        }

        public void SwapPositions(int nFirstPos, int nSecondPos)
        {
            int nActiveRecs = this.TotalPartyMembers();
            if (nFirstPos > nActiveRecs || nSecondPos > nActiveRecs) throw new Exception("Asked to swap a party member who doesn't exist First:#" + nFirstPos.ToString()
                + " Second:#" + nSecondPos.ToString());

            PlayerCharacterRecord tempRec = Records[nFirstPos];
            Records[nFirstPos] = Records[nSecondPos];
            Records[nSecondPos] = tempRec;
        }

        public PlayerCharacterRecord GetCharacterRecordByNPC(NonPlayerCharacterReference npc)
        {
            foreach (PlayerCharacterRecord record in Records)
            {
                if (record.Name == npc.Name)
                {
                    return record;
                }
            }
            return null;
            //throw new Exception("Was unable to match CharacterRecord with NPC: " + npc.Name);
        }

        public int TotalPartyMembers()
        {
            int nPartyMembers = 0;
            foreach (PlayerCharacterRecord characterRecord in Records)
            {
                if (characterRecord.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InParty)
                    nPartyMembers++;
            }
            Debug.Assert(nPartyMembers > 0 && nPartyMembers <= 6);
            return nPartyMembers;
        }
    }
    
}
