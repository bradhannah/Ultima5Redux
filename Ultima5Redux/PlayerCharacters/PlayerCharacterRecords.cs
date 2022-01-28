using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.PlayerCharacters
{
    [DataContract] public class PlayerCharacterRecords
    {
        private const byte CHARACTER_OFFSET = 0x20;

        private const int TOTAL_CHARACTER_RECORDS = 16;
        public const int AVATAR_RECORD = 0x00;
        public const int MAX_PARTY_MEMBERS = 6;

        [DataMember] public readonly List<PlayerCharacterRecord> Records = new(TOTAL_CHARACTER_RECORDS);

        [IgnoreDataMember] public PlayerCharacterRecord AvatarRecord => Records[0];

        /// <summary>
        ///     Are there at least one player based on current combat statuses that can be seen by the enemy?
        /// </summary>
        public bool AtLeastOnePlayerSeenOnCombatMap
        {
            get
            {
                List<PlayerCharacterRecord> playerCharacterRecordsInParty = Records.Where(record =>
                    record.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InTheParty).ToList();
                return playerCharacterRecordsInParty.Any(record => !record.IsInvisible) &&
                       playerCharacterRecordsInParty.Any(record =>
                           record.Stats.Status != PlayerCharacterRecord.CharacterStatus.Dead);
            }
        }

        public int MaxCharactersInParty => MAX_PARTY_MEMBERS;

        [JsonConstructor] private PlayerCharacterRecords()
        {
        }

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

        public string ApplyRandomCharacterStatusForMixSpell()
        {
            string retStr = "";
            const int MAX_INJURE_AMOUNT = 20;

            void injurePlayer(PlayerCharacterRecord record) =>
                record.Stats.CurrentHp -= Utils.Ran.Next() % (MAX_INJURE_AMOUNT + 1);

            switch (Utils.Ran.Next() % 4)
            {
                case 0:
                    // poison - the Avatar
                    retStr = GameReferences.DataOvlRef.StringReferences
                        .GetString(DataOvlReference.ExclaimStrings.POISONED_BANG_N).Trim();
                    AvatarRecord.Stats.Status = PlayerCharacterRecord.CharacterStatus.Poisoned;
                    break;
                case 1:
                    // Gas - poison the whole party
                    retStr = "GAS!";
                    foreach (PlayerCharacterRecord record in Records)
                    {
                        record.Stats.Status = PlayerCharacterRecord.CharacterStatus.Poisoned;
                    }

                    break;
                case 2:
                    // Acid - hurt the Avatar
                    retStr = "ACID!";
                    injurePlayer(AvatarRecord);
                    break;
                case 3:
                    // Bomb - hurt the party
                    retStr = "BOMB!";
                    foreach (PlayerCharacterRecord record in Records)
                    {
                        injurePlayer(record);
                    }

                    break;
                default:
                    throw new Ultima5ReduxException("EH?");
            }

            return retStr;
        }

        /// <summary>
        ///     When you exit combat you revert from being a Rat to a real boy!
        /// </summary>
        public void ClearCombatStatuses()
        {
            foreach (PlayerCharacterRecord record in Records.Where(record => record.IsRat))
            {
                record.TurnIntoNotARat();
            }

            foreach (PlayerCharacterRecord record in Records.Where(record => record.IsInvisible))
            {
                record.TurnVisible();
            }
        }

        /// <summary>
        ///     Gets all active character records for members in the Avatars party
        /// </summary>
        /// <returns></returns>
        public List<PlayerCharacterRecord> GetActiveCharacterRecords()
        {
            List<PlayerCharacterRecord> activeCharacterRecords = new();

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
        }

        public int GetCharacterIndexByNPC(NonPlayerCharacterReference npc)
        {
            for (int i = 0; i < Records.Count; i++)
            {
                if (Records[i].Name == npc.Name) return i;
            }

            return -1;
        }

        public PlayerCharacterRecord GetCharacterRecordByNPC(NonPlayerCharacterReference npc) =>
            Records.FirstOrDefault(record => record.Name == npc.Name);

        public int GetIndexOfPlayerCharacterRecord(PlayerCharacterRecord record)
        {
            for (int i = 0; i < Records.Count; i++)
            {
                if (Records[i] == record) return i;
            }

            return -1;
        }

        /// <summary>
        ///     Gets the number of active characters in the Avatars party
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfActiveCharacters() => GetActiveCharacterRecords().Count;

        public List<PlayerCharacterRecord> GetPlayersAtInn(SmallMapReferences.SingleMapReference.Location location)
        {
            return Records.Where(record => record.CurrentInnLocation == location).ToList();
        }

        public void HealAllPlayers()
        {
            foreach (PlayerCharacterRecord characterRecord in Records)
            {
                if (characterRecord.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.InTheParty)
                    characterRecord.Stats.CurrentHp = characterRecord.Stats.MaximumHp;
            }
        }

        public void IncrementStayingAtInnCounters()
        {
            foreach (PlayerCharacterRecord record in Records)
            {
                if (record.PartyStatus == PlayerCharacterRecord.CharacterPartyStatus.AtTheInn)
                    record.MonthsSinceStayingAtInn++;
            }
        }

        public bool IsEquipmentEquipped(DataOvlReference.Equipment equipment) =>
            Records.Any(record => record.Equipped.IsEquipped(equipment));

        /// <summary>
        ///     Is my party full (at capacity)
        /// </summary>
        /// <returns>true if party is full</returns>
        public bool IsFullParty()
        {
            Debug.Assert((TotalPartyMembers() <= MAX_PARTY_MEMBERS), "You have more party members than you should.");
            return TotalPartyMembers() == MAX_PARTY_MEMBERS;
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

        /// <summary>
        ///     Injures all party members due to rough sea
        /// </summary>
        public void RoughSeasInjure()
        {
            Random ran = new();
            foreach (PlayerCharacterRecord record in Records)
            {
                record.Stats.CurrentHp -= ran.Next(1, 9);
            }
        }

        public void SendCharacterToInn(PlayerCharacterRecord record,
            SmallMapReferences.SingleMapReference.Location location)
        {
            record.SendCharacterToInn(location);
        }

        public void SwapPositions(int nFirstPos, int nSecondPos)
        {
            int nActiveRecs = TotalPartyMembers();
            if (nFirstPos > nActiveRecs || nSecondPos > nActiveRecs)
                throw new Ultima5ReduxException("Asked to swap a party member who doesn't exist First:#" + nFirstPos +
                                                " Second:#" + nSecondPos);

            (Records[nFirstPos], Records[nSecondPos]) = (Records[nSecondPos], Records[nFirstPos]);
        }

        public int TotalPartyMembers()
        {
            int nPartyMembers = Records.Count(characterRecord => characterRecord.PartyStatus ==
                                                                 PlayerCharacterRecord.CharacterPartyStatus.InTheParty);

            Debug.Assert(nPartyMembers > 0 && nPartyMembers <= 6);
            return nPartyMembers;
        }
    }
}