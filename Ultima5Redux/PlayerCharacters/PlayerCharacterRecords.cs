using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.PlayerCharacters
{
    public class PlayerCharacterRecords
    {
        public const int AVATAR_RECORD = 0x00;

        private const int TOTAL_CHARACTER_RECORDS = 16;
        private const byte CHARACTER_OFFSET = 0x20;
        public const int MAX_PARTY_MEMBERS = 6;

        public readonly List<PlayerCharacterRecord> Records = new List<PlayerCharacterRecord>(TOTAL_CHARACTER_RECORDS);

        public PlayerCharacterRecords(List<byte> rawByteList)
        {
            for (int nRecord = 0; nRecord < TOTAL_CHARACTER_RECORDS; nRecord++)
            {
                byte[] characterArray = new byte[CHARACTER_OFFSET];
                // start at 0x02 byte because the first two characters are 0x0000
                rawByteList.CopyTo(nRecord * CHARACTER_OFFSET, characterArray, 0x00,
                    PlayerCharacterRecord.CHARACTER_RECORD_BYTE_ARRAY_SIZE);
                Records.Add(new PlayerCharacterRecord(characterArray));
            }
        }

        public PlayerCharacterRecord AvatarRecord => Records[0];

        public int MaxCharactersInParty => MAX_PARTY_MEMBERS;

        public List<PlayerCharacterRecord> GetPlayersAtInn(SmallMapReferences.SingleMapReference.Location location)
        {
            return Records.Where(record => record.CurrentInnLocation == location).ToList();
        }

        public bool IsEquipmentEquipped(DataOvlReference.Equipment equipment)
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
            int nActiveRecs = TotalPartyMembers();
            if (nFirstPos > nActiveRecs || nSecondPos > nActiveRecs)
                throw new Ultima5ReduxException("Asked to swap a party member who doesn't exist First:#" + nFirstPos
                    + " Second:#" + nSecondPos);

            PlayerCharacterRecord tempRec = Records[nFirstPos];
            Records[nFirstPos] = Records[nSecondPos];
            Records[nSecondPos] = tempRec;
        }

        public PlayerCharacterRecord GetCharacterRecordByNPC(NonPlayerCharacterReference npc)
        {
            foreach (PlayerCharacterRecord record in Records)
            {
                if (record.Name == npc.Name) return record;
            }

            return null;
        }

        public void JoinPlayerCharacter(PlayerCharacterRecord record)
        {
            int nJoinedCharacterIndex = TotalPartyMembers();
            Debug.Assert(nJoinedCharacterIndex < MAX_PARTY_MEMBERS);
            Debug.Assert(record.PartyStatus != PlayerCharacterRecord.CharacterPartyStatus.InTheParty);

            Records.Remove(record);
            Records.Insert(nJoinedCharacterIndex, record);

            record.PartyStatus = PlayerCharacterRecord.CharacterPartyStatus.InTheParty;
        }

        public int TotalPartyMembers()
        {
            int nPartyMembers = 0;
            foreach (PlayerCharacterRecord characterRecord in Records)
            {
                if (characterRecord.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InTheParty)
                    nPartyMembers++;
            }

            Debug.Assert(nPartyMembers > 0 && nPartyMembers <= 6);
            return nPartyMembers;
        }

        public int GetIndexOfPlayerCharacterRecord(PlayerCharacterRecord record)
        {
            for (int i = 0; i < Records.Count; i++ )
            {
                if (Records[i] == record) return i;
            }

            return -1;
        }

        public void HealAllPlayers()
        {
            foreach (PlayerCharacterRecord characterRecord in Records)
            {
                if (characterRecord.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InTheParty)
                    characterRecord.Stats.CurrentHp = characterRecord.Stats.MaximumHp;
            }
        }

        public void SendCharacterToInn(PlayerCharacterRecord record,
            SmallMapReferences.SingleMapReference.Location location)
        {
            record.SendCharacterToInn(location);
        }

        public void IncrementStayingAtInnCounters()
        {
            foreach (PlayerCharacterRecord record in Records)
            {
                if (record.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.AtTheInn)
                    record.MonthsSinceStayingAtInn++;
            }
        }

        /// <summary>
        ///     Gets all active character records for members in the Avatars party
        /// </summary>
        /// <returns></returns>
        public List<PlayerCharacterRecord> GetActiveCharacterRecords()
        {
            List<PlayerCharacterRecord> activeCharacterRecords = new List<PlayerCharacterRecord>();

            foreach (PlayerCharacterRecord characterRecord in Records)
            {
                if (characterRecord.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InTheParty)
                    activeCharacterRecords.Add(characterRecord);
            }

            if (activeCharacterRecords.Count == 0)
                throw new Ultima5ReduxException("Even the Avatar is dead, no records returned in active party");
            if (activeCharacterRecords.Count > MAX_PARTY_MEMBERS)
                throw new Ultima5ReduxException("There are too many party members in the party... party...");

            return activeCharacterRecords;
        }

        /// <summary>
        ///     Gets the number of active characters in the Avatars party
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfActiveCharacters()
        {
            // Note: this is inefficient!
            return GetActiveCharacterRecords().Count;
        }

        /// <summary>
        ///     Gets a character from the active party by index
        ///     Throws an exception if you asked for a member who isn't there - so check first
        /// </summary>
        /// <param name="nPosition"></param>
        /// <returns></returns>
        public PlayerCharacterRecord GetCharacterFromParty(int nPosition)
        {
            Debug.Assert(nPosition >= 0 && nPosition < MAX_PARTY_MEMBERS, "There are a maximum of 6 characters");
            Debug.Assert(nPosition < TotalPartyMembers(), "You cannot request a character that isn't on the roster");

            return GetActiveCharacterRecords()[nPosition];

            // int nPartyMember = 0;
            // foreach (PlayerCharacterRecord characterRecord in Records)
            // {
            //     if (characterRecord.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InTheParty)
            //         if (nPartyMember++ == nPosition) return characterRecord;
            // }
            // throw new Ultima5ReduxException("I've asked for member of the party who is apparently not there...");
        }

        /// <summary>
        ///     Adds an NPC character to the party, and maps their CharacterRecord
        /// </summary>
        /// <param name="npc">the NPC to add</param>
        public void AddMemberToParty(NonPlayerCharacterReference npc)
        {
            PlayerCharacterRecord record = GetCharacterRecordByNPC(npc);
            if (record == null)
                throw new Ultima5ReduxException("Adding a member to party resulted in no retrieved record");
            record.PartyStatus = PlayerCharacterRecord.CharacterPartyStatus.InTheParty;
        }

        /// <summary>
        ///     Is my party full (at capacity)
        /// </summary>
        /// <returns>true if party is full</returns>
        public bool IsFullParty()
        {
            Debug.Assert(!(TotalPartyMembers() > MAX_PARTY_MEMBERS), "You have more party members than you should.");
            return TotalPartyMembers() == MAX_PARTY_MEMBERS;
        }

        /// <summary>
        /// Injures all party members due to rough sea
        /// </summary>
        public void RoughSeasInjure()
        {
            Random ran = new Random();
            foreach (PlayerCharacterRecord record in Records)
            {
                record.Stats.CurrentHp -= ran.Next(1, 9);
            }
        }

        /// <summary>
        /// When you exit combat you revert from being a Rat to a real boy!
        /// </summary>
        public void ClearRatStatuses()
        {
            foreach (PlayerCharacterRecord record in Records.Where(record =>
                record.Stats.Status == PlayerCharacterRecord.CharacterStatus.Rat))
            {
                record.TurnIntoNotARat();
            }
        }
    }
}