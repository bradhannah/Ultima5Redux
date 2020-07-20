using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;

namespace Ultima5Redux.MapCharacters
{
    public class Healer : ShoppeKeeper
    {
        public Healer(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
        }

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new List<ShoppeKeeperOption>() 
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyHealer),
        };
        
        public override string GetHelloResponse(TimeOfDay tod = null, ShoppeKeeperReference shoppeKeeperReference = null)
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(165, 168);
            if (shoppeKeeperReference != null)
                return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                    shoppeKeeperName: shoppeKeeperReference.ShoppeKeeperName,
                    shoppeName: shoppeKeeperReference.ShoppeName,
                    tod: tod);
            throw new Ultima5ReduxException("Can't get a hello response without a ShoppeKeeperReference");        }

        public override string GetWhichWouldYouSee()
        {
            
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                    .DQ_WE_HAVE_POWERS_TO_CURE_HEAL_RESURRECT_DOT_DQ_N)
                + ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                    .SAYS_NAME_DOT_N_N_DQ_WHAT_IS_THE_NATURE_OF_THY_NEED_Q_DQ), shoppeKeeperName: this.TheShoppeKeeperReference.ShoppeKeeperName); 
        }

        public override string GetForSaleList()
        {
            throw new System.NotImplementedException();
        }
    }
}