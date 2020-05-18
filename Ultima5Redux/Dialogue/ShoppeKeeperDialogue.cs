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

        private string GetMerchantStringWithNoSubstitution(int nIndex)
        {
            return _merchantStrings[nIndex];
        }

        private int CountReplacementVariables(int nDialogueIndex)
        {
            int freq = Regex.Matches(GetMerchantString(nDialogueIndex), @"[\%\&\$\#\@\*\^]").Count;
            return freq;
            // character == '%' || character == '&' || character == '$' || character == '#' || character == '@'
            //        || character == '*' || character == '^';
        }
        
        /// <summary>
        /// Gets the merchant string with full variable replacement 
        /// </summary>
        /// <param name="nDialogueIndex">index into un-replaced strings</param>
        /// <param name="nGold">how many gold to fill in</param>
        /// <returns>a complete string with full replacements</returns>
        private string GetMerchantString(int nDialogueIndex, int nGold = -1)
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

            return sb.ToString();
        }
        
        public string GetEquipmentBuyingOutput(int nDialogueIndex, int nGold)
        {
            Debug.Assert(nDialogueIndex >= 8 && nDialogueIndex <= 48);
            Debug.Assert(CountReplacementVariables(nDialogueIndex) == 1);
            
            return GetMerchantString(nDialogueIndex, nGold:nGold);
        }
    }
}