using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ultima5Redux.Data
{
    /// <summary>
    /// A shortcut class that allows you to put in the STRING enum without the DataChunkName when looking up original U5 strings
    /// </summary>
    public class U5StringRef
    {
        private DataOvlReference _dataRef;
        public U5StringRef(DataOvlReference dataRef)
        {
            this._dataRef = dataRef;
            _strMap = new Dictionary<Type, SomeStrings>();
            //SomeStrings strs = dataRef.GetDataChunk(DataOvlReference.DataChunkName.TRAVEL).GetChunkAsStringList();

            _strMap.Add(typeof(DataOvlReference.ChunkPhrasesConversation), dataRef.GetDataChunk(DataOvlReference.DataChunkName.PHRASES_CONVERSATION).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.TravelStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.TRAVEL).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.LocationStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.LOCATION_NAMES).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.WorldStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.WORLD).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ChitChatStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.CHIT_CHAT).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.KeypressCommandsStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.KEYPRESS_COMMANDS).GetChunkAsStringList());
            //_strMap.Add(typeof(DataOvlReference.Vision1Strings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.VISION1).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.Vision2Strings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.VISION2).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.OpeningThingsStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.OPENING_THINGS_STUFF).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.KlimbingStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.KLIMBING).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.GetThingsStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.GET_THINGS).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.SpecialItemNamesStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SPECIAL_ITEM_NAMES).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.WearUseItemStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.WEAR_USE_ITEM).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShardsStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHARDS).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShadowlordStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.WORDS_OF_POWER).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.PotionsStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.POTIONS).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.SpellStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SPELLS).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.SpecialItemNames2Strings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SPECIAL_ITEM_NAMES2).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.LongArmourString), dataRef.GetDataChunk(DataOvlReference.DataChunkName.LONG_ARMOUR).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShortArmourString), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHORT_ARMOUR).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.EquippingStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIPPING).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ZstatsStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.ZSTATS).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.SleepTransportStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SLEEP_TRANSPORT).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ReagentStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.REAGENTS).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ExclaimStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.EXCLAIMS).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ThingsIFindStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.THINGS_I_FIND).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShoppeKeeperSellingStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_SELLING).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShoppeKeeperBlacksmithPositiveExclamation), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_POS_EXCLAIM).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShoppeKeeperBlacksmithHello), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_HELLO).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShoppeKeeperBlacksmithWeHave), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_BLACKSMITH_WE_HAVE).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShoppeKeeperGeneralStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_GENERAL).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShoppeKeeperInnkeeperStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_INNKEEPER).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShoppeKeeperInnkeeper2Strings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_INNKEEPER_2).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShoppeKeeperReagentStrings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_REAGENTS).GetChunkAsStringList());
            _strMap.Add(typeof(DataOvlReference.ShoppeKeeperGeneral2Strings), dataRef.GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_GENERAL_2).GetChunkAsStringList());
        }

        private readonly Dictionary<Type, SomeStrings> _strMap;

        /// <summary>
        /// Returns a string based on an enumeration
        /// </summary>
        /// <remarks>I wrote this not because I should - but because I could. I feel both pride and shame.</remarks>
        /// <param name="strObj"></param>
        /// <returns></returns>
        public string GetString(object strObj) 
        {
            Debug.Assert(_strMap.ContainsKey(strObj.GetType()));

            return _strMap[strObj.GetType()].Strs[(int)strObj];
        }
  
    }
}
