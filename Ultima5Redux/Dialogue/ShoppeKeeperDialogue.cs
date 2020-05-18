using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Ultima5Redux.Data;

namespace Ultima5Redux.Dialogue
{
    public class ShoppeKeeperDialogue
    {
        
        private enum ShoppeKeeperChunkNames { Unused, AllData }
        private readonly DataChunks<ShoppeKeeperChunkNames> _dataChunks;
        private readonly List<string> _merchantStrings = new List<string>();
        private readonly Random _random = new Random();
        
        private const int PISSED_OFF_START = 0;
        private const int PISSED_OFF_STOP = 3;
        private const int HAPPY_START = 4;
        private const int HAPPY_STOP = 7;

        /// <summary>
        /// Construct using the on disk references
        /// </summary>
        /// <param name="u5Directory"></param>
        /// <param name="dataOvlReference"></param>
        public ShoppeKeeperDialogue(string u5Directory, DataOvlReference dataOvlReference)
        {
            string shoppeKeeperDataFilePath = Path.Combine(u5Directory, FileConstants.SHOPPE_DAT);
            
            _dataChunks = new DataChunks<ShoppeKeeperChunkNames>(shoppeKeeperDataFilePath, ShoppeKeeperChunkNames.Unused);
            
            _dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "All Shoppe Keeper Conversations", 0x00, 0x2797, 0, ShoppeKeeperChunkNames.AllData);
            
            BuildConversationTable(dataOvlReference);
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
        private string GetMerchantString(int nDialogueIndex, int nGold = -1, string equipmentName = "")
        {
            // % is gold
            // & is current piece of equipment
            // # current business (maybe with apostrophe s)
            // $ merchants name
            // @ barkeeps food/drink etc
            // * location of thing
            // ^ quantity of thing (ie. reagent)
            
            string merchantStr = GetMerchantStringWithNoSubstitution(nDialogueIndex);
            StringBuilder sb = new StringBuilder(merchantStr);
            if (nGold > 0)
            {
                sb.Replace("%", nGold.ToString());
            }

            if (equipmentName != "")
            {
                sb.Replace("&", equipmentName);
            }

            return sb.ToString();
        }
        
        /// <summary>
        /// Gets merchant response to asking to buy a piece of equipment
        /// </summary>
        /// <param name="nDialogueIndex">index into dialogue array</param>
        /// <param name="nGold">how much gold will it cost?</param>
        /// <returns>the complete response string</returns>
        public string GetEquipmentBuyingOutput(int nDialogueIndex, int nGold)
        {
            Debug.Assert(nDialogueIndex >= 8 && nDialogueIndex <= 48);
            Debug.Assert(CountReplacementVariables(nDialogueIndex) == 1);
            
            return GetMerchantString(nDialogueIndex, nGold:nGold);
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
        
        
    }
}