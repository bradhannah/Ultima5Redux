using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class BarKeeper : ShoppeKeeper
    {
        private readonly Dictionary<string, string> _gossipWordToPersonMap = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _gossipWordToPlaceMap = new Dictionary<string, string>();

        private readonly BarKeeperStockReference _barKeeperStockReference = new BarKeeperStockReference();

        public bool BoughtSomethingFromBarKeep { get; set; } = false;
        
        public BarKeeperStockReference.BarKeeperStock TheBarKeeperStock => 
            _barKeeperStockReference.GetBarKeeperStock(TheShoppeKeeperReference.ShoppeKeeperLocation);

        public BarKeeper(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference theShoppeKeeperReference, 
            DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
            // the words the bar keeper knows about
            List<string> gossipWords = dataOvlReference
                .GetDataChunk(DataOvlReference.DataChunkName.BAR_KEEP_GOSSIP_WORDS).GetChunkAsStringList().Strs;
            // the people they gossip about
            List<string> gossipPeople = dataOvlReference
                .GetDataChunk(DataOvlReference.DataChunkName.BAR_KEEP_GOSSIP_PEOPLE).GetChunkAsStringList().Strs;
            // the location of the people they gossip about
            List<string> gossipLocations = dataOvlReference
                .GetDataChunk(DataOvlReference.DataChunkName.BAR_KEEP_GOSSIP_PLACES).GetChunkAsStringList().Strs;
            // the map of word to the location
            List<byte> gossipListMap =
                dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.BAR_KEEP_GOSSIP_MAP).GetAsByteList();
            
            List<byte> locations = dataOvlReference
                .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_TOWNES_TAVERN).GetAsByteList();
            
            // initialize the quick look up map for gossip look ups

            // let's make sure they all line up properly first
            Debug.Assert(gossipLocations != null && gossipPeople != null && gossipWords != null && 
                         (gossipPeople.Count == gossipWords.Count));
            
            for (int nIndex = 0; nIndex < gossipWords.Count; nIndex++)
            {
                string gossipWord = gossipWords[nIndex];
                _gossipWordToPersonMap.Add(gossipWord, gossipPeople[nIndex]);
                // it requires additional translation since the original code only provides 13 possible locations
                // but 26 words and people to speak with
                int nLocationIndex = gossipListMap[nIndex];
                _gossipWordToPlaceMap.Add(gossipWord, gossipLocations[nLocationIndex]);
            }
        }

        public string GetGossipResponse(string word)
        {
            Debug.Assert(DoesBarKeeperKnowGossip(word));
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(85, 88);
            return ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                personOfInterest: _gossipWordToPersonMap[word],
                locationToFindPersonOfInterest: _gossipWordToPlaceMap[word]);
        }

        public bool DoesBarKeeperKnowGossip(string word)
        {
            return _gossipWordToPlaceMap.ContainsKey(word);
        }

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new List<ShoppeKeeperOption>() 
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyBarkeeper),
        };

        public override string GetHelloResponse(TimeOfDay tod = null, ShoppeKeeperReference shoppeKeeperReference = null)
        {
            //57-60
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(57, 60);
            return ShoppeKeeperDialogueReference.GetMerchantString(nIndex, tod: tod, 
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName, 
                shoppeName:TheShoppeKeeperReference.ShoppeName );
        }

        
        public override string GetPissedOffNotEnoughMoney()
        {
            string origStr = DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperBarKeepStrings2
                .DQ_N_N_CANT_PAY_Q_B_BEAT_IT_BANG_DQ_N_YELLS_SP);
            return origStr.Replace("\"\n\n", "").Replace("\n", " ") + this.TheShoppeKeeperReference.ShoppeKeeperName;
        }

        public string GetPissedOffNotEnoughMoneyRations()
        {
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperBarKeepStrings2
                .THOU_HAST_N_HEITHER_GOLD_NOR_N_NEED_BANG_OUT_BANG_DQ_N).Replace("\n", " ") + 
                             TheShoppeKeeperReference.ShoppeKeeperName + ".";
            
        }

        public override string GetWhichWouldYouSee()
        {
            int nIndex;
            if (BoughtSomethingFromBarKeep)
            {
                nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(73, 76);
                return ShoppeKeeperDialogueReference.GetMerchantString(nIndex);
            }
            nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(69, 72);
            return ShoppeKeeperDialogueReference.GetMerchantString(nIndex);
            //69-76
            //throw new System.NotImplementedException();

            // 69,"What'll it be... a leg of our tender roast Mutton, a tankard of Ale, or Rations for thy travels?"
            // 70,"How may I serve thee? Our fine Wines, perhaps, or possibly a bite of_Cheese?"
            // 71,"Shall I bring thee a bottle of Rum, or art thou hungry for a side of our famous wild Boar?"
            // 72,"Wouldst thou sample our finely brewed Stout, or desirest thou some fresh Fruits? We also sell the finest Provisions!"
        }

        public int GetPriceBasedOnParty(int nPricePerPartyMember, int nPartyMembers)
        {
            return nPricePerPartyMember * nPartyMembers;;
        }
        
        public string GetFoodOrDrinkOfferAndConfirmation(int nPricePerPartyMember, int nPartMembers, PlayerCharacterRecord.CharacterGender avatarGender)
        {
            int nTotalPrice = GetPriceBasedOnParty(nPricePerPartyMember, nPartMembers);
            return ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(
                                                                       DataOvlReference.ShoppeKeeperBarKeepStrings2
                                                                           .THAT_WILL_BE_SP) + nTotalPrice.ToString() +
                                                                  DataOvlReference.StringReferences.GetString(
                                                                       DataOvlReference.ShoppeKeeperBarKeepStrings2
                                                                           .SP_GOLD_FOR_THE_SP) +
                                                                   GetNumberInPartyAsStringWord(nPartMembers) +
                                                                   DataOvlReference.StringReferences
                                                                       .GetString(DataOvlReference
                                                                           .ShoppeKeeperBarKeepStrings2.S_OF_YE_COMMA_N)
                                                                       .TrimEnd() +
                                                                   " " + GetGenderedFormalPronoun(avatarGender) + ". " +
                                                                   DataOvlReference.StringReferences
                                                                       .GetString(DataOvlReference
                                                                           .ShoppeKeeperBarKeepStrings2.N_ENJOY_BANG_DQ)
                                                                       .TrimStart());
        }

        public string GetRationOffer(PlayerCharacterRecords records)
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(77, 83);
            return ShoppeKeeperDialogueReference.GetMerchantString(nIndex,  
                nGold:BarKeeperStockReference.GetAdjustedPrice(records.AvatarRecord.Stats.Intelligence,TheBarKeeperStock.RationPrice));
        }

        public override string GetForSaleList()
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(69, 72);
            return ShoppeKeeperDialogueReference.GetMerchantString(nIndex);
        }
    }

}