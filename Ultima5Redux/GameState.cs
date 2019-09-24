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
        private CharacterRecords characterRecords;
        #endregion

        #region Public Properties
        /// <summary>
        /// Users Karma
        /// </summary>
        public uint Karma { get; set; }

        /// <summary>
        /// The name of the Avatar
        /// </summary>
        public string AvatarsName { get { return characterRecords.Records[CharacterRecords.AVATAR_RECORD].Name; } }
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
            NPC_ISMET_TABLE
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
            characterRecords = new CharacterRecords(rawCharacterRecords.GetAsByteList());

            //dataChunks.AddDataChunk()
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Bitmap, "NPC Killed Bitmap", 0x5B4, 0x80, 0x00, DataChunkName.NPC_ISALIVE_TABLE);
            dataChunks.AddDataChunk(DataChunk.DataFormatType.Bitmap, "NPC Met Bitmap", 0x634, 0x80, 0x00, DataChunkName.NPC_ISMET_TABLE);

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

        /// <summary>
        /// Adds an NPC character to the party, and maps their CharacterRecord
        /// </summary>
        /// <param name="npc">the NPC to add</param>
        public void AddMemberToParty(NonPlayerCharacters.NonPlayerCharacter npc)
        {
            CharacterRecord record = characterRecords.GetCharacterRecordByNPC(npc);
            record.InnOrParty = (int)CharacterRecord.CharacterInnOrParty.InParty;
        }

        /// <summary>
        /// Is my party full (at capacity)
        /// </summary>
        /// <returns>true if party is full</returns>
        public bool IsFullParty()
        {
            Debug.Assert(!(characterRecords.TotalPartyMembers() > CharacterRecords.MAX_PARTY_MEMBERS), "You have more party members than you should.");
            return (characterRecords.TotalPartyMembers() == CharacterRecords.MAX_PARTY_MEMBERS);
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
