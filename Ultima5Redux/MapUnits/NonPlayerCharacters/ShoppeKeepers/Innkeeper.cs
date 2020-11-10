using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class Innkeeper : ShoppeKeeper
    {
        private readonly InnKeeperServiceReference _innKeeperServiceReference;

        public Innkeeper(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference,
            ShoppeKeeperReference theShoppeKeeperReference,
            DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, theShoppeKeeperReference,
            dataOvlReference)
        {
            _innKeeperServiceReference = new InnKeeperServiceReference(dataOvlReference);
        }

        public InnKeeperServiceReference.InnKeeperServices InnKeeperServices => _innKeeperServiceReference
            .GetInnKeeperServicesByLocation(TheShoppeKeeperReference.ShoppeKeeperLocation);

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions { get; }

        public override string GetHelloResponse(TimeOfDay tod = null)
        {
            //174-177
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(174, 177);
            return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName,
                shoppeName: TheShoppeKeeperReference.ShoppeName,
                tod: tod);
        }

        public override string GetWhichWouldYouSee()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(
                       DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperInnkeeper2Strings
                           .N_N_NAME_ASKS_N_ART_THOU_HERE_TO_PICKUP_OR_N).TrimStart(),
                       shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName) +
                   FlattenStr(DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperInnkeeper2Strings
                       .LEAVE_A_COMPANION_OR_TO_REST_FOR_NIGHT_Q_DQ));
            //N_N_NAME_ASKS_N_ART_THOU_HERE_TO_PICKUP_OR_N
        }

        public override string GetForSaleList()
        {
            throw new NotImplementedException();
        }

        public override string GetPissedOffShoppeKeeperGoodbyeResponse()
        {
            // 178 - 181
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(178, 181);
            return AddSaysShoppeKeeper(ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName,
                shoppeName: TheShoppeKeeperReference.ShoppeName));
        }

        public string GetOfferForRest(PlayerCharacterRecords records)
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(InnKeeperServices.DialogueOfferIndex,
                GetCostOfRest(records));
        }

        public int GetCostOfRest(PlayerCharacterRecords records)
        {
            return InnKeeperServices.RestCost * records.TotalPartyMembers();
        }

        public override string GetPissedOffNotEnoughMoney()
        {
            return AddSaysShoppeKeeper(ShoppeKeeperDialogueReference.GetMerchantString(
                DataOvlReference.StringReferences.GetString(
                    DataOvlReference.ShoppeKeeperInnkeeperStrings.HIGHWAYMAN_BANG_CHEAP_OUT_BANG),
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName,
                shoppeName: TheShoppeKeeperReference.ShoppeName));
        }

        public string GetNoRoomAtTheInn(PlayerCharacterRecords records)
        {
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperInnkeeperStrings
                       .I_AM_SORRY_COMMA_N).Trim() + " " + GetGenderedFormalPronoun(records.AvatarRecord.Gender) +
                   FlattenStr(DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperInnkeeperStrings
                       .COMMA_BUT_WE_HAVE_NO_ROOM_N_N));
        }

        public string GetNoOneAtTheInnForPickup()
        {
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperInnkeeper2Strings
                .N_N_ONE_MUST_FIRST_BE_LEFT_BEHIND_BANG_N_N).Trim();
        }

        public string GetWhoWillYouCheckout()
        {
            return FlattenStr(DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperInnkeeper2Strings
                .N_N_DQ_WHO_WILL_N_CHECK_OUT_Q_DQ));
        }

        public int GetCostOfInnStay(PlayerCharacterRecord record)
        {
            return GetBaseCostOfInnStay() * Math.Max(1, (int) record.MonthsSinceStayingAtInn);
        }

        public int GetBaseCostOfInnStay()
        {
            return _innKeeperServiceReference
                .GetInnKeeperServicesByLocation(TheShoppeKeeperReference.ShoppeKeeperLocation).MonthlyLeaveCost;
        }

        public string GetDoNotPossessNecessaryFundsForPickup()
        {
            return "\"" + FlattenStr(ShoppeKeeperDialogueReference.GetMerchantString(193,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName));
        }

        public string GetHopeStayEnjoyable()
        {
            return AddSaysShoppeKeeper(DataOvlReference.StringReferences.GetString(DataOvlReference
                       .ShoppeKeeperInnkeeper2Strings
                       .I_HOPE_THOU_HAST_THY_STAY_ENJOYABLE_COMMA_N)) + "\n\n" +
                   FlattenStr(DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperInnkeeper2Strings
                       .IS_THERE_N_ANYTHING_MORE_N_I_CAN_DO_FOR_N_THEE_Q_DQ));
        }

        public string GetWhoWillStay()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(
                    DataOvlReference.ShoppeKeeperInnkeeper2Strings.ASKS_N_WHO_WILL_STAY_Q),
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName);
        }

        public string GetLeaveWithInnConfirmation()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(
                       DataOvlReference.ShoppeKeeperInnkeeper2Strings
                           .DQ_THE_RATE_FOR_OUR_MOST_COMFORTABLE_ROOM_WILL_BE_))
                   +
                   ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(
                           DataOvlReference.ShoppeKeeperInnkeeper2Strings.GLD_GOLD_PER_MONTH_DUE_AT_CHECKOUT_DOT),
                       GetBaseCostOfInnStay())
                   +
                   ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(
                       DataOvlReference.ShoppeKeeperInnkeeper2Strings.N_WILT_THOU_TAKE_IT_Q_DQ));
        }

        public string GetIThankThee()
        {
            return FlattenStr(ShoppeKeeperDialogueReference.GetMerchantString(
                DataOvlReference.StringReferences.GetString(
                    DataOvlReference.ShoppeKeeperInnkeeper2Strings.I_THANK_THEE_DQ_N_SAYS_NAME_DOT_N_N),
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName));
        }

        public string GetThatWillBeGold(PlayerCharacterRecord record)
        {
            string thatWillBeStr = ShoppeKeeperDialogueReference.GetMerchantString(
                DataOvlReference.StringReferences.GetString(
                    DataOvlReference.ShoppeKeeperInnkeeper2Strings.N_N_DQ_THAT_WILL_BE_GLD_GOLD_PLEASE_DOT_DQ_N_N),
                GetCostOfInnStay(record));
            return FlattenStr(thatWillBeStr.Substring(0, thatWillBeStr.Length - 1));
        }

        public string GetThyFriendHasDied()
        {
            return AddSaysShoppeKeeper(DataOvlReference.StringReferences.GetString(
                DataOvlReference.ShoppeKeeperInnkeeper2Strings.THY_FRIEND_HAS_DIED_BY_THE_WAY_DOT_N));
        }

        public string GetNotAMorgue()
        {
            return FlattenStr(ShoppeKeeperDialogueReference.GetMerchantString(192,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName));
        }
    }
}