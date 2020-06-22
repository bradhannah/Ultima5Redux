using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.PlayerCharacters;
// ReSharper disable UnusedMember.Global

namespace Ultima5Redux.MapCharacters
{
    public class BlackSmith : ShoppeKeeper
    {
        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new List<ShoppeKeeperOption>()
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyBlacksmith),
            new ShoppeKeeperOption("Sell", ShoppeKeeperOption.DialogueType.SellBlacksmith)
        };

        private readonly Inventory _inventory;
        /// <summary>
        /// maps the equipment (index) to the merchant string they use when buying it from them
        /// </summary>
        private readonly Dictionary<int, int> _equipmentMapToMerchantStrings = new Dictionary<int, int>();
        
        /// <summary>
        /// Blacksmith constructor
        /// </summary>
        /// <param name="shoppeKeeperDialogueReference"></param>
        /// <param name="inventory"></param>
        /// <param name="theShoppeKeeperReferences"></param>
        /// <param name="dataOvlReference"></param>
        public BlackSmith(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, Inventory inventory,
            ShoppeKeeperReference theShoppeKeeperReferences, DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, theShoppeKeeperReferences, dataOvlReference)
        {
            _inventory = inventory;
            // go through each of the pieces of equipment in order to build a map of equipment index
            // -> merchant string list
            int nEquipmentCounter = 0;
            foreach (DataOvlReference.Equipment equipment in Enum.GetValues((typeof(DataOvlReference.Equipment))))
            {
                // we only look at equipment up to SpikedCollars
                if ((int) equipment > (int) DataOvlReference.Equipment.SpikedCollar) continue;
                
                const int nEquipmentOffset = 8;

                CombatItem item = inventory.GetItemFromEquipment(equipment);
                if (item.BasePrice <= 0) continue;
                // add an equipment offset because equipment strings don't start at zero in the merchant strings
                _equipmentMapToMerchantStrings.Add((int) equipment, nEquipmentCounter + nEquipmentOffset);
                nEquipmentCounter++;
            }
        }

        /// <summary>
        /// Get blacksmiths typical hello response
        /// </summary>
        /// <param name="tod"></param>
        /// <param name="shoppeKeeperName"></param>
        /// <param name="shoppeName"></param>
        /// <returns></returns>
        public override string GetHelloResponse(TimeOfDay tod = null, string shoppeKeeperName = "", string shoppeName = "")
        {
            string helloStr = base.GetHelloResponse(tod) + "\n\n" + TheShoppeKeeperReference.ShoppeKeeperName + " says \"" +
                              GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_HELLO);           
            return helloStr;        }

        /// <summary>
        /// Gets the listing of all equipment the blacksmith sells
        /// </summary>
        /// <returns></returns>
        public string GetEquipmentForSaleList()
        {
            StringBuilder sb = new StringBuilder();
            char itemChar = 'a';
            foreach (DataOvlReference.Equipment equipment in TheShoppeKeeperReference.EquipmentForSaleList)
            {
                sb.Append((itemChar++) + "..." +  _inventory.GetItemFromEquipment(equipment).LongName + "\n");
            }

            return sb.ToString().Trim();
        }
        
        /// <summary>
        /// Gets the response of blacksmith offering to show you what they have
        /// </summary>
        /// <returns></returns>
        public string GetYouCanBuy()
        {
            return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_POS_EXCLAIM).Trim() + " "
                + GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_WE_HAVE);
        }
        
        
        /// <summary>
        /// Gets request of which item you would like to sell to the blacksmith
        /// </summary>
        /// <returns></returns>
        public string GetWhichItemToSell()
        {
            return "Which item wouldst thou like to sell?";
        }
        
        /// <summary>
        /// Blacksmith asks what you would like to see (to buy) 
        /// </summary>
        /// <returns></returns>
        public override string GetWhichWouldYouSee()
        {
            return "Which would ye see?";
        }

        /// <summary>
        /// Gets merchant response to asking to buy a piece of equipment
        /// </summary>
        /// <param name="nEquipmentIndex">index into dialogue array</param>
        /// <param name="nGold">how much gold will it cost?</param>
        /// <returns>the complete response string</returns>
        private string GetEquipmentBuyingOutput(int nEquipmentIndex, int nGold)
        {
            int nDialogueIndex = _equipmentMapToMerchantStrings[nEquipmentIndex];
            Debug.Assert(nEquipmentIndex >= 0 && nEquipmentIndex <= (int)DataOvlReference.Equipment.SpikedCollar);
            Debug.Assert(ShoppeKeeperDialogueReference.CountReplacementVariables(nDialogueIndex) == 1);
            return ShoppeKeeperDialogueReference.GetMerchantString(nDialogueIndex, nGold:nGold);
        }

        /// <summary>
        /// Gets the blacksmiths specific response to trying to buy a specific piece of equipment
        /// </summary>
        /// <param name="equipment">which equipment?</param>
        /// <param name="nGold">how much does he charge?</param>
        /// <returns></returns>
        public string GetEquipmentBuyingOutput(DataOvlReference.Equipment equipment, int nGold)
        {
            return GetEquipmentBuyingOutput((int) equipment, nGold);
        }

        /// <summary>
        /// Gets the response string when vendor is trying to sell a particular piece of equipment
        /// </summary>
        /// <param name="nGold">how much to charge?</param>
        /// <param name="equipmentName">the name of the equipment</param>
        /// <returns>the complete response string</returns>
        public string GetEquipmentSellingOutput(int nGold, string equipmentName)
        {
            int sellStringIndex = ShoppeKeeperDialogueReference.GetRandomIndexFromRange(49, 56);
            
            return ShoppeKeeperDialogueReference.GetMerchantString(sellStringIndex, nGold: nGold, equipmentName: equipmentName);
        }

        /// <summary>
        /// Blacksmith statement when you ask to sell
        /// </summary>
        /// <returns></returns>
        public string GetOfferToSellLeadupOutput()
        {
            return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_WHATS_FOR_SALE);
        }

        /// <summary>
        /// The blacksmiths nasty response to attempting to sell ammo 
        /// </summary>
        /// <returns></returns>
        public string DontDealInAmmoOutput()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(
                DataOvlReference.ShoppeKeeperSellingStrings
                    .DONT_DEAL_AMMO_GROWL_NAME), shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName);
        }
        
        /// <summary>
        /// Blacksmiths response after buying an item
        /// </summary>
        /// <returns></returns>
        public string GetDoneResponse()
        {
            string doneResponse = ShoppeKeeperDialogueReference.GetMerchantString(
                DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperSellingStrings
                    .YES_DONE_SAYS_NAME), shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName);
            doneResponse = doneResponse.Replace("Yes", "").TrimStart();
            return doneResponse.Replace("!\"\n", "\"! ");
        }
    }
}