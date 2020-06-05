using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapCharacters
{
    public class ShoppeKeeperOption
    {
        public enum DialogueType { None, OkGoodbye, BuyBlacksmith, SellBlacksmith }

        
        public string ButtonName { get; }
        public DialogueType DialogueOption { get; }

        public ShoppeKeeperOption(string buttonName, DialogueType dialogueOption)
        {
            ButtonName = buttonName;
            DialogueOption = dialogueOption;
        }
    }
    
    public abstract class ShoppeKeeper
    {
        protected ShoppeKeeper(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference shoppeKeeperReference)
        {
            _shoppeKeeperDialogueReference = shoppeKeeperDialogueReference;
            ShoppeKeeperReference = shoppeKeeperReference;
        }
        
        protected readonly ShoppeKeeperDialogueReference _shoppeKeeperDialogueReference;
        public readonly ShoppeKeeperReference ShoppeKeeperReference;

        private const int PISSED_OFF_START = 0;
        private const int PISSED_OFF_STOP = 3;
        private const int HAPPY_START = 4;
        private const int HAPPY_STOP = 7;

        public abstract List<ShoppeKeeperOption> ShoppeKeeperOptions { get; }
        
        private string GetTimeOfDayName(TimeOfDay tod)
        {
            if (tod.Hour > 5 && tod.Hour < 12) return "morning";
            if (tod.Hour >= 12 && tod.Hour < 6) return "afternoon";
            return "evening";
        }
        
        public string GetHelloResponse(TimeOfDay tod)
        {
            //Maps.ShoppeKeeperReference shoppeKeeper = _shoppeKeeperReferences.GetShoppeKeeperReference(location, npcType);
            
            string response = @"Good "+GetTimeOfDayName(tod)+", and welcome to " +ShoppeKeeperReference.ShoppeName + "!\n\n" + 
                              ShoppeKeeperReference.ShoppeKeeperName + " says, \"Greetings traveller! Wish ye to Buy, or hast thou wares to Sell?\"";
            return response;
        }
        
        /// <summary>
        /// Get a random response when the shoppekeeper gets pissed off at you
        /// </summary>
        /// <returns></returns>
        public string GetPissedOffShoppeKeeperGoodbyeResponse()
        {
            return _shoppeKeeperDialogueReference.GetRandomMerchantStringFromRange(PISSED_OFF_START, PISSED_OFF_STOP);
        }

        /// <summary>
        /// Get a random response when the shoppekeeper is happy as you leave
        /// </summary>
        /// <returns></returns>
        public string GetHappyShoppeKeeperGoodbyeResponse()
        {
            return _shoppeKeeperDialogueReference.GetRandomMerchantStringFromRange(HAPPY_START, HAPPY_STOP);
        }

        public string GetThanksAfterPurchaseResponse()
        {
            return "Thank thee kindly!";
        }

        public string GetPissedOffNotBuyingResponse()
        {
            return "Stop wasting my time!";
        }        
    }

    public class BlackSmith : ShoppeKeeper
    {
        private readonly DataOvlReference _dataOvlReference;

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new List<ShoppeKeeperOption>()
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyBlacksmith),
            new ShoppeKeeperOption("Sell", ShoppeKeeperOption.DialogueType.SellBlacksmith)
        };

        private readonly Dictionary<int, int> _equipmentMapToMerchantStrings = new Dictionary<int, int>();
        
        public BlackSmith(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, Inventory inventory,
            ShoppeKeeperReference shoppeKeeperReferences, DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, shoppeKeeperReferences)
        {
            _dataOvlReference = dataOvlReference;
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
    }
}