using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.Dialogue
{
    public class ShoppeKeeperDialogueReference
    {
        private readonly DataOvlReference _dataOvlReference;

        private enum ShoppeKeeperChunkNames { Unused, AllData }
        private readonly DataChunks<ShoppeKeeperChunkNames> _dataChunks;
        private readonly List<string> _merchantStrings = new List<string>();
        private readonly Random _random = new Random();
        private readonly ShoppeKeeperReferences _shoppeKeeperReferences;
        private readonly Inventory _inventory;
        
        private readonly Dictionary<int, int> _previousRandomSelectionByMin =
            new Dictionary<int, int>();

        /// <summary>
        /// Construct using the on disk references
        /// </summary>
        /// <param name="u5Directory"></param>
        /// <param name="dataOvlReference"></param>
        /// <param name="npcReferences"></param>
        /// <param name="inventory"></param>
        public ShoppeKeeperDialogueReference(string u5Directory, DataOvlReference dataOvlReference, NonPlayerCharacterReferences npcReferences, Inventory inventory)
        {
            _dataOvlReference = dataOvlReference;
            string shoppeKeeperDataFilePath = Path.Combine(u5Directory, FileConstants.SHOPPE_DAT);

            _inventory = inventory;
            
            _dataChunks = new DataChunks<ShoppeKeeperChunkNames>(shoppeKeeperDataFilePath, ShoppeKeeperChunkNames.Unused);
            
            _dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "All Shoppe Keeper Conversations", 0x00, 0x2797, 0, ShoppeKeeperChunkNames.AllData);
            
            BuildConversationTable(dataOvlReference);
            
            _shoppeKeeperReferences = new ShoppeKeeperReferences(dataOvlReference, npcReferences);
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
                Console.WriteLine((i++) + @","+@""""+convertedStr.Replace('\n', '_').Replace('"', '+')+@"""");
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

        internal int CountReplacementVariables(int nDialogueIndex)
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
        /// <param name="bUseRichText"></param>
        /// <param name="shoppeKeeperName"></param>
        /// <param name="shoppeName"></param>
        /// <param name="tod"></param>
        /// <param name="nQuantity"></param>
        /// <returns>a complete string with full replacements</returns>
        internal string GetMerchantString(int nDialogueIndex, int nGold = -1, string equipmentName = "", 
            bool bUseRichText = true, string shoppeKeeperName = "", string shoppeName = "", TimeOfDay tod = null, 
            int nQuantity = 0)
        {
            string merchantStr = GetMerchantStringWithNoSubstitution(nDialogueIndex);
            return GetMerchantString(merchantStr, nGold, equipmentName, bUseRichText, shoppeKeeperName,
                shoppeName, tod, nQuantity);
        }

        /// <summary>
        /// Gets the merchant string with full variable replacement 
        /// </summary>
        /// <param name="dialogue">string to do variable replacement on</param>
        /// <param name="nGold">how many gold to fill in</param>
        /// <param name="equipmentName"></param>
        /// <param name="bUseRichText"></param>
        /// <param name="shoppeKeeperName"></param>
        /// <param name="shoppeName"></param>
        /// <param name="tod"></param>
        /// <param name="nQuantity"></param>
        /// <returns>a complete string with full replacements</returns>
        internal static string GetMerchantString(string dialogue, int nGold = -1, string equipmentName = "", 
            bool bUseRichText = true, string shoppeKeeperName = "", string shoppeName = "", TimeOfDay tod = null,
            int nQuantity = 0)
        {
            // % is gold
            // & is current piece of equipment
            // # current business (maybe with apostrophe s)
            // $ merchants name
            // @ barkeeps food/drink etc
            // * location of thing
            // ^ quantity of thing (ie. reagent)
            const string HighlightColor = "<color=#00CC00>";
            //const string RegularColor = "<color=#FFFFFF>";
            const string QuantityColor = "<color=#00ffffff>";
            const string CloseColor = "</color>";

            StringBuilder sb = new StringBuilder(dialogue);
            if (nGold >= 0)
            {
                sb.Replace("%", HighlightColor+nGold.ToString()+CloseColor);
            }
            if (equipmentName != "")
            {
                sb.Replace("&", HighlightColor+equipmentName.ToString()+CloseColor);
            }
            if (shoppeKeeperName != "")
            {
                sb.Replace("$", shoppeKeeperName);
            }
            if (shoppeName != "")
            {
                sb.Replace("#", shoppeName);
            }
            if (tod != null)
            {
                sb.Replace("@", tod.TimeOfDayName);
            }
            if (nQuantity > 0)
            {
                sb.Replace("^", QuantityColor+nQuantity.ToString()+CloseColor);
            }

            return sb.ToString();
        }

        internal int GetRandomMerchantStringIndexFromRange(int nMin, int nMax)
        {
            // if this hasn't been access before, then lets add a chunk to make sure we don't repeat the same thing 
            // twice in a row
            if (!_previousRandomSelectionByMin.ContainsKey(nMin))
            {
                _previousRandomSelectionByMin.Add(nMin, -1);
            }

            Debug.Assert(nMin < nMax);
            int nTotalResponses = nMax - nMin;
            
            int nResponseIndex = GetRandomIndexFromRange(nMin, nMax);
            
            // if this response is the same as the last response, then we add one and make sure it is still in bounds 
            // by modding it 
            if (nResponseIndex == _previousRandomSelectionByMin[nMin])
            {
                nResponseIndex =
                    nMin + ((nResponseIndex + 1) % nTotalResponses); 
            }

            _previousRandomSelectionByMin[nMin] = nResponseIndex;
            return nResponseIndex;
        }
        
        /// <summary>
        /// Returns an unsubstituted merchant string from within a particular range
        /// </summary>
        /// <param name="nMin"></param>
        /// <param name="nMax"></param>
        /// <returns></returns>
        internal string GetRandomMerchantStringFromRange(int nMin, int nMax)
        {
            return _merchantStrings[GetRandomMerchantStringIndexFromRange(nMin, nMax)];
        }
        
        /// <summary>
        /// Returns a random integer between two integers
        /// </summary>
        /// <param name="nMin"></param>
        /// <param name="nMax"></param>
        /// <returns></returns>
        internal int GetRandomIndexFromRange(int nMin, int nMax)
        {
            Debug.Assert(nMax > nMin);
            int nDiff = nMax - nMin;

            return _random.Next(nDiff) + nMin;
        }

        /// <summary>
        /// Gets a shoppekeeper based on location and NPC type
        /// </summary>
        /// <param name="location"></param>
        /// <param name="npcType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">couldn't find the shoppe keeper at that particular location</exception>
        public ShoppeKeeper GetShoppeKeeper(SmallMapReferences.SingleMapReference.Location location, NonPlayerCharacterReference.NPCDialogTypeEnum npcType)
        {
            switch (npcType)
            {
                case NonPlayerCharacterReference.NPCDialogTypeEnum.Blacksmith:
                    return new BlackSmith(this, _inventory,
                        _shoppeKeeperReferences.GetShoppeKeeperReference(location, npcType), _dataOvlReference);
                case NonPlayerCharacterReference.NPCDialogTypeEnum.Barkeeper:
                    return new BarKeeper(this, 
                        _shoppeKeeperReferences.GetShoppeKeeperReference(location, npcType), _dataOvlReference);
                case NonPlayerCharacterReference.NPCDialogTypeEnum.HorseSeller:
                    return new HorseSeller(this, 
                        _shoppeKeeperReferences.GetShoppeKeeperReference(location, npcType), _dataOvlReference);
                case NonPlayerCharacterReference.NPCDialogTypeEnum.Shipwright:
                    return new Shipwright(this, 
                        _shoppeKeeperReferences.GetShoppeKeeperReference(location, npcType), _dataOvlReference);
                case NonPlayerCharacterReference.NPCDialogTypeEnum.Healer:
                    return new Healer(this, 
                        _shoppeKeeperReferences.GetShoppeKeeperReference(location, npcType), _dataOvlReference);
                case NonPlayerCharacterReference.NPCDialogTypeEnum.InnKeeper:
                    return new Innkeeper(this, 
                        _shoppeKeeperReferences.GetShoppeKeeperReference(location, npcType), _dataOvlReference);
                case NonPlayerCharacterReference.NPCDialogTypeEnum.MagicSeller:
                    return new MagicSeller(this, _inventory,
                        _shoppeKeeperReferences.GetShoppeKeeperReference(location, npcType), _dataOvlReference);
                case NonPlayerCharacterReference.NPCDialogTypeEnum.GuildMaster:
                    return new GuildMaster(this, 
                        _shoppeKeeperReferences.GetShoppeKeeperReference(location, npcType), _dataOvlReference);
                default:
                    throw new ArgumentOutOfRangeException(nameof(npcType), npcType, null);
            }
        }
    }
}