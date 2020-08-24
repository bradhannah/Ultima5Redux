using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class MagicSeller : ShoppeKeeper
    {
        private readonly Inventory _inventory;

        public MagicSeller(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, Inventory inventory, ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) : 
            base(shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
            _inventory = inventory;
        }

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new List<ShoppeKeeperOption>()
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyMagicSeller),
        };

        public override string GetHelloResponse(TimeOfDay tod = null, ShoppeKeeperReference shoppeKeeperReference = null)
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(127, 130);
            if (shoppeKeeperReference != null)
                return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                    shoppeKeeperName: shoppeKeeperReference.ShoppeKeeperName,
                    shoppeName: shoppeKeeperReference.ShoppeName,
                    tod: tod);
            throw new Ultima5ReduxException("Can't get a hello response without a ShoppeKeeperReference");
        }

        public override string GetWhichWouldYouSee()
        {
            string retStr= ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(
                DataOvlReference.ShoppeKeeperGeneralStrings
                    .YES_N_N_FINE_BANG_WE_SELL_COLON), shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName);
            retStr = retStr.Replace("Yes", "");
            retStr = retStr.Replace("\n\n", "\n").TrimEnd();
            return retStr;
        }

        public override string GetForSaleList()
        {
            List<Reagent> reagents = GetReagentsForSale();
            
            StringBuilder sb = new StringBuilder();
            char itemChar = 'a';
            foreach (Reagent reagent in reagents)
            {
                sb.Append((itemChar++) + "..." +  reagent.LongName + "\n");
            }
            return sb.ToString().TrimEnd();
        }

        public List<Reagent> GetReagentsForSale()//SmallMapReferences.SingleMapReference.Location location)
        {
            return _inventory.SpellReagents.GetReagentsForSale(this.TheShoppeKeeperReference
                .ShoppeKeeperLocation); //location);
        }

        public override string GetPissedOffNotEnoughMoney()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(147, shoppeKeeperName:TheShoppeKeeperReference.ShoppeKeeperName);
        }

        public override string GetThanksAfterPurchaseResponse()
        {
            return DataOvlReference.StringReferences.GetString(DataOvlReference.OpeningThingsStrings.N_N_I_THANK_THEE_N)
                .TrimStart();
        }

        public string GetReagentBuyingOutput(Reagent reagent)
        {
            const int nStartReagentBuyStrings = 139;
            // get the index 
            int nIndex = reagent.ReagentIndex;

            int nPrice = reagent.GetAdjustedBuyPrice(null, TheShoppeKeeperReference.NpcRef.MapLocation);
            int nQuantity = reagent.GetQuantityForSale(TheShoppeKeeperReference.NpcRef.MapLocation);
            Debug.Assert(nQuantity > 0);
            return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nStartReagentBuyStrings + nIndex,
                nQuantity:nQuantity, nGold:nPrice) + "\n" +
                   DataOvlReference.StringReferences.
                       GetString(DataOvlReference.ShoppeKeeperGeneral2Strings.IS_THIS_THY_NEED_Q_DQ).Trim();
        }

    }
}