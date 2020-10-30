using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    /// <summary>
    /// Shoppe keeper options
    /// Each of these describe the different behaviours that the shoppe keeper is responsible for
    /// </summary>
    public class ShoppeKeeperOption
    {
        public enum DialogueType
        {
           None, OkGoodbye, BuyBlacksmith, SellBlacksmith, BuyMagicSeller, BuyShipwright,
             BuyBarkeeper, BuyHealer, BuyGuildMaster, RestInnkeeper, GossipInnkeeper, BuyHorses,
             DropOffPartyMemberInnkeeper 
        }

        public string ButtonName { get; }
        public DialogueType DialogueOption { get; }

        public ShoppeKeeperOption(string buttonName, DialogueType dialogueOption)
        {
            ButtonName = buttonName;
            DialogueOption = dialogueOption;
        }
    }
    
    /// <summary>
    /// A generic shoppe keeper with all common elements described
    /// </summary>
    public abstract class ShoppeKeeper
    {
        /// <summary>
        /// Construct a shoppe keeper
        /// </summary>
        /// <param name="shoppeKeeperDialogueReference"></param>
        /// <param name="theShoppeKeeperReference"></param>
        /// <param name="dataOvlReference"></param>
        protected ShoppeKeeper(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference)
        {
            ShoppeKeeperDialogueReference = shoppeKeeperDialogueReference;
            DataOvlReference = dataOvlReference;
            TheShoppeKeeperReference = theShoppeKeeperReference;
        }
        /// <summary>
        /// A list of the shoppe keeper options (abilities)
        /// </summary>
        public abstract List<ShoppeKeeperOption> ShoppeKeeperOptions { get; }

        public ShoppeKeeperReference TheShoppeKeeperReference { get; private set; }
        
        protected readonly ShoppeKeeperDialogueReference ShoppeKeeperDialogueReference;
        protected readonly DataOvlReference DataOvlReference;

        /// <summary>
        /// Dictionary that tracks previous random choice and helps to make sure they don't repeat
        /// </summary>
        private readonly Dictionary<DataOvlReference.DataChunkName, int> _previousRandomSelectionByChunk =
            new Dictionary<DataOvlReference.DataChunkName, int>();

        private const int PISSED_OFF_START = 0;
        private const int PISSED_OFF_STOP = 3;
        private const int HAPPY_START = 4;
        private const int HAPPY_STOP = 7;

        protected string FlattenStr(string str)
        {
            return str.Trim().Replace("\n", " ");
        }

        /// <summary>
        /// Quotes the string and adds "says shoppekeeper"
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        protected string AddSaysShoppeKeeper(string str, string saysStr = "says")
        {
            str = FlattenStr(str);
            if (!str.StartsWith("\"")) str = "\"" + str;
            if (!str.EndsWith("\"")) str += "\"";
            str += " " + saysStr+ " " + TheShoppeKeeperReference.ShoppeKeeperName;
            return str;
        }
        
        public bool IsOnDuty(TimeOfDay tod)
        {
            // shoppe keepers are open during their 1 and 3 index into their schedule (0 based)
            int nScheduleIndex = TheShoppeKeeperReference.NpcRef.Schedule.GetScheduleIndex(tod);
            return nScheduleIndex == 1 || nScheduleIndex == 3;
        }
        
        /// <summary>
        /// Gets a standard hello response based on current time of day 
        /// </summary>
        /// <param name="tod"></param>
        /// <returns></returns>
        public virtual string GetHelloResponse(TimeOfDay tod = null)
        {
            if (tod == null) throw new Ultima5ReduxException("can't pass null TOD to blacksmith");
            // todo: convert to data.ovl @ 0x8028
            string response = @"Good " + tod.TimeOfDayName + ", and welcome to " +
                              TheShoppeKeeperReference.ShoppeName + "!"; 
            return response;
        }

        public string GetComeLaterResponse()
        {
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ChitChatStrings
                .MERCH_SEE_ME_AT_SHOP1) + DataOvlReference.StringReferences.GetString(DataOvlReference.ChitChatStrings
                .MERCH_SEE_ME_AT_SHOP2);
        }
        
        /// <summary>
        /// Get a random response when the shoppekeeper gets pissed off at you
        /// </summary>
        /// <returns></returns>
        public virtual string GetPissedOffShoppeKeeperGoodbyeResponse()
        {
            return "\"" + ShoppeKeeperDialogueReference.GetRandomMerchantStringFromRange(PISSED_OFF_START, PISSED_OFF_STOP)
                + " says " + TheShoppeKeeperReference.ShoppeKeeperName;
        }

        /// <summary>
        /// Get a random response when the shoppekeeper is happy as you leave
        /// </summary>
        /// <returns></returns>
        public virtual string GetHappyShoppeKeeperGoodbyeResponse()
        {
            return "\"" + ShoppeKeeperDialogueReference.GetRandomMerchantStringFromRange(HAPPY_START, HAPPY_STOP)
                + " says " + TheShoppeKeeperReference.ShoppeKeeperName;
        }

        public virtual string GetThyInterest()
        {
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneralStrings
                .N_THY_INTEREST_Q_QUOTE);
        }
        
        /// <summary>
        /// Gets a common response after a purchase
        /// </summary>
        /// <returns></returns>
        public virtual string GetThanksAfterPurchaseResponse()
        {
            return "Thank thee kindly!";
        }

        /// <summary>
        /// Gets a common response after deciding not to buy
        /// </summary>
        /// <returns></returns>
        public virtual string GetPissedOffNotBuyingResponse()
        {
            return "Stop wasting my time!";
        }

        /// <summary>
        /// Gets a pissed off response when you don't have enough to buy the thing you tried to buy
        /// </summary>
        /// <returns></returns>
        public virtual string GetPissedOffNotEnoughMoney()
        {
            return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_NOT_ENOUGH_MONEY);
        }

        /// <summary>
        /// Gets a common response asking if you want to buy the thing
        /// </summary>
        /// <returns></returns>
        public string GetDoYouWantToBuy()
        {
            return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_DO_YOU_WANT);
        }

        /// <summary>
        /// Gets a random string from a datachunk string list
        /// It automatically prevents the same string from being selected twice in a row
        /// </summary>
        /// <param name="chunkName"></param>
        /// <returns></returns>
        protected string GetRandomStringFromChoices(DataOvlReference.DataChunkName chunkName)
        {
            List<string> responses = DataOvlReference.GetDataChunk(chunkName)
                .GetChunkAsStringList().Strs;

            // if this hasn't been access before, then lets add a chunk to make sure we don't repeat the same thing 
            // twice in a row
            if (!_previousRandomSelectionByChunk.ContainsKey(chunkName))
            {
                _previousRandomSelectionByChunk.Add(chunkName, -1);
            }

            int nResponseIndex = ShoppeKeeperDialogueReference.GetRandomIndexFromRange(0, responses.Count);
            
            // if this response is the same as the last response, then we add one and make sure it is still in bounds 
            // by modding it 
            if (nResponseIndex == _previousRandomSelectionByChunk[chunkName])
                nResponseIndex = (nResponseIndex + 1) % responses.Count;

            _previousRandomSelectionByChunk[chunkName] = nResponseIndex;
            
            return responses[nResponseIndex];
        }

        //public abstract string GetHelloResponse(TimeOfDay tod = null); 
            //string shoppeKeeperName = "", string shoppeName = "");
        public abstract string GetWhichWouldYouSee();
        public abstract string GetForSaleList();

        /// <summary>
        /// Get all locations that reagents are sold
        /// </summary>
        /// <param name="dataOvlReference"></param>
        /// <param name="chunkName"></param>
        /// <returns></returns>
        public static List<SmallMapReferences.SingleMapReference.Location> GetLocations(DataOvlReference dataOvlReference, DataOvlReference.DataChunkName chunkName)
        {
            List<SmallMapReferences.SingleMapReference.Location> locations =
                new List<SmallMapReferences.SingleMapReference.Location>();

            foreach (SmallMapReferences.SingleMapReference.Location location in 
                dataOvlReference.GetDataChunk(chunkName).GetAsByteList())
            {
                locations.Add(location);
            }

            return locations;
        }
        
        protected string GetNumberInPartyAsStringWord(int nNum)
        {
            if (nNum > 6) return "many";
            if (nNum == 1) return "one";
            Debug.Assert(nNum > 0);
            int nStartIndex = (int)DataOvlReference.ShoppeKeeperBarKeepStrings2.TWO - 2;
            return DataOvlReference.StringReferences.GetString((DataOvlReference.ShoppeKeeperBarKeepStrings2) nStartIndex + nNum);
        }

        protected string GetGenderedFormalPronoun(PlayerCharacterRecord.CharacterGender gender)
        {
            if (gender == PlayerCharacterRecord.CharacterGender.Male)
                return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperBarKeepStrings2.SIR);
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperBarKeepStrings2.MILADY);
        }


    }
}