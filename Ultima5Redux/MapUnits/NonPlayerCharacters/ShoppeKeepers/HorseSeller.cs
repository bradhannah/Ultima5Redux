﻿using System;
using System.Collections.Generic;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class HorseSeller : ShoppeKeeper
    {
        private readonly PlayerCharacterRecords _playerCharacterRecords;

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new()
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyHorses)
        };

        public HorseSeller(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference,
            ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference,
            PlayerCharacterRecords playerCharacterRecords) : base(shoppeKeeperDialogueReference,
            theShoppeKeeperReference, dataOvlReference) =>
            _playerCharacterRecords = playerCharacterRecords;

        public override string GetForSaleList() => throw new NotImplementedException();

        public override string GetHelloResponse(TimeOfDay tod = null)
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(92, 95);
            return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName,
                shoppeName: TheShoppeKeeperReference.ShoppeName, tod: tod);
        }

        public override string GetPissedOffNotBuyingResponse()
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(96, 99);
            return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex) + " says " +
                   TheShoppeKeeperReference.ShoppeKeeperName + ".";
        }

        public override string GetPissedOffNotEnoughMoney() =>
            (DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneralStrings
                .N_N_QUOTE_THOU_COULDST_NOT_AFFORD_TO) + ShoppeKeeperDialogueReference.GetMerchantString(
                DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneralStrings
                    .FEED_IT_BANG_QUOTE_N_YELLS_SK_N), shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName))
            .Trim();

        public override string GetThanksAfterPurchaseResponse()
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(100, 103);
            return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                       shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName) + " says " +
                   TheShoppeKeeperReference.ShoppeKeeperName + ".";
        }

        public override string GetThyInterest() =>
            ShoppeKeeperDialogueReference.GetMerchantString(103,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName).Trim();

        public override string GetWhichWouldYouSee() =>
            ShoppeKeeperDialogueReference.GetMerchantString(104, GetHorsePrice()).Trim();

        public int GetHorsePrice() =>
            Horse.GetPrice(TheShoppeKeeperReference.ShoppeKeeperLocation, _playerCharacterRecords);

        public string GetStablesAreClosed() =>
            DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneralStrings
                .THE_STABLES_ARE_CLOSED_DOT_N).Trim();
    }
}