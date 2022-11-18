using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References.Dialogue;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class ShoppeKeeperDialogueReference
    {
        private enum ShoppeKeeperChunkNames { Unused, AllData }

        private readonly DataChunks<ShoppeKeeperChunkNames> _dataChunks;
        private readonly DataOvlReference _dataOvlReference;
        private readonly List<string> _merchantStrings = new();

        private readonly Dictionary<int, int> _previousRandomSelectionByMin = new();

        private readonly Random _random = new();

        /// <summary>
        ///     Construct using the on disk references
        /// </summary>
        /// <param name="u5Directory"></param>
        /// <param name="dataOvlReference"></param>
        public ShoppeKeeperDialogueReference(string u5Directory, DataOvlReference dataOvlReference)
        {
            _dataOvlReference = dataOvlReference;

            _dataChunks = new DataChunks<ShoppeKeeperChunkNames>(u5Directory, FileConstants.SHOPPE_DAT,
                ShoppeKeeperChunkNames.Unused);

            _dataChunks.AddDataChunk(DataChunk.DataFormatType.StringList, "All Shoppe Keeper Conversations", 0x00,
                0x2797, 0, ShoppeKeeperChunkNames.AllData);

            BuildConversationTable(dataOvlReference);
        }

        /// <summary>
        ///     Gets the merchant string with full variable replacement
        /// </summary>
        /// <param name="dialogue">string to do variable replacement on</param>
        /// <param name="nGold">how many gold to fill in</param>
        /// <param name="equipmentName"></param>
        /// <param name="bUseRichText"></param>
        /// <param name="shoppeKeeperName"></param>
        /// <param name="shoppeName"></param>
        /// <param name="tod"></param>
        /// <param name="nQuantity"></param>
        /// <param name="genderedAddress"></param>
        /// <param name="personOfInterest"></param>
        /// <param name="locationToFindPersonOfInterest"></param>
        /// <returns>a complete string with full replacements</returns>
        internal static string GetMerchantString(string dialogue, int nGold = -1, string equipmentName = "",
            bool bUseRichText = true, string shoppeKeeperName = "", string shoppeName = "", TimeOfDay tod = null,
            int nQuantity = 0, string genderedAddress = "", string personOfInterest = "",
            string locationToFindPersonOfInterest = "")
        {
            // % is gold
            // & is current piece of equipment
            // # current business (maybe with apostrophe s)
            // $ merchants name
            // @ barkeeps food/drink etc
            // * location of thing
            // ^ quantity of thing (ie. reagent)
            string highlightColor = bUseRichText ? "<color=#00CC00>" : "";
            string quantityColor = bUseRichText ? "<color=#00ffffff>" : "";
            string closeColor = bUseRichText ? "</color>" : "";

            StringBuilder sb = new(dialogue);
            if (nGold >= 0) sb.Replace("%", highlightColor + nGold + closeColor);
            if (equipmentName != "") sb.Replace("&", highlightColor + equipmentName + closeColor);
            if (shoppeKeeperName != "") sb.Replace("$", shoppeKeeperName);
            if (shoppeName != "") sb.Replace("#", shoppeName);
            if (genderedAddress != "") sb.Replace(char.ToString((char)20), genderedAddress);
            if (tod != null) sb.Replace("@", tod.TimeOfDayName);

            if (personOfInterest != "") sb.Replace("&", quantityColor + personOfInterest + closeColor);
            if (locationToFindPersonOfInterest != "")
                sb.Replace("*", highlightColor + locationToFindPersonOfInterest + closeColor);
            if (nQuantity > 0) sb.Replace("^", quantityColor + nQuantity + closeColor);

            return sb.ToString();
        }

        internal int CountReplacementVariables(int nDialogueIndex)
        {
            int freq = Regex.Matches(GetMerchantString(nDialogueIndex), @"[\%\&\$\#\@\*\^\*]").Count;
            return freq;
        }

        /// <summary>
        ///     Gets the merchant string with full variable replacement
        /// </summary>
        /// <param name="nDialogueIndex">index into un-replaced strings</param>
        /// <param name="nGold">how many gold to fill in</param>
        /// <param name="equipmentName"></param>
        /// <param name="bUseRichText"></param>
        /// <param name="shoppeKeeperName"></param>
        /// <param name="shoppeName"></param>
        /// <param name="tod"></param>
        /// <param name="nQuantity"></param>
        /// <param name="genderedAddress"></param>
        /// <param name="personOfInterest"></param>
        /// <param name="locationToFindPersonOfInterest"></param>
        /// <param name="bHighlightDetails"></param>
        /// <returns>a complete string with full replacements</returns>
        internal string GetMerchantString(int nDialogueIndex, int nGold = -1, string equipmentName = "",
            bool bUseRichText = true, string shoppeKeeperName = "", string shoppeName = "", TimeOfDay tod = null,
            int nQuantity = 0, string genderedAddress = "", string personOfInterest = "",
            string locationToFindPersonOfInterest = "", bool bHighlightDetails = false)
        {
            string merchantStr = GetMerchantStringWithNoSubstitution(nDialogueIndex);
            return GetMerchantString(merchantStr, nGold, equipmentName, bUseRichText, shoppeKeeperName, shoppeName, tod,
                nQuantity, genderedAddress, personOfInterest, locationToFindPersonOfInterest);
        }

        /// <summary>
        ///     Returns a random integer between two integers
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
        ///     Returns an unsubstituted merchant string from within a particular range
        /// </summary>
        /// <param name="nMin"></param>
        /// <param name="nMax"></param>
        /// <returns></returns>
        internal string GetRandomMerchantStringFromRange(int nMin, int nMax) =>
            _merchantStrings[GetRandomMerchantStringIndexFromRange(nMin, nMax)];

        internal int GetRandomMerchantStringIndexFromRange(int nMin, int nMax)
        {
            // if this hasn't been access before, then lets add a chunk to make sure we don't repeat the same thing 
            // twice in a row
            if (!_previousRandomSelectionByMin.ContainsKey(nMin)) _previousRandomSelectionByMin.Add(nMin, -1);

            Debug.Assert(nMin < nMax);
            int nTotalResponses = nMax - nMin;

            int nResponseIndex = GetRandomIndexFromRange(nMin, nMax);

            // if this response is the same as the last response, then we add one and make sure it is still in bounds 
            // by modding it 
            if (nResponseIndex == _previousRandomSelectionByMin[nMin])
                nResponseIndex = nMin + (nResponseIndex + 1) % nTotalResponses;

            _previousRandomSelectionByMin[nMin] = nResponseIndex;
            return nResponseIndex;
        }

        /// <summary>
        ///     Decompresses the strings and fills in each compressed word
        /// </summary>
        /// <param name="dataOvlReference"></param>
        private void BuildConversationTable(DataOvlReference dataOvlReference)
        {
            IEnumerable<string> rawShoppeStrings = _dataChunks.GetDataChunk(ShoppeKeeperChunkNames.AllData)
                .GetChunkAsStringList().StringList;
            CompressedWordReference compressedWordReference = new(dataOvlReference);
            foreach (string rawShoppeString in rawShoppeStrings)
            {
                string convertedStr =
                    compressedWordReference.ReplaceRawMerchantStringsWithCompressedWords(rawShoppeString);
                _merchantStrings.Add(convertedStr);
            }
        }

        /// <summary>
        ///     Gets the merchant string before substitutions
        /// </summary>
        /// <param name="nDialogueIndex"></param>
        /// <returns></returns>
        private string GetMerchantStringWithNoSubstitution(int nDialogueIndex) => _merchantStrings[nDialogueIndex];

        /// <summary>
        ///     Gets a shoppekeeper based on location and NPC type
        /// </summary>
        /// <param name="location"></param>
        /// <param name="specificNpcType"></param>
        /// <param name="playerCharacterRecords"></param>
        /// <param name="inventory"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">couldn't find the shoppe keeper at that particular location</exception>
        public ShoppeKeeper GetShoppeKeeper(SmallMapReferences.SingleMapReference.Location location,
            NonPlayerCharacterReference.SpecificNpcDialogType specificNpcType,
            PlayerCharacterRecords playerCharacterRecords, Inventory inventory)
        {
            switch (specificNpcType)
            {
                case NonPlayerCharacterReference.SpecificNpcDialogType.Blacksmith:
                    return new BlackSmith(this, inventory,
                        GameReferences.Instance.ShoppeKeeperRefs.GetShoppeKeeperReference(location, specificNpcType),
                        _dataOvlReference);
                case NonPlayerCharacterReference.SpecificNpcDialogType.Barkeeper:
                    return new BarKeeper(this,
                        GameReferences.Instance.ShoppeKeeperRefs.GetShoppeKeeperReference(location, specificNpcType),
                        _dataOvlReference);
                case NonPlayerCharacterReference.SpecificNpcDialogType.HorseSeller:
                    return new HorseSeller(this,
                        GameReferences.Instance.ShoppeKeeperRefs.GetShoppeKeeperReference(location, specificNpcType),
                        _dataOvlReference,
                        playerCharacterRecords);
                case NonPlayerCharacterReference.SpecificNpcDialogType.Shipwright:
                    return new Shipwright(this,
                        GameReferences.Instance.ShoppeKeeperRefs.GetShoppeKeeperReference(location, specificNpcType),
                        _dataOvlReference);
                case NonPlayerCharacterReference.SpecificNpcDialogType.Healer:
                    return new Healer(this,
                        GameReferences.Instance.ShoppeKeeperRefs.GetShoppeKeeperReference(location, specificNpcType),
                        _dataOvlReference);
                case NonPlayerCharacterReference.SpecificNpcDialogType.InnKeeper:
                    return new Innkeeper(this,
                        GameReferences.Instance.ShoppeKeeperRefs.GetShoppeKeeperReference(location, specificNpcType),
                        _dataOvlReference);
                case NonPlayerCharacterReference.SpecificNpcDialogType.MagicSeller:
                    return new MagicSeller(this, inventory,
                        GameReferences.Instance.ShoppeKeeperRefs.GetShoppeKeeperReference(location, specificNpcType),
                        _dataOvlReference);
                case NonPlayerCharacterReference.SpecificNpcDialogType.GuildMaster:
                    return new GuildMaster(this,
                        GameReferences.Instance.ShoppeKeeperRefs.GetShoppeKeeperReference(location, specificNpcType),
                        _dataOvlReference);
                default:
                    throw new ArgumentOutOfRangeException(nameof(specificNpcType), specificNpcType, null);
            }
        }
    }
}