using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;
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
        protected ShoppeKeeper(ShoppeKeeperDialogueReference shoppeKeeperReference, SmallMapReferences.SingleMapReference.Location location,
            NonPlayerCharacterReference.NPCDialogTypeEnum npcType)
        {
            _shoppeKeeperDialogueReference = shoppeKeeperReference;
            _location = location;
            _npcType = npcType;
        }
        
        protected readonly ShoppeKeeperDialogueReference _shoppeKeeperDialogueReference;
        protected readonly SmallMapReferences.SingleMapReference.Location _location;
        protected readonly NonPlayerCharacterReference.NPCDialogTypeEnum _npcType;

        public abstract List<ShoppeKeeperOption> ShoppeKeeperOptions { get; }
    }

    public class BlackSmith : ShoppeKeeper
    {
        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new List<ShoppeKeeperOption>()
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyBlacksmith),
            new ShoppeKeeperOption("Sell", ShoppeKeeperOption.DialogueType.SellBlacksmith)
        };

        private readonly Dictionary<int, int> _equipmentMapToMerchantStrings = new Dictionary<int, int>();
        
        public BlackSmith(ShoppeKeeperDialogueReference shoppeKeeperReference, Inventory inventory,
            SmallMapReferences.SingleMapReference.Location location, 
            NonPlayerCharacterReference.NPCDialogTypeEnum npcType) : base(shoppeKeeperReference, location, npcType)
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