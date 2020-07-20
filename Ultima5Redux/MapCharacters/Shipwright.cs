using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;

namespace Ultima5Redux.MapCharacters
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
            throw new System.NotImplementedException();
        }

        public override string GetWhichWouldYouSee()
        {
            throw new System.NotImplementedException();
        }

        public override string GetForSaleList()
        {
            throw new System.NotImplementedException();
        }
    }
}