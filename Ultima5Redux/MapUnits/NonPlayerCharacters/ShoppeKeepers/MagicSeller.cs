using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class MagicSeller : ShoppeKeeper
    {
        private readonly Inventory _inventory;

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new()
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyMagicSeller)
        };

        public MagicSeller(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, Inventory inventory,
            ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) : base(
            shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference) =>
            _inventory = inventory;

        public override string GetForSaleList()
        {
            List<Reagent> reagents = GetReagentsForSale();

            StringBuilder sb = new();
            char itemChar = 'a';
            foreach (Reagent reagent in reagents)
            {
                sb.Append(itemChar++ + "..." + reagent.LongName + "\n");
            }

            return sb.ToString().TrimEnd();
        }

        public override string GetHelloResponse(TimeOfDay tod = null)
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(127, 130);
            return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName,
                shoppeName: TheShoppeKeeperReference.ShoppeName, tod: tod);
        }

        public override string GetPissedOffNotEnoughMoney() =>
            ShoppeKeeperDialogueReference.GetMerchantString(147,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName);

        public override string GetThanksAfterPurchaseResponse() =>
            DataOvlReference.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.N_N_I_THANK_THEE_N)
                .TrimStart();

        public override string GetWhichWouldYouSee()
        {
            string retStr = ShoppeKeeperDialogueReference.GetMerchantString(
                DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperGeneralStrings
                    .YES_N_N_FINE_BANG_WE_SELL_COLON), shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName);
            retStr = retStr.Replace("Yes", "");
            retStr = retStr.Replace("\n\n", "\n").TrimEnd();
            return retStr;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public string GetReagentBuyingOutput(Reagent reagent)
        {
            const int nStartReagentBuyStrings = 139;
            // get the index 
            int nIndex = reagent.ReagentIndex;

            int nPrice = reagent.GetAdjustedBuyPrice(null, TheShoppeKeeperReference.NpcRef.MapLocation);
            int nQuantity = reagent.GetQuantityForSale(TheShoppeKeeperReference.NpcRef.MapLocation);
            Debug.Assert(nQuantity > 0);
            return "\"" +
                   ShoppeKeeperDialogueReference.GetMerchantString(nStartReagentBuyStrings + nIndex,
                       nQuantity: nQuantity, nGold: nPrice) + "\n" + DataOvlReference.StringReferences
                       .GetString(DataOvlReference.ShoppeKeeperGeneral2Strings.IS_THIS_THY_NEED_Q_DQ).Trim();
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public List<Reagent> GetReagentsForSale() =>
            _inventory.SpellReagents.GetReagentsForSale(TheShoppeKeeperReference
                .ShoppeKeeperLocation);
    }
}