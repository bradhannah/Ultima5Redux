using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapCharacters
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
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyBlacksmith),
        };

//        public override string GetHelloResponse(TimeOfDay tod = null, string shoppeKeeperName = "", string shoppeName = "")
        public override string GetHelloResponse(TimeOfDay tod = null, ShoppeKeeperReference shoppeKeeperReference = null)
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(127, 130);
            if (shoppeKeeperReference != null)
                return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                    shoppeKeeperName: shoppeKeeperReference.ShoppeKeeperName,
                    shoppeName: shoppeKeeperReference.ShoppeName);
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
            return "";
        }

        public List<Reagent> GetReagentsForSale()//SmallMapReferences.SingleMapReference.Location location)
        {
            return _inventory.SpellReagents.GetReagentsForSale(this.TheShoppeKeeperReference
                .ShoppeKeeperLocation); //location);
        }
    }
}