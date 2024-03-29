﻿using System;
using System.Collections.Generic;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.References;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class Healer : ShoppeKeeper
    {
        public enum RemedyTypes
        {
            Cure,
            Heal,
            Resurrect
        }

        public override List<ShoppeKeeperOption> ShoppeKeeperOptions => new()
        {
            new ShoppeKeeperOption("Buy", ShoppeKeeperOption.DialogueType.BuyHealer)
        };

        public HealerServices Services { get; }

        public Healer(ShoppeKeeperDialogueReference shoppeKeeperDialogueReference,
            ShoppeKeeperReference theShoppeKeeperReference, DataOvlReference dataOvlReference) : base(
            shoppeKeeperDialogueReference, theShoppeKeeperReference, dataOvlReference) =>
            Services = new HealerServices(dataOvlReference);

        public override string GetForSaleList() => throw new NotImplementedException();

        public override string GetHelloResponse(TimeOfDay tod = null)
        {
            int nIndex = ShoppeKeeperDialogueReference.GetRandomMerchantStringIndexFromRange(165, 168);
            return "\"" + ShoppeKeeperDialogueReference.GetMerchantString(nIndex,
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName,
                shoppeName: TheShoppeKeeperReference.ShoppeName, tod: tod);
        }

        public override string GetThanksAfterPurchaseResponse() =>
            //
            DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                .N_N_DQ_IS_THERE_ANY_OTHER_WAY_IN_WHICH_I_MAY_N).Trim() + "\n" + DataOvlReference.StringReferences
                .GetString(DataOvlReference.ShoppeKeeperHealerStrings.AID_THEE_Q).Trim();

        public override string GetWhichWouldYouSee() =>
            DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                .DQ_WE_HAVE_POWERS_TO_CURE_HEAL_RESURRECT_DOT_DQ_N) + ShoppeKeeperDialogueReference.GetMerchantString(
                DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                    .SAYS_NAME_DOT_N_N_DQ_WHAT_IS_THE_NATURE_OF_THY_NEED_Q_DQ),
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName);

        public bool DoesPlayerNeedRemedy(RemedyTypes remedy, PlayerCharacterRecord record)
        {
            return remedy switch
            {
                RemedyTypes.Cure => record.Stats.Status == PlayerCharacterRecord.CharacterStatus.Poisoned,
                RemedyTypes.Heal => record.Stats.CurrentHp < record.Stats.MaximumHp,
                RemedyTypes.Resurrect => record.Stats.Status == PlayerCharacterRecord.CharacterStatus.Dead,
                _ => throw new ArgumentOutOfRangeException(nameof(remedy), remedy, null)
            };
        }

        public string GetHealerRemedyOfferPrice(RemedyTypes remedy)
        {
            string offerStr = remedy switch
            {
                RemedyTypes.Cure => DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings.I_CAN_CURE_THY_POISONED_BODY),
                RemedyTypes.Heal => "\"" +
                                    DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings.I_CAN_HEAL_THEE),
                RemedyTypes.Resurrect => DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings.I_CAN_RAISE_THIS_UNFORTUNATE_PERSON_FROM) +
                                         DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings.THE_DEAD),
                _ => throw new ArgumentOutOfRangeException(nameof(remedy), remedy, null)
            };

            offerStr += ShoppeKeeperDialogueReference.GetMerchantString(
                DataOvlReference.StringReferences
                    .GetString(DataOvlReference.ShoppeKeeperHealerStrings.FOR_GOLD_GOLD_DO_N_N_WILT_THO_N_PAY_Q_DQ)
                    .Replace("\np", " p"), GetPrice(remedy));
            return offerStr;
        }

        /// <summary>
        ///     Gets the price of the given service at the particular location
        /// </summary>
        /// <param name="remedy"></param>
        /// <returns></returns>
        public int GetPrice(RemedyTypes remedy) =>
            Services.GetServicePrice(TheShoppeKeeperReference.ShoppeKeeperLocation, remedy);

        public string GetRemedyVerb(RemedyTypes remedy)
        {
            return remedy switch
            {
                RemedyTypes.Cure => DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings.CURING),
                RemedyTypes.Heal => DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings.HEALING),
                RemedyTypes.Resurrect => DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings.RESURRECT),
                _ => throw new ArgumentOutOfRangeException(nameof(remedy), remedy, null)
            };
        }

        public string GetWhoNeedsAid() =>
            DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                .N_N_DQ_WHO_NEEDS_MY_AID_Q_DQ).Trim();

        public string NoNeedForMyArt() =>
            ShoppeKeeperDialogueReference.GetMerchantString(
                DataOvlReference.StringReferences
                    .GetString(DataOvlReference.ShoppeKeeperHealerStrings2
                        .DQ_THOU_HAST_NO_NEED_OF_THIS_ART_BANG_DQ_SAYS_NAME).Trim().Replace("\n", " "),
                shoppeKeeperName: TheShoppeKeeperReference.ShoppeKeeperName) + "\n\n" +
            DataOvlReference.StringReferences.GetString(
                    DataOvlReference.ShoppeKeeperHealerStrings.N_N_DQ_IS_THERE_ANY_OTHER_WAY_IN_WHICH_I_MAY_N)
                .Trim() +
            "\n" + DataOvlReference.StringReferences.GetString(DataOvlReference.ShoppeKeeperHealerStrings
                .AID_THEE_Q);
    }
}