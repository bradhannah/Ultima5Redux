using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class Healer : ShoppeKeeper
    {
        public enum RemedyTypes { Cure, Heal, Resurrect }

        public HealerServices Services { get; }

        
        public Healer(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
            Services = new HealerServices(dataOvlReference);
        }

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new List<ShoppeKeeperOption>() 
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyHealer),
        };
        
        public override string GetHelloResponse(TimeOfDay tod = null, ShoppeKeeperReference shoppeKeeperReference = null)
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(165, 168);
            if (shoppeKeeperReference != null)
                return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                    shoppeKeeperName: shoppeKeeperReference.ShoppeKeeperName,
                    shoppeName: shoppeKeeperReference.ShoppeName,
                    tod: tod);
            throw new Ultima5ReduxException("Can't get a hello response without a ShoppeKeeperReference");        }

        public override string GetWhichWouldYouSee()
        {
            
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                    .DQ_WE_HAVE_POWERS_TO_CURE_HEAL_RESURRECT_DOT_DQ_N)
                + ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                    .SAYS_NAME_DOT_N_N_DQ_WHAT_IS_THE_NATURE_OF_THY_NEED_Q_DQ), shoppeKeeperName: this.TheShoppeKeeperReference.ShoppeKeeperName); 
        }

        public string GetWhoNeedsAid()
        {
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                .N_N_DQ_WHO_NEEDS_MY_AID_Q_DQ).Trim();
        }

        public string GetRemedyVerb(RemedyTypes remedy)
        {
            switch (remedy)
            {
                case RemedyTypes.Cure:
                    return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                        .CURING);
                case RemedyTypes.Heal:
                    return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                        .HEALING);
                case RemedyTypes.Resurrect:
                    return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                        .RESURRECT);
                default:
                    throw new ArgumentOutOfRangeException(nameof(remedy), remedy, null);
            }
        }

        public string NoNeedForMyArt()
        {
            return ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(
                    DataOvlReference.ShoppeKeeperHealerStrings2
                        .DQ_THOU_HAST_NO_NEED_OF_THIS_ART_BANG_DQ_SAYS_NAME).Trim().Replace("\n", " "),
                shoppeKeeperName: this.TheShoppeKeeperReference.ShoppeKeeperName) + "\n" +
                   DataOvlReference.StringReferences.GetString(
                       DataOvlReference.ShoppeKeeperHealerStrings.N_N_DQ_IS_THERE_ANY_OTHER_WAY_IN_WHICH_I_MAY_N).Trim() + "\n"
                + DataOvlReference.StringReferences.GetString(
                       DataOvlReference.ShoppeKeeperHealerStrings.AID_THEE_Q);
        }

        public string GetHealerRemedyOfferPrice(RemedyTypes remedy)
        {
            string offerStr;
            switch (remedy)
            {
                case RemedyTypes.Cure:
                    offerStr = DataOvlReference.StringReferences.GetString(
                        DataOvlReference.ShoppeKeeperHealerStrings.I_CAN_CURE_THY_POISONED_BODY);
                    break;
                case RemedyTypes.Heal:
                    offerStr = DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings.I_CAN_HEAL_THEE);
                    break;
                case RemedyTypes.Resurrect:
                    offerStr = DataOvlReference.StringReferences.GetString(
                                   DataOvlReference.ShoppeKeeperHealerStrings.I_CAN_RAISE_THIS_UNFORTUNATE_PERSON_FROM)
                               + DataOvlReference.StringReferences.GetString(
                                   DataOvlReference.ShoppeKeeperHealerStrings.THE_DEAD);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(remedy), remedy, null);
            }
            offerStr += ShoppeKeeperDialogueReference.GetMerchantString(DataOvlReference.StringReferences.GetString(
                DataOvlReference.ShoppeKeeperHealerStrings
                    .FOR_GOLD_GOLD_DO_N_N_WILT_THO_N_PAY_Q_DQ).Replace("\np", " p"), nGold: GetPrice(remedy));
            return offerStr;

        }
        
        public bool DoesPlayerNeedRemedy(RemedyTypes remedy, PlayerCharacterRecord record)
        {
            switch (remedy)
            {
                case RemedyTypes.Cure:
                    return record.Stats.Status == PlayerCharacterRecord.CharacterStatus.Poisioned;
                case RemedyTypes.Heal:
                    return record.Stats.CurrentHp < record.Stats.MaximumHp;
                case RemedyTypes.Resurrect:
                    return record.Stats.Status == PlayerCharacterRecord.CharacterStatus.Dead;
                default:
                    throw new ArgumentOutOfRangeException(nameof(remedy), remedy, null);
            }
        }
        
        public override string GetForSaleList()
        {
            throw new System.NotImplementedException();
        }

        public override string GetThanksAfterPurchaseResponse()
        {
            //
            return DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                    .N_N_DQ_IS_THERE_ANY_OTHER_WAY_IN_WHICH_I_MAY_N).Trim() + "\n" +
                DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                        .AID_THEE_Q).Trim();
        }

        /// <summary>
        /// Gets the price of the given service at the particular location
        /// </summary>
        /// <param name="remedy"></param>
        /// <returns></returns>
        public int GetPrice(RemedyTypes remedy)
        {
            return Services.GetServicePrice(this.TheShoppeKeeperReference.ShoppeKeeperLocation, remedy);
        }
    }
}