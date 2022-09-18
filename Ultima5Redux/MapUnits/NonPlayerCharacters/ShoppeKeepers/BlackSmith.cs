using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers;

// ReSharper disable UnusedMember.Global

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class BlackSmith : ShoppeKeeper
    {
        /// <summary>
        ///     maps the equipment (index) to the merchant string they use when buying it from them
        /// </summary>
        private readonly Dictionary<int, int> _equipmentMapToMerchantStrings = new();

        private readonly Inventory _inventory;

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new()
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyBlacksmith),
            new ShoppeKeeperOption("Sell", ShoppeKeeperOption.DialogueType.SellBlacksmith)
        };

        /// <summary>
        ///     Blacksmith constructor
        /// </summary>
        /// <param name="shoppeKeeperDialogueReference"></param>
        /// <param name="inventory"></param>
        /// <param name="theShoppeKeeperReferences"></param>
        /// <param name="dataOvlReference"></param>
        public BlackSmith(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, Inventory inventory,
            ShoppeKeeperReference theShoppeKeeperReferences, DataOvlReference dataOvlReference) : base(
            shoppeKeeperDialogueReference, theShoppeKeeperReferences, dataOvlReference)
        {
            _inventory = inventory;
            // go through each of the pieces of equipment in order to build a map of equipment index
            // -> merchant string list
            int nEquipmentCounter = 0;
            foreach (DataOvlReference.Equipment equipment in Enum.GetValues(typeof(DataOvlReference.Equipment)))
            {
                // we only look at equipment up to SpikedCollars
                if ((int)equipment > (int)DataOvlReference.Equipment.SpikedCollar) continue;

                const int nEquipmentOffset = 8;

                CombatItem item = inventory.GetItemFromEquipment(equipment);
                if (item.BasePrice <= 0) continue;
                // add an equipment offset because equipment strings don't start at zero in the merchant strings
                _equipmentMapToMerchantStrings.Add((int)equipment, nEquipmentCounter + nEquipmentOffset);
                nEquipmentCounter++;
            }
        }

        /// <summary>
        ///     Gets merchant response to asking to buy a piece of equipment
        /// </summary>
        /// <param name="nEquipmentIndex">index into dialogue array</param>
        /// <param name="nGold">how much gold will it cost?</param>
        /// <returns>the complete response string</returns>
        private string GetEquipmentBuyingOutput(int nEquipmentIndex, int nGold)
        {
            int nDialogueIndex = _equipmentMapToMerchantStrings[nEquipmentIndex];
            Debug.Assert(nEquipmentIndex >= 0 && nEquipmentIndex <= (int)DataOvlReference.Equipment.SpikedCollar);
            Debug.Assert(ShoppeKeeperDialogueReference.CountReplacementVariables(nDialogueIndex) == 1);
            return ShoppeKeeperDialogueReference.GetMerchantString(nDialogueIndex, nGold);
        }

        /// <summary>
        ///     Gets the listing of all equipment the blacksmith sells
        /// </summary>
        /// <returns></returns>
        public override string GetForSaleList()
        {
            StringBuilder sb = new();
            char itemChar = 'a';
            foreach (DataOvlReference.Equipment equipment in TheShoppeKeeperReference.EquipmentForSaleList)
            {
                CombatItem item = _inventory?.GetItemFromEquipment(equipment);
                if (item == null)
                    throw new Ultima5ReduxException("Couldn't get item from " + equipment);
                sb.Append(itemChar++ + "..." + item.LongName + "\n");
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        ///     Get blacksmiths typical hello response
        /// </summary>
        /// <param name="tod"></param>
        /// <returns></returns>
        public override string GetHelloResponse(TimeOfDay tod = null)
        {
            string helloStr = base.GetHelloResponse(tod) + "\n\n" + TheShoppeKeeperReference.ShoppeKeeperName +
                              " says \"" +
                              GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_HELLO);
            return helloStr;
        }

        /// <summary>
        ///     Blacksmith asks what you would like to see (to buy)
        /// </summary>
        /// <returns></returns>
        public override string GetWhichWouldYouSee() => "Which would ye see?";

        /// <summary>
        ///     The blacksmiths nasty response to attempting to sell ammo
        /// </summary>
        /// <returns></returns>
        public string DontDealInAmmoOutput() =>
            ShoppeKeeperDialogueReference.GetMerchantString(
                DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperSellingStrings
                    .DONT_DEAL_AMMO_GROWL_NAME), shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName);

        /// <summary>
        ///     Blacksmiths response after buying an item
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

        /// <summary>
        ///     Gets the blacksmiths specific response to trying to buy a specific piece of equipment
        /// </summary>
        /// <param name="equipment">which equipment?</param>
        /// <param name="nGold">how much does he charge?</param>
        /// <returns></returns>
        public string GetEquipmentBuyingOutput(DataOvlReference.Equipment equipment, int nGold) =>
            GetEquipmentBuyingOutput((int)equipment, nGold);

        /// <summary>
        ///     Gets the response string when vendor is trying to sell a particular piece of equipment
        /// </summary>
        /// <param name="nGold">how much to charge?</param>
        /// <param name="equipmentName">the name of the equipment</param>
        /// <returns>the complete response string</returns>
        public string GetEquipmentSellingOutput(int nGold, string equipmentName)
        {
            int sellStringIndex = ShoppeKeeperDialogueReference.GetRandomIndexFromRange(49, 56);

            return ShoppeKeeperDialogueReference.GetMerchantString(sellStringIndex, nGold, equipmentName);
        }

        /// <summary>
        ///     Blacksmith statement when you ask to sell
        /// </summary>
        /// <returns></returns>
        public string GetOfferToSellLeadupOutput() =>
            GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_WHATS_FOR_SALE);

        /// <summary>
        ///     Gets request of which item you would like to sell to the blacksmith
        /// </summary>
        /// <returns></returns>
        public string GetWhichItemToSell() => "Which item wouldst thou like to sell?";

        /// <summary>
        ///     Gets the response of blacksmith offering to show you what they have
        /// </summary>
        /// <returns></returns>
        public string GetYouCanBuy() =>
            GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_POS_EXCLAIM)
                .Trim() + " " +
            GetRandomStringFromChoices(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_WE_HAVE);
    }
}