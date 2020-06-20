using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.Dialogue;
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
    }
}