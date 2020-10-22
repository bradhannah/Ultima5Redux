using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class Innkeeper : ShoppeKeeper
    {

        private readonly InnKeeperServiceReference _innKeeperServiceReference;

        public InnKeeperServiceReference.InnKeeperServices InnKeeperServices => _innKeeperServiceReference
            .GetInnKeeperServicesByLocation(TheShoppeKeeperReference.ShoppeKeeperLocation);
      
        public Innkeeper(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference theShoppeKeeperReference, 
            DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
            _innKeeperServiceReference = new InnKeeperServiceReference(dataOvlReference);
        }

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions { get; }
        public override string GetHelloResponse(TimeOfDay tod = null)
        {
            //174-177
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(174, 177);
            return ShoppeKeeperDialogueReference.GetMerchantString(nIndex,  
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName, 
                shoppeName:TheShoppeKeeperReference.ShoppeName,
                tod: tod);
        }

        public override string GetWhichWouldYouSee()
        {
            return (ShoppeKeeperDialogueReference.GetMerchantString(
                    DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperInnkeeper2Strings
                        .N_N_NAME_ASKS_N_ART_THOU_HERE_TO_PICKUP_OR_N),
                    shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName) +
                DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperInnkeeper2Strings
                    .LEAVE_A_COMPANION_OR_TO_REST_FOR_NIGHT_Q_DQ)).Trim().Replace("\n", " ");
            //N_N_NAME_ASKS_N_ART_THOU_HERE_TO_PICKUP_OR_N
        }

        public override string GetForSaleList()
        {
            throw new System.NotImplementedException();
        }

        public override string GetPissedOffShoppeKeeperGoodbyeResponse()
        {
            // 178 - 181
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(178, 181);
            return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,  
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName, 
                shoppeName:TheShoppeKeeperReference.ShoppeName) + " says " +
                   TheShoppeKeeperReference.ShoppeKeeperName;
        }

        public string GetOfferForRest(PlayerCharacterRecords records)
        {
            return (ShoppeKeeperDialogueReference.GetMerchantString(InnKeeperServices.DialogueOfferIndex, nGold: GetCostOfRest(records)));
        }

        public int GetCostOfRest(PlayerCharacterRecords records)
        {
            return InnKeeperServices.RestCost * records.TotalPartyMembers();
        }
        
        public override string GetPissedOffNotEnoughMoney()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(
                           DataOvlReference.ShoppeKeeperInnkeeperStrings.HIGHWAYMAN_BANG_CHEAP_OUT_BANG), 
                    shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName,
                    shoppeName:TheShoppeKeeperReference.ShoppeName).Trim() + " says " + TheShoppeKeeperReference.ShoppeKeeperName;
        }
    }
}