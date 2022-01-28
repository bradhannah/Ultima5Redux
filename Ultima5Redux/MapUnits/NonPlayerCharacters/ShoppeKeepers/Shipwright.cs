using System;
using System.Collections.Generic;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class Shipwright : ShoppeKeeper
    {
        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new()
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyShipwright)
        };

        public Shipwright(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference,
            ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) : base(
            shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
        }

        public override string GetForSaleList()
        {
            throw new NotImplementedException();
        }

        public override string GetHappyShoppeKeeperGoodbyeResponse()
        {
            return AddSaysShoppeKeeper(ShoppeKeeperDialogueReference.GetMerchantString(
                ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(113, 116),
                shoppeName: TheShoppeKeeperReference.ShoppeName,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName));
        }

        public override string GetHelloResponse(TimeOfDay tod = null)
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(
                ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(105, 108),
                shoppeName: TheShoppeKeeperReference.ShoppeName,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName, tod: tod).TrimStart();
        }

        public override string GetPissedOffNotEnoughMoney()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(122).Trim();
        }

        public override string GetThanksAfterPurchaseResponse()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(123).TrimStart() + "\n\n" +
                   ShoppeKeeperDialogueReference.GetMerchantString(124).Trim() + " Avatar?\"";
        }

        public override string GetWhichWouldYouSee()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(119).Trim();
        }

        /// <summary>
        ///     The dock is occupied, so it's gonna be 87000 gold for that Frigate
        /// </summary>
        /// <returns></returns>
        public string GetDockIsOccupiedExpensiveShipResponse()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(125).Trim();
        }

        public string GetFrigateOffer(SmallMapReferences.SingleMapReference.Location location,
            PlayerCharacterRecords records)
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(117, Frigate.GetPrice(location, records));
        }

        public string GetSkiffOffer(SmallMapReferences.SingleMapReference.Location location,
            PlayerCharacterRecords records)
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(118, Skiff.GetPrice(location, records));
        }

        public string GetSkiffPlacedInFrigate()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(120).Trim();
        }
    }
}