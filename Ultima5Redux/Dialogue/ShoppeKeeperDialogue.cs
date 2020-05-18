using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        
    }
}