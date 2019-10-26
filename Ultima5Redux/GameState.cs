using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class GameState
    {
        #region Private Variables
        /// <summary>
        /// A random number generator - capable of seeding in future
        /// </summary>
        private Random ran = new Random();

        /// <summary>
        /// Game State raw data
        /// </summary>
        private DataChunks<DataChunkName> dataChunks;

        /// <summary>
        /// 2D array of flag indicating if an NPC is met [mastermap][npc#]
        /// </summary>
        private bool[][] npcIsMetArray;

        /// <summary>
        /// 2D array of flag indicating if an NPC is dead [mastermap][npc#]
        /// </summary>
        private bool[][] npcIsDeadArray;

        /// <summary>
        /// All player character records
        /// </summary>
        public CharacterRecords CharacterRecords { get; }
        #endregion

        #region Public Properties
        public string FormattedDate
        {
            get
            {
                return Month + "-" + Day + "-" + Year;
            }
        }

        public string FormattedTime
        {
            get
            {
                string suffix;
                if (Hour < 12) suffix = "AM"; else suffix = "PM";
                return Hour % 12 + ":" + String.Format("{0:D2}", Minute) + " " + suffix;
            }
        }

        
        public UInt16 Year
        {
            get
            {
                return dataChunks.GetDataChunk(DataChunkName.CURRENT_YEAR).GetChunkAsUINT16();
            }
            set
            {
                dataChunks.GetDataChunk(DataChunkName.CURRENT_YEAR).SetChunkAsUINT16(value);
            }
        }

        public byte Month
        {
            get
            {
                return dataChunks.GetDataChunk(DataChunkName.CURRENT_MONTH).GetChunkAsByte();
            }
            set
            {
                dataChunks.GetDataChunk(DataChunkName.CURRENT_MONTH).SetChunkAsByte(value);
            }
        }

        public byte Day
        {
            get
            {
                return dataChunks.GetDataChunk(DataChunkName.CURRENT_DAY).GetChunkAsByte();
            }
            set
            {
                dataChunks.GetDataChunk(DataChunkName.CURRENT_DAY).SetChunkAsByte(value);
            }
        }

        public byte Hour
        {
            get
            {
                return dataChunks.GetDataChunk(DataChunkName.CURRENT_HOUR).GetChunkAsByte();
            }
            set
            {
                Debug.Assert(value >= 0 && value <= 23);
                dataChunks.GetDataChunk(DataChunkName.CURRENT_HOUR).SetChunkAsByte(value);
            }
        }

        public byte Minute
        {
            get
            {
                return dataChunks.GetDataChunk(DataChunkName.CURRENT_MINUTE).GetChunkAsByte();
            }
            set
            {
                Debug.Assert(value >= 0 && value <= 59);
                dataChunks.GetDataChunk(DataChunkName.CURRENT_MINUTE).SetChunkAsByte(value);
            }
        }

        public byte Gems
        {
            get
            {
                return dataChunks.GetDataChunk(DataChunkName.GEMS_QUANTITY).GetChunkAsByte();
            }
            set
            {
                dataChunks.GetDataChunk(DataChunkName.GEMS_QUANTITY).SetChunkAsByte(value);
            }
        }
        public byte Torches
        {
            get
            {
                return dataChunks.GetDataChunk(DataChunkName.TORCHES_QUANTITY).GetChunkAsByte();
            }
            set
            {
                dataChunks.GetDataChunk(DataChunkName.TORCHES_QUANTITY).SetChunkAsByte(value);

            }
        }
        public byte Keys
        {
            get
            {
                return dataChunks.GetDataChunk(DataChunkName.KEYS_QUANTITY).GetChunkAsByte();
            }
            set
            {
                dataChunks.GetDataChunk(DataChunkName.KEYS_QUANTITY).SetChunkAsByte(value);

            }
        }
        public UInt16 Gold 
        {
            get
            {
                return dataChunks.GetDataChunk(DataChunkName.GOLD_QUANTITY).GetChunkAsUINT16();
            }
            set
            {
                dataChunks.GetDataChunk(DataChunkName.GOLD_QUANTITY).SetChunkAsUINT16(value);

            }
        }

        public UInt16 Food
        {
            get
            {
                return dataChunks.GetDataChunk(DataChunkName.FOOD_QUANTITY).GetChunkAsUINT16();
            }
            set
            {
                dataChunks.GetDataChunk(DataChunkName.FOOD_QUANTITY).SetChunkAsUINT16(value);
            }
        }


        /// <summary>
        /// Users Karma
        /// </summary>
        public uint Karma { get; set; }

        /// <summary>
        /// The name of the Avatar
        /// </summary>
        public string AvatarsName { get { return CharacterRecords.Records[CharacterRecords.AVATAR_RECORD].Name; } }
        #endregion

        #region Enumerations
        /// <summary>
        /// Data chunks for each of the save game sections
        /// </summary>
        public enum DataChunkName
        {
            Unused,
            CHARACTER_RECORDS,
            NPC_ISALIVE_TABLE,
            NPC_ISMET_TABLE,
            N_PEOPLE_PARTY,
            FOOD_QUANTITY,
            GOLD_QUANTITY,
            KEYS_QUANTITY,
            GEMS_QUANTITY,
            TORCHES_QUANTITY,
            CURRENT_YEAR,
            CURRENT_MONTH,
            CURRENT_DAY,
            CURRENT_HOUR,
            CURRENT_MINUTE
        };
        #endregion

        #region Constructors
        /// <summary>
        /// Construct the GameState
        /// </summary>
        /// <param name="u5Directory">Directory of the game State files</param>
        public GameState(string u5Directory)
        {
            string saveFileAndPath = Path.Combine(u5Directory, FileConstants.SAVED_GAM);

            dataChunks = new DataChunks<DataChunkName>(saveFileAndPath, DataChunkName.Unused);

            List<byte> gameStateByteArray;
            gameStateByteArray = Utils.GetFileAsByteList(saveFileAndPath);

            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "All Character Records (ie. name, stats)", 0x02, 0x20*16, 0x00, DataChunkName.CHARACTER_RECORDS);
            DataChunk rawCharacterRecords = dataChunks.GetDataChunk(DataChunkName.CHARACTER_RECORDS);
            CharacterRecords = new CharacterRecords(rawCharacterRecords.GetAsByteList());

            // quantities of standard items
            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16, "Food Quantity", 0x202, 0x02, 0x00, DataChunkName.FOOD_QUANTITY);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16, "Gold Quantity", 0x204, 0x02, 0x00, DataChunkName.GOLD_QUANTITY);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Keys Quantity", 0x206, 0x01, 0x00, DataChunkName.KEYS_QUANTITY);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Gems Quantity", 0x207, 0x01, 0x00, DataChunkName.GEMS_QUANTITY);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Torches Quantity", 0x208, 0x01, 0x00, DataChunkName.TORCHES_QUANTITY);

            // time and date
            dataChunks.AddDataChunk(DataChunk.DataFormatType.UINT16, "Current Year", 0x2CE, 0x02, 0x00, DataChunkName.CURRENT_YEAR);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Month", 0x2D7, 0x01, 0x00, DataChunkName.CURRENT_MONTH);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Day", 0x2D8, 0x01, 0x00, DataChunkName.CURRENT_DAY);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Hour", 0x2D9, 0x01, 0x00, DataChunkName.CURRENT_HOUR);
            // 0x2DA is copy of 2D9 for some reason
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Byte, "Current Hour", 0x2DB, 0x01, 0x00, DataChunkName.CURRENT_MINUTE);


            //dataChunks.AddDataChunk()
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Bitmap, "NPC Killed Bitmap", 0x5B4, 0x80, 0x00, DataChunkName.NPC_ISALIVE_TABLE);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Bitmap, "NPC Met Bitmap", 0x634, 0x80, 0x00, DataChunkName.NPC_ISMET_TABLE);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.ByteList, "Number of Party Members", 0x2B5, 0x1, 0x00, DataChunkName.N_PEOPLE_PARTY);

            // Initialize the table to determine if an NPC is dead
            List<bool> npcAlive = dataChunks.GetDataChunk(DataChunkName.NPC_ISALIVE_TABLE).GetAsBitmapBoolList();
            npcIsDeadArray = Utils.ListTo2DArray<bool>(npcAlive, NonPlayerCharacters.NPCS_PER_TOWN, 0x00, NonPlayerCharacters.NPCS_PER_TOWN * SmallMapReference.SingleMapReference.TOTAL_SMALL_MAP_LOCATIONS);

            // Initialize a table to determine if an NPC has been met
            List<bool> npcMet = dataChunks.GetDataChunk(DataChunkName.NPC_ISMET_TABLE).GetAsBitmapBoolList();
            // these will map directly to the towns and the NPC dialog #
            npcIsMetArray = Utils.ListTo2DArray<bool>(npcMet, NonPlayerCharacters.NPCS_PER_TOWN, 0x00, NonPlayerCharacters.NPCS_PER_TOWN * SmallMapReference.SingleMapReference.TOTAL_SMALL_MAP_LOCATIONS);
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Advances time and takes care of all day, month, year calculations
        /// </summary>
        /// <param name="nMinutes">Number of minutes to advance (maximum of 9*60)</param>
        public void AdvanceTime(int nMinutes)
        {
            // ensuring that you can't advance more than a day ensures that we can make some time saving assumptions
            if (nMinutes > (60 * 9)) throw new Exception("You can not advance more than 9 hours at a time");

            // if we add the time, and it enters the next hour then we have some work to do
            if (Minute + nMinutes > 59)
            {
                int nExtraMinutes;
                byte nHours = (byte)Math.DivRem(nMinutes, 60, out nExtraMinutes);

                byte newHour = (byte)((Hour + nHours + 1));//% 24);
                Minute = (byte)((Minute + (byte)nExtraMinutes) % 60);

                // if it puts us into a new day
                if (newHour <= 23)
                {
                    Hour = newHour;
                }
                else
                {
                    Hour = (byte)(newHour % 24);
                    // if the day + 1 is more days then we are allow in the month, then restart the days, and go to next month
                    int nDay = (byte)(Day + 1);
                    if (nDay > 28)
                    {
                        Day = 1;
                        int nMonth = (byte)(Month + 1);
                        // if the next month goes beyond 13, then we reset and advance the year
                        if (nMonth > 13)
                        {
                            Month = 1;
                            Year += 1;
                        }
                        else
                        {
                            Month += 1;
                        }
                    }
                    else
                    {
                        Day = (byte)(Day + 1);
                    }
                }
            }
            else
            {
                Minute += (byte)nMinutes;
            }
        }

        /// <summary>
        /// Using the random number generator, provides 1 in howMany odds of returning true
        /// </summary>
        /// <param name="howMany">1 in howMany odds of returning true</param>
        /// <returns>true if odds are beat</returns>
        public bool OneInXOdds(int howMany)
        {
            // if ran%howMany is zero then we beat the odds
            int nextRan = ran.Next();
            return ((nextRan % howMany) == 0);
        }

        /// <summary>
        /// Is NPC alive?
        /// </summary>
        /// <param name="npc">NPC object</param>
        /// <returns>true if NPC is alive</returns>
        public bool NpcIsAlive(NonPlayerCharacters.NonPlayerCharacter npc)
        {
            // the array isDead becasue LB stores 0=alive, 1=dead
            // I think it's easier to evaluate if they are alive
            return npcIsDeadArray[npc.MapReference.Id][npc.DialogIndex]==false;
        }

        /// <summary>
        /// Sets the flag to indicate the NPC is met
        /// </summary>
        /// <param name="npc"></param>
        public void SetMetNPC(NonPlayerCharacters.NonPlayerCharacter npc)
        {
            npcIsMetArray[npc.MapReference.Id][npc.DialogIndex] = true;
        }

    public List<CharacterRecord> GetActiveCharacterRecords()
    {
        List<CharacterRecord> activeCharacterRecords = new List<CharacterRecord>();

        foreach (CharacterRecord characterRecord in CharacterRecords.Records)
        {
            if (characterRecord.PartyStatus == CharacterRecord.CharacterPartyStatus.InParty)
                activeCharacterRecords.Add(characterRecord);
        }
        if (activeCharacterRecords.Count == 0) throw new Exception("Even the Avatar is dead, no records returned in active party");
        if (activeCharacterRecords.Count > CharacterRecords.MAX_PARTY_MEMBERS) throw new Exception("There are too many party members in the party... party...");

        return activeCharacterRecords;
    }

    public CharacterRecord GetCharacterFromParty(int nPosition)
        {
            Debug.Assert(nPosition > 0 && nPosition < CharacterRecords.MAX_PARTY_MEMBERS, "There are a maximum of 6 characters");
            Debug.Assert(nPosition >= CharacterRecords.TotalPartyMembers(), "You cannot request a character that isn't on the roster");

            int nPartyMember = 0;
            foreach (CharacterRecord characterRecord in CharacterRecords.Records)
            {
                if (characterRecord.PartyStatus == CharacterRecord.CharacterPartyStatus.InParty)
                    if (nPartyMember++ == nPosition) return characterRecord;
            }
            throw new Exception("I've asked for member of the party who is aparently not there...");
        }

        /// <summary>
        /// Adds an NPC character to the party, and maps their CharacterRecord
        /// </summary>
        /// <param name="npc">the NPC to add</param>
        public void AddMemberToParty(NonPlayerCharacters.NonPlayerCharacter npc)
        {
            CharacterRecord record = CharacterRecords.GetCharacterRecordByNPC(npc);
            record.PartyStatus = CharacterRecord.CharacterPartyStatus.InParty;
        }

        /// <summary>
        /// Is my party full (at capacity)
        /// </summary>
        /// <returns>true if party is full</returns>
        public bool IsFullParty()
        {
            Debug.Assert(!(CharacterRecords.TotalPartyMembers() > CharacterRecords.MAX_PARTY_MEMBERS), "You have more party members than you should.");
            return (CharacterRecords.TotalPartyMembers() == CharacterRecords.MAX_PARTY_MEMBERS);
        }

        /// <summary>
        /// Has the NPC met the avatar yet?
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public bool NpcHasMetAvatar(NonPlayerCharacters.NonPlayerCharacter npc)
        {
            return npcIsMetArray[npc.MapReference.Id][npc.DialogIndex];
        }

        /// <summary>
        /// DEBUG FUNCTION
        /// </summary>
        /// <param name="npc"></param>
        public void SetNpcHasMetAvatar(NonPlayerCharacters.NonPlayerCharacter npc, bool hasMet)
        {
            npcIsMetArray[npc.MapReference.Id][npc.DialogIndex] = hasMet;
        }

        #endregion
    }
}
