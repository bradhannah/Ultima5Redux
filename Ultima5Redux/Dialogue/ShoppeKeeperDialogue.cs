using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.MapCharacters;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.Dialogue
{
    public class ShoppeKeeperDialogue
    {
        private readonly DataOvlReference _dataOvlReference;

        private enum ShoppeKeeperChunkNames { Unused, AllData }
        private readonly DataChunks<ShoppeKeeperChunkNames> _dataChunks;
        private readonly List<string> _merchantStrings = new List<string>();
        private readonly Dictionary<int, int> _equipmentMapToMerchantStrings = new Dictionary<int, int>();
        private readonly Random _random = new Random();
        private readonly ShoppeKeeperReferences _shoppeKeeperReferences;
        
        private const int PISSED_OFF_START = 0;
        private const int PISSED_OFF_STOP = 3;
        private const int HAPPY_START = 4;
        private const int HAPPY_STOP = 7;

        /// <summary>
        /// Construct using the on disk references
        /// </summary>
        /// <param name="u5Directory"></param>
        /// <param name="dataOvlReference"></param>
        /// <param name="npcReferences"></param>
        public ShoppeKeeperDialogue(string u5Directory, DataOvlReference dataOvlReference, NonPlayerCharacterReferences npcReferences, Inventory inventory)
        {
            _dataOvlReference = dataOvlReference;
            string shoppeKeeperDataFilePath = Path.Combine(u5Directory, FileConstants.SHOPPE_DAT);
            
            _dataChunks = new DataChunks<ShoppeKeeperChunkNames>(shoppeKeeperDataFilePath, ShoppeKeeperChunkNames.Unused);
            
            _dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "All Shoppe Keeper Conversations", 0x00, 0x2797, 0, ShoppeKeeperChunkNames.AllData);
            
            BuildConversationTable(dataOvlReference);
            
            _shoppeKeeperReferences = new ShoppeKeeperReferences(dataOvlReference, npcReferences);

            // go through each of the pieces of equipment in order to build a map of equipment index
            // -> merchant string list
            int nEquipmentCounter = 0;
            foreach (DataOvlReference.Equipment equipment in Enum.GetValues((typeof(DataOvlReference.Equipment))))
            {
                // we only look at equipment up to SpikedCollars
                if ((int) equipment > (int) DataOvlReference.Equipment.SpikedCollar) continue;
                
                const int nEquipmentOffset = 8;

                CombatItem item = inventory.GetItemFromEquipment(equipment);
                if (item.BasePrice > 0)
                {
                    // add an equipment offset because equipment strings don't start at zero in the merchant strings
                    _equipmentMapToMerchantStrings.Add((int) equipment, nEquipmentCounter + nEquipmentOffset);
                    nEquipmentCounter++;
                }
            }
        }

        /// <summary>
        /// Decompresses the strings and fills in each compressed word
        /// </summary>
        /// <param name="dataOvlReference"></param>
        private void BuildConversationTable(DataOvlReference dataOvlReference)
        {
            IEnumerable<string> rawShoppeStrings = _dataChunks.GetDataChunk(ShoppeKeeperChunkNames.AllData).GetChunkAsStringList().Strs;
            CompressedWordReference compressedWordReference = new CompressedWordReference(dataOvlReference);
            int i = 0;
            foreach (string rawShoppeString in rawShoppeStrings)
            {
                string convertedStr = compressedWordReference.ReplaceRawMerchantStringsWithCompressedWords(rawShoppeString);
                Console.WriteLine((i++) + ","+"\""+convertedStr.Replace('\n', '_').Replace('"', '+')+"\"");
                _merchantStrings.Add(convertedStr);
            }
        }

        /// <summary>
        /// Gets the merchant string before substitutions
        /// </summary>
        /// <param name="nDialogueIndex"></param>
        /// <returns></returns>
        private string GetMerchantStringWithNoSubstitution(int nDialogueIndex)
        {
            return _merchantStrings[nDialogueIndex];
        }

        private int CountReplacementVariables(int nDialogueIndex)
        {
            int freq = Regex.Matches(GetMerchantString(nDialogueIndex), @"[\%\&\$\#\@\*\^]").Count;
            return freq;
        }

        /// <summary>
        /// Gets the merchant string with full variable replacement 
        /// </summary>
        /// <param name="nDialogueIndex">index into un-replaced strings</param>
        /// <param name="nGold">how many gold to fill in</param>
        /// <param name="equipmentName"></param>
        /// <returns>a complete string with full replacements</returns>
        private string GetMerchantString(int nDialogueIndex, int nGold = -1, string equipmentName = "", bool bUseRichText = true)
        {
            // % is gold
            // & is current piece of equipment
            // # current business (maybe with apostrophe s)
            // $ merchants name
            // @ barkeeps food/drink etc
            // * location of thing
            // ^ quantity of thing (ie. reagent)
            
            const string HIGHLIGHT_COLOR = "<color=#00CC00>";
            const string REGULAR_COLOR = "<color=#FFFFFF>";

            string merchantStr = GetMerchantStringWithNoSubstitution(nDialogueIndex);
            StringBuilder sb = new StringBuilder(merchantStr);
            if (nGold > 0)
            {
                sb.Replace("%", HIGHLIGHT_COLOR+nGold.ToString()+REGULAR_COLOR);
            }

            if (equipmentName != "")
            {
                sb.Replace("&", HIGHLIGHT_COLOR+equipmentName.ToString()+REGULAR_COLOR);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets merchant response to asking to buy a piece of equipment
        /// </summary>
        /// <param name="nEquipmentIndex">index into dialogue array</param>
        /// <param name="nGold">how much gold will it cost?</param>
        /// <param name="bUseRichText"></param>
        /// <returns>the complete response string</returns>
        private string GetEquipmentBuyingOutput(int nEquipmentIndex, int nGold, bool bUseRichText)
        {
            int nDialogueIndex = _equipmentMapToMerchantStrings[nEquipmentIndex];
            Debug.Assert(nEquipmentIndex >= 0 && nEquipmentIndex <= (int)DataOvlReference.Equipment.SpikedCollar);
            Debug.Assert(CountReplacementVariables(nDialogueIndex) == 1);
            return GetMerchantString(nDialogueIndex, nGold:nGold);
        }

        public string GetEquipmentBuyingOutput(DataOvlReference.Equipment equipment, int nGold, bool bUseRichText = true)
        {
            return GetEquipmentBuyingOutput((int) equipment, nGold, bUseRichText);
        }

        /// <summary>
        /// Gets the response string when vendor is trying to sell a particular piece of equipment
        /// </summary>
        /// <param name="nGold">how much to charge?</param>
        /// <param name="equipmentName">the name of the equipment</param>
        /// <returns>the complete response string</returns>
        public string GetEquipmentSellingOutput(int nGold, string equipmentName)
        {
            int sellStringIndex = GetRandomIndexFromRange(49, 56);
            
            return GetMerchantString(sellStringIndex, nGold: nGold, equipmentName: equipmentName);
        }

        /// <summary>
        /// Returns a random integer between two integers
        /// </summary>
        /// <param name="nMin"></param>
        /// <param name="nMax"></param>
        /// <returns></returns>
        private int GetRandomIndexFromRange(int nMin, int nMax)
        {
            Debug.Assert(nMax > nMin);
            int nDiff = nMax - nMin;

            return _random.Next(nDiff) + nMin;
        }
        
        /// <summary>
        /// Returns an unsubstituted merchant string from within a particular range
        /// </summary>
        /// <param name="nMin"></param>
        /// <param name="nMax"></param>
        /// <returns></returns>
        private string GetRandomMerchantStringFromRange(int nMin, int nMax)
        {
            return _merchantStrings[GetRandomIndexFromRange(nMin, nMax)];
        }
        
        /// <summary>
        /// Get a random response when the shoppekeeper gets pissed off at you
        /// </summary>
        /// <returns></returns>
        public string GetPissedOffShoppeKeeperGoodbyeResponse()
        {
            return GetRandomMerchantStringFromRange(PISSED_OFF_START, PISSED_OFF_STOP);
        }

        /// <summary>
        /// Get a random response when the shoppekeeper is happy as you leave
        /// </summary>
        /// <returns></returns>
        public string GetHappyShoppeKeeperGoodbyeResponse()
        {
            return GetRandomMerchantStringFromRange(HAPPY_START, HAPPY_STOP);
        }

        public string GetThanksAfterPurchaseResponse()
        {
            return "Thank thee kindly!";
        }

        public string GetPissedOffNotBuyingResponse()
        {
            return "Stop wasting my time!";
        }
        
        private string GetShoppeNameByLocation(SmallMapReferences.SingleMapReference.Location location, NonPlayerCharacterReference.NPCDialogTypeEnum npcType)
        {
            //_dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.STORE_NAMES).GetChunkAsStringList().
            return "";
        }

        private string GetTimeOfDayName(TimeOfDay tod)
        {
            if (tod.Hour > 5 && tod.Hour < 12) return "morning";
            if (tod.Hour >= 12 && tod.Hour < 6) return "afternoon";
            return "evening";
        }
        
        public string GetHelloResponse(SmallMapReferences.SingleMapReference.Location location, NonPlayerCharacterReference.NPCDialogTypeEnum npcType, TimeOfDay tod)
        {
            ShoppeKeeperReference shoppeKeeper = _shoppeKeeperReferences.GetShoppeKeeperReference(location, npcType);
            
            string response = @"Good "+GetTimeOfDayName(tod)+", and welcome to " +shoppeKeeper.ShoppeName + "!\n\n" + 
                              shoppeKeeper.ShoppeKeeperName + " says, \"Greetings traveller! Wish ye to Buy, or hast thou wares to Sell?\"";
            return response;
        }
        
        
    }
}