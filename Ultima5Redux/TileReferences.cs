using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

// produces light
// light radius
// is pushable
// is useable
// is searchable
// 

namespace Ultima5Redux
{
    public class TileReferences
    {
        public Dictionary<int, TileReference> TileReferenceDictionary { get; }
        public Dictionary<string, TileReference> TileReferenceByStringDictionary { get; } = new Dictionary<string, TileReference>(512);

        public TileReferences()
        {
            TileReferenceDictionary = TileReferences.Load();

            //foreach (TileReference tileRef in TileReferenceDictionary)
            for (int i = 0; i < TileReferenceDictionary.Count; i++)
            {
                TileReference tileRef = TileReferenceDictionary[i];
                // this is gross, but I can't seem to get the index number in any other way...
                tileRef.Index = i;
                TileReferenceByStringDictionary.Add(tileRef.Name, tileRef);
            }
        }

        static public Dictionary<int, TileReference> Load()
        {
            Dictionary<int, TileReference> result = JsonConvert.DeserializeObject<Dictionary<int, TileReference>>(Properties.Resources.TileData);

            return result;
        }
    }
}
