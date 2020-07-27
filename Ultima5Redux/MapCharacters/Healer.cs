using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapCharacters
{
    public class Healer : ShoppeKeeper
    {
        public enum RemedyTypes { Cure, Heal, Resurrect }
        
        public Healer(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference, ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) : base(shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference)
        {
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
                       DataOvlReference.ShoppeKeeperInnkeeper2Strings.IS_THERE_N_ANYTHING_MORE_N_I_CAN_DO_FOR_N_THEE_Q_DQ).Replace("\n", " ");
        }
        
        public bool DoesPlayerNeedRemedy(RemedyTypes remedy, PlayerCharacterRecord record)
        {
            switch (remedy)
            {
                case RemedyTypes.Cure:
                    break;
                case RemedyTypes.Heal:
                    break;
                case RemedyTypes.Resurrect:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(remedy), remedy, null);
            }
            return true;
        }
        
        public override string GetForSaleList()
        {
            throw new System.NotImplementedException();
        }
    }
}