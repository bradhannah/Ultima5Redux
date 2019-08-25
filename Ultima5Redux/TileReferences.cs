using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ultima5Redux
{
    public class TileReferences
    {
        public Dictionary<int, TileReference> TileReferenceDictionary { get; }

        public TileReferences()
        {
            TileReferenceDictionary = TileReferences.Load();
        }

        static public Dictionary<int, TileReference> Load()
        {
            Dictionary<int, TileReference> result = JsonConvert.DeserializeObject<Dictionary<int, TileReference>>(Properties.Resources.TileData);

            return result;
        }
    }
}
