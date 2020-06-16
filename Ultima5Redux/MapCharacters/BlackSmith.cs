using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly Dictionary<int, int> _equipmentMapToMerchantStrings = new Dictionary<int, int>();
        
        public BlackSmith(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, Inventory inventory,
            ShoppeKeeperReference theShoppeKeeperReferences, DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, theShoppeKeeperReferences, dataOvlReference)
        {
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

        public override string GetHelloResponse(TimeOfDay tod)
        {
            string helloStr = base.GetHelloResponse(tod) + "\n\n" + TheShoppeKeeperReference.ShoppeKeeperName + " says \"" +
                              GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_HELLO);           
            return helloStr;
        }

        // public override string GetWeHaveResponse()
        // {
        //     return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_WE_HAVE).Replace(":"," ")+"many great things!";
        // }

        public string GetYouCanBuy()
        {
            return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_POS_EXCLAIM).Trim() + " "
                + GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_WE_HAVE);
        }
        
        
        public string GetWhichItemToSell()
        {
            return "Which item wouldst thou like to sell?";
        }
        
        public string GetWhichWouldYouSee()
        {
            return "Which would ye see?";
        }

        /// <summary>
        /// Gets merchant response to asking to buy a piece of equipment
        /// </summary>
        /// <param name="nEquipmentIndex">index into dialogue array</param>
        /// <param name="nGold">how much gold will it cost?</param>
        /// <param name="bUseRichText"></param>
        /// <returns>the complete response string</returns>
        private string GetEquipmentBuyingOutput(int nEquipmentIndex, int nGold, bool bUseRichText)
        {
            int nDialogueIndex = _equipmentMapToMerchantStrings[nEquipmentIndex];
            Debug.Assert(nEquipmentIndex >= 0 && nEquipmentIndex <= (int)DataOvlReference.Equipment.SpikedCollar);
            Debug.Assert(_shoppeKeeperDialogueReference.CountReplacementVariables(nDialogueIndex) == 1);
            return _shoppeKeeperDialogueReference.GetMerchantString(nDialogueIndex, nGold:nGold);
        }

        public string GetEquipmentBuyingOutput(DataOvlReference.Equipment equipment, int nGold, bool bUseRichText = true)
        {
            return GetEquipmentBuyingOutput((int) equipment, nGold, bUseRichText);
        }

        /// <summary>
        /// Gets the response string when vendor is trying to sell a particular piece of equipment
        /// </summary>
        /// <param name="nGold">how much to charge?</param>
        /// <param name="equipmentName">the name of the equipment</param>
        /// <returns>the complete response string</returns>
        public string GetEquipmentSellingOutput(int nGold, string equipmentName)
        {
            int sellStringIndex = _shoppeKeeperDialogueReference.GetRandomIndexFromRange(49, 56);
            
            return _shoppeKeeperDialogueReference.GetMerchantString(sellStringIndex, nGold: nGold, equipmentName: equipmentName);
        }

        public string GetOfferToSellLeadupOutput()
        {
            return GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_WHATS_FOR_SALE);
        }

        public string DontDealInAmmoOutput()
        {
            return _shoppeKeeperDialogueReference.GetMerchantString(_dataOvlReference.StringReferences.GetString(
                DataOvlReference.ShoppeKeeperSellingStrings
                    .DONT_DEAL_AMMO_GROWL_NAME), shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName);
        }
        
        public string GetDoneResponse()
        {
            string doneResponse = _shoppeKeeperDialogueReference.GetMerchantString(
                _dataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperSellingStrings
                    .YES_DONE_SAYS_NAME), shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName);
            return doneResponse.Replace("!\"\n", "\"! ");
        }
    }
}