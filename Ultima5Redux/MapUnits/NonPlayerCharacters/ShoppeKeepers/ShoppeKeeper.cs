using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers;

// ReSharper disable UnusedMemberInSuper.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    /// <summary>
    ///     Shoppe keeper options
    ///     Each of these describe the different behaviours that the shoppe keeper is responsible for
    /// </summary>
    public class ShoppeKeeperOption
    {
        public enum DialogueType
        {
            None, OkGoodbye, BuyBlacksmith, SellBlacksmith, BuyMagicSeller, BuyShipwright, BuyBarkeeper, BuyHealer,
            BuyGuildMaster, RestInnkeeper, GossipInnkeeper, BuyHorses, DropOffPartyMemberInnkeeper
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
    ///     A generic shoppe keeper with all common elements described
    /// </summary>
    public abstract class ShoppeKeeper
    {
        private const int HAPPY_START = 4;
        private const int HAPPY_STOP = 7;
        private const int PISSED_OFF_START = 0;
        private const int PISSED_OFF_STOP = 3;

        /// <summary>
        ///     Dictionary that tracks previous random choice and helps to make sure they don't repeat
        /// </summary>
        private readonly Dictionary<DataOvlReference.DataChunkName, int> _previousRandomSelectionByChunk = new();

        protected readonly DataOvlReference DataOvlReference;

        protected readonly ShoppeKeeperDialogueReference ShoppeKeeperDialogueReference;

        /// <summary>
        ///     A list of the shoppe keeper options (abilities)
        /// </summary>
        public abstract List<ShoppeKeeperOption> ShoppeKeeperOptions { get; }

        public ShoppeKeeperReference TheShoppeKeeperReference { get; }

        /// <summary>
        ///     Construct a shoppe keeper
        /// </summary>
        /// <param name="shoppeKeeperDialogueReference"></param>
        /// <param name="theShoppeKeeperReference"></param>
        /// <param name="dataOvlReference"></param>
        protected ShoppeKeeper(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference,
            ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference)
        {
            ShoppeKeeperDialogueReference = shoppeKeeperDialogueReference;
            DataOvlReference = dataOvlReference;
            TheShoppeKeeperReference = theShoppeKeeperReference;
        }

        /// <summary>
        ///     Get all locations that reagents are sold
        /// </summary>
        /// <param name="dataOvlReference"></param>
        /// <param name="chunkName"></param>
        /// <returns></returns>
        public static IEnumerable<SmallMapReferences.SingleMapReference.Location> GetLocations(
            DataOvlReference dataOvlReference, DataOvlReference.DataChunkName chunkName)
        {
            List<SmallMapReferences.SingleMapReference.Location> locations = new();

            foreach (byte b in dataOvlReference.GetDataChunk(chunkName).GetAsByteList())
            {
                var location =
                    (SmallMapReferences.SingleMapReference.Location)b;
                locations.Add(location);
            }

            return locations;
        }

        public abstract string GetForSaleList();

        public abstract string GetWhichWouldYouSee();

        /// <summary>
        ///     Get a random response when the shoppekeeper is happy as you leave
        /// </summary>
        /// <returns></returns>
        public virtual string GetHappyShoppeKeeperGoodbyeResponse() =>
            "\"" + ShoppeKeeperDialogueReference.GetRandomMerchantStringFromRange(HAPPY_START, HAPPY_STOP) +
            " says " + TheShoppeKeeperReference.ShoppeKeeperName;

        /// <summary>
        ///     Gets a standard hello response based on current time of day
        /// </summary>
        /// <param name="tod"></param>
        /// <returns></returns>
        public virtual string GetHelloResponse(TimeOfDay tod = null)
        {
            if (tod == null) throw new Ultima5ReduxException("can't pass null TOD to blacksmith");
            string response = $@"Good {tod.TimeOfDayName}, and welcome to {TheShoppeKeeperReference.ShoppeName}!";
            return response;
        }

        /// <summary>
        ///     Gets a common response after deciding not to buy
        /// </summary>
        /// <returns></returns>
        public virtual string GetPissedOffNotBuyingResponse() => "Stop wasting my time!";

        /// <summary>
        ///     Gets a pissed off response when you don't have enough to buy the thing you tried to buy
        /// </summary>
        /// <returns></returns>
        public virtual string GetPissedOffNotEnoughMoney() =>
            GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_NOT_ENOUGH_MONEY);

        /// <summary>
        ///     Get a random response when the shoppekeeper gets pissed off at you
        /// </summary>
        /// <returns></returns>
        public virtual string GetPissedOffShoppeKeeperGoodbyeResponse() =>
            "\"" +
            ShoppeKeeperDialogueReference.GetRandomMerchantStringFromRange(PISSED_OFF_START, PISSED_OFF_STOP) +
            " says " + TheShoppeKeeperReference.ShoppeKeeperName;

        /// <summary>
        ///     Gets a common response after a purchase
        /// </summary>
        /// <returns></returns>
        public virtual string GetThanksAfterPurchaseResponse() => "Thank thee kindly!";

        public virtual string GetThyInterest() =>
            DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneralStrings
                .N_THY_INTEREST_Q_QUOTE);

        public string GetComeLaterResponse() =>
            DataOvlReference.StringReferences.GetString(DataOvlReference.ChitChatStrings.MERCH_SEE_ME_AT_SHOP1) +
            DataOvlReference.StringReferences.GetString(DataOvlReference.ChitChatStrings.MERCH_SEE_ME_AT_SHOP2);

        /// <summary>
        ///     Gets a common response asking if you want to buy the thing
        /// </summary>
        /// <returns></returns>
        public string GetDoYouWantToBuy() =>
            GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_DO_YOU_WANT);

        public bool IsOnDuty(TimeOfDay tod)
        {
            // shoppe keepers are open during their 1 and 3 index into their schedule (0 based)
            int nScheduleIndex = TheShoppeKeeperReference.NpcRef.Schedule.GetScheduleIndex(tod);
            return nScheduleIndex == 1 || nScheduleIndex == 3;
        }

        /// <summary>
        ///     Quotes the string and adds "says shoppekeeper"
        /// </summary>
        /// <param name="str"></param>
        /// <param name="saysStr"></param>
        /// <returns></returns>
        protected string AddSaysShoppeKeeper(string str, string saysStr = "says")
        {
            str = FlattenStr(str);
            if (!str.StartsWith("\"")) str = "\"" + str;
            if (!str.EndsWith("\"")) str += "\"";
            str += " " + saysStr + " " + TheShoppeKeeperReference.ShoppeKeeperName;
            return str;
        }

        protected string FlattenStr(string str) => str.Trim().Replace("\n", " ");

        protected string GetGenderedFormalPronoun(PlayerCharacterRecord.CharacterGender gender)
        {
            if (gender == PlayerCharacterRecord.CharacterGender.Male)
                return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperBarKeepStrings2.SIR);
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperBarKeepStrings2.MILADY);
        }

        protected string GetNumberInPartyAsStringWord(int nNum)
        {
            if (nNum > 6) return "many";
            if (nNum == 1) return "one";
            Debug.Assert(nNum > 0);
            const int nStartIndex = (int)DataOvlReference.ShoppeKeeperBarKeepStrings2.TWO - 2;
            return DataOvlReference.StringReferences.GetString(
                (DataOvlReference.ShoppeKeeperBarKeepStrings2)nStartIndex + nNum);
        }

        /// <summary>
        ///     Gets a random string from a datachunk string list
        ///     It automatically prevents the same string from being selected twice in a row
        /// </summary>
        /// <param name="chunkName"></param>
        /// <returns></returns>
        protected string GetRandomStringFromChoices(DataOvlReference.DataChunkName chunkName)
        {
            List<string> responses = DataOvlReference.GetDataChunk(chunkName).GetChunkAsStringList().StringList;

            // if this hasn't been access before, then lets add a chunk to make sure we don't repeat the same thing 
            // twice in a row
            if (!_previousRandomSelectionByChunk.ContainsKey(chunkName))
                _previousRandomSelectionByChunk.Add(chunkName, -1);

            int nResponseIndex = ShoppeKeeperDialogueReference.GetRandomIndexFromRange(0, responses.Count);

            // if this response is the same as the last response, then we add one and make sure it is still in bounds 
            // by modding it 
            if (nResponseIndex == _previousRandomSelectionByChunk[chunkName])
                nResponseIndex = (nResponseIndex + 1) % responses.Count;

            _previousRandomSelectionByChunk[chunkName] = nResponseIndex;

            return responses[nResponseIndex];
        }
    }
}