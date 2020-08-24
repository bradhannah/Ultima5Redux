using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.SeaFaringVessel;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class Shipwright : ShoppeKeeper
    {
        public Shipwright(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
        }

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new List<ShoppeKeeperOption>() 
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyShipwright)
        };
        
        public override string GetHelloResponse(TimeOfDay tod = null, ShoppeKeeperReference shoppeKeeperReference = null)
        {
            if (shoppeKeeperReference != null)
                return ShoppeKeeperDialogueReference.GetMerchantString(ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(105,108),
                    shoppeName: shoppeKeeperReference.ShoppeName,
                    shoppeKeeperName: shoppeKeeperReference.ShoppeKeeperName,
                    tod: tod);
            throw new Ultima5ReduxException("Did not include shoppeKeeperReference in Hello response");
        }

        public override string GetWhichWouldYouSee()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(119);
        }

        public override string GetForSaleList()
        {
            throw new System.NotImplementedException();
        }

        public string GetFrigateOffer(SmallMapReferences.SingleMapReference.Location location, PlayerCharacterRecords records)
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(117, nGold: Frigate.GetPrice(location, records));
        }

        public string GetSkiffOffer(SmallMapReferences.SingleMapReference.Location location, PlayerCharacterRecords records)
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(118, nGold: Skiff.GetPrice(location, records));
        }
    }
}