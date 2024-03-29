﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.References;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class GuildMaster : ShoppeKeeper
    {
        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new()
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyGuildMaster)
        };

        public GuildMaster(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference,
            ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) : base(
            shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
        }

        public override string GetForSaleList() =>
            DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneral2Strings
                .A_DOTS_KEYS_N) +
            DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneral2Strings
                .B_DOTS_GEMS_N) +
            DataOvlReference.StringReferences
                .GetString(DataOvlReference.ShoppeKeeperGeneral2Strings.C_DOTS_TORCHES_N_N)
                .Trim();

        public override string GetHelloResponse(TimeOfDay tod = null)
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(148, 151);
            return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName,
                shoppeName: TheShoppeKeeperReference.ShoppeName, tod: tod);
        }

        public override string GetThyInterest() =>
            DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneral2Strings
                .THY_CONCERN_Q_DQ);

        public override string GetWhichWouldYouSee() =>
            DataOvlReference.StringReferences
                .GetString(DataOvlReference.ShoppeKeeperGeneral2Strings.YES_N_N_DQ__WE_SELL_COLON_N_N)
                .Replace("Yes", "").TrimStart();

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public string GetProvisionBuyOutput(ProvisionReferences.SpecificProvisionType specificProvision, int nGold)
        {
            return specificProvision switch
            {
                ProvisionReferences.SpecificProvisionType.Torches => ShoppeKeeperDialogueReference.GetMerchantString(
                    162, nGold),
                ProvisionReferences.SpecificProvisionType.Gems => ShoppeKeeperDialogueReference.GetMerchantString(161,
                    nGold),
                ProvisionReferences.SpecificProvisionType.Keys => ShoppeKeeperDialogueReference.GetMerchantString(160,
                    nGold),
                ProvisionReferences.SpecificProvisionType.SkullKeys => throw new ArgumentOutOfRangeException(
                    nameof(specificProvision), specificProvision, null),
                ProvisionReferences.SpecificProvisionType.Food => throw new ArgumentOutOfRangeException(
                    nameof(specificProvision), specificProvision, null),
                ProvisionReferences.SpecificProvisionType.Gold => throw new ArgumentOutOfRangeException(
                    nameof(specificProvision), specificProvision, null),
                _ => throw new ArgumentOutOfRangeException(nameof(specificProvision), specificProvision, null)
            };
        }
    }
}