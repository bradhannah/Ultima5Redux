using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class GuildMaster : ShoppeKeeper
    {
        public GuildMaster(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference,
            ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) :
            base(shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
        }

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new List<ShoppeKeeperOption>
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyGuildMaster)
        };

        public override string GetHelloResponse(TimeOfDay tod = null)
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(148, 151);
            return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName,
                shoppeName: TheShoppeKeeperReference.ShoppeName,
                tod: tod);
        }

        public override string GetWhichWouldYouSee()
        {
            return DataOvlReference.StringReferences.GetString(
                    DataOvlReference.ShoppeKeeperGeneral2Strings.YES_N_N_DQ__WE_SELL_COLON_N_N).Replace("Yes", "")
                .TrimStart();
        }

        public override string GetThyInterest()
        {
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneral2Strings
                .THY_CONCERN_Q_DQ);
        }

        public override string GetForSaleList()
        {
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneral2Strings
                       .A_DOTS_KEYS_N)
                   + DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneral2Strings
                       .B_DOTS_GEMS_N)
                   + DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneral2Strings
                       .C_DOTS_TORCHES_N_N).Trim();
        }

        public string GetProvisionBuyOutput(Provision.ProvisionTypeEnum provision, int nGold)
        {
            switch (provision)
            {
                case Provision.ProvisionTypeEnum.Torches:
                    return ShoppeKeeperDialogueReference.GetMerchantString(162, nGold);
                case Provision.ProvisionTypeEnum.Gems:
                    return ShoppeKeeperDialogueReference.GetMerchantString(161, nGold);
                case Provision.ProvisionTypeEnum.Keys:
                    return ShoppeKeeperDialogueReference.GetMerchantString(160, nGold);
                case Provision.ProvisionTypeEnum.SkullKeys:
                default:
                    throw new ArgumentOutOfRangeException(nameof(provision), provision, null);
            }
        }
    }
}