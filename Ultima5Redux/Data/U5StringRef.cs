using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ultima5Redux.Data
{
    /// <summary>
    ///     A shortcut class that allows you to put in the STRING enum without the DataChunkName when looking up original U5
    ///     strings
    /// </summary>
    public class U5StringRef
    {
        private readonly Dictionary<Type, SomeStrings> _strMap;

        public U5StringRef(DataOvlReference dataRef)
        {
            // is it ugly? Yes. Is it magical? Also yes.
            _strMap = new Dictionary<Type, SomeStrings>
            {
                {
                    typeof(DataOvlReference.ChunkPhrasesConversation),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.PHRASES_CONVERSATION).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.TravelStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.TRAVEL).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.LocationStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.LOCATION_NAMES).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.WorldStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.WORLD).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ChitChatStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.CHIT_CHAT).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.KeypressCommandsStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.KEYPRESS_COMMANDS).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.Vision2Strings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.VISION2).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.OpeningThingsStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.OPENING_THINGS_STUFF).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.KlimbingStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.KLIMBING).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.GetThingsStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.GET_THINGS).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.SpecialItemNamesStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SPECIAL_ITEM_NAMES).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.WearUseItemStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.WEAR_USE_ITEM).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShardsStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHARDS).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShadowlordStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.WORDS_OF_POWER).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.PotionsStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.POTIONS).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.SpellStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SPELLS).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.SpecialItemNames2Strings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SPECIAL_ITEM_NAMES2).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.LongArmourString),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.LONG_ARMOUR).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShortArmourString),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHORT_ARMOUR).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.EquippingStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIPPING).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ZstatsStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.ZSTATS).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.SleepTransportStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SLEEP_TRANSPORT).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ReagentStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.REAGENTS).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ExclaimStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.EXCLAIMS).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ThingsIFindStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.THINGS_I_FIND).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperSellingStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_SELLING).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperBlacksmithPositiveExclamation), dataRef
                        .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_POS_EXCLAIM)
                        .GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperBlacksmithHello), dataRef
                        .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_HELLO)
                        .GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperBlacksmithWeHave), dataRef
                        .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_WE_HAVE)
                        .GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperGeneralStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_GENERAL).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperInnkeeperStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_INNKEEPER).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperInnkeeper2Strings), dataRef
                        .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_INNKEEPER_2)
                        .GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperReagentStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_REAGENTS).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperGeneral2Strings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_GENERAL_2).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperHealerStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_HEALER).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperHealerStrings2),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_HEALER2).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperBarKeepStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BAR_KEEP).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.ShoppeKeeperBarKeepStrings2),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BAR_KEEP_2).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.YellingStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.YELLING).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.WordsOfPower),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.WORDS_OF_POWER).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.WorldStrings2),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.WORLD2).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.EnemyIndividualNamesMixed),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.MONSTER_NAMES_MIXED).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.EnemyOutOfCombatNamesUpper),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.MONSTER_NAMES_UPPER).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.BattleStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.BATTLE).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.AdditionalStrings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.ADDITIONAL).GetChunkAsStringList()
                },
                {
                    typeof(DataOvlReference.Battle2Strings),
                    dataRef.GetDataChunk(DataOvlReference.DataChunkName.BATTLE2).GetChunkAsStringList()
                },
            };
        }

        /// <summary>
        ///     Returns a string based on an enumeration
        /// </summary>
        /// <remarks>I wrote this not because I should - but because I could. I feel both pride and shame.</remarks>
        /// <param name="strObj"></param>
        /// <returns></returns>
        public string GetString(object strObj)
        {
            Debug.Assert(_strMap.ContainsKey(strObj.GetType()));

            return _strMap[strObj.GetType()].StringList[(int)strObj];
        }

        public string GetDirectionString(Point2D.Direction direction)
        {
            switch (direction)
            {
                case Point2D.Direction.Up:
                    return GetString(DataOvlReference.TravelStrings.NORTH);
                case Point2D.Direction.Down:
                    return GetString(DataOvlReference.TravelStrings.SOUTH);
                case Point2D.Direction.Left:
                    return GetString(DataOvlReference.TravelStrings.WEST);
                case Point2D.Direction.Right:
                    return GetString(DataOvlReference.TravelStrings.EAST);
                case Point2D.Direction.None:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
    }
}