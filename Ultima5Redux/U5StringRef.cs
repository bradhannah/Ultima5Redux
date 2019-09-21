using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class U5StringRef
    {
        private DataOvlReference dataRef;
        public U5StringRef(DataOvlReference dataRef)
        {
            this.dataRef = dataRef;
            strMap = new Dictionary<Type, SomeStrings>();
            SomeStrings strs = dataRef.GetDataChunk(DataOvlReference.DataChunkName.TRAVEL).GetChunkAsStringList();

            strMap.Add(typeof(DataOvlReference.TRAVEL_STRINGS), dataRef.GetDataChunk(DataOvlReference.DataChunkName.TRAVEL).GetChunkAsStringList());
            strMap.Add(typeof(DataOvlReference.LOCATION_STRINGS), dataRef.GetDataChunk(DataOvlReference.DataChunkName.LOCATION_NAMES).GetChunkAsStringList());
            strMap.Add(typeof(DataOvlReference.WORLD_STRINGS), dataRef.GetDataChunk(DataOvlReference.DataChunkName.WORLD).GetChunkAsStringList());
            strMap.Add(typeof(DataOvlReference.CHIT_CHAT_STRINGS), dataRef.GetDataChunk(DataOvlReference.DataChunkName.CHIT_CHAT).GetChunkAsStringList());
            strMap.Add(typeof(DataOvlReference.KEYPRESS_COMMANDS_STRINGS), dataRef.GetDataChunk(DataOvlReference.DataChunkName.KEYPRESS_COMMANDS).GetChunkAsStringList());
        }

        private Dictionary<Type, SomeStrings> strMap;

        /// <summary>
        /// Returns a string based on an enumeration
        /// </summary>
        /// <remarks>I wrote this not because I should - but because I could. I feel both pride and shame.</remarks>
        /// <param name="strObj"></param>
        /// <returns></returns>
        public string GetString(object strObj) 
        {
            Debug.Assert(strMap.ContainsKey(strObj.GetType()));

            return strMap[strObj.GetType()].Strs[(int)strObj];
        }
  
    }
}
