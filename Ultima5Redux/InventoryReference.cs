using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ultima5Redux3D
{


    //[DataContract]
    public class InventoryReferences
    {
        private Dictionary<string, List<InventoryReference>> invRefsDictionary;
        public enum InventoryReferenceType { Reagent, Armament, Spell, Item }

        public List<InventoryReference> GetInventoryReferenceList(InventoryReferenceType inventoryReferenceType)
        {
            return (invRefsDictionary[inventoryReferenceType.ToString()]);
        }

        public InventoryReference GetInventoryReference(InventoryReferenceType inventoryReferenceType, string invItem)
        {
            // todo: this is a really slow and inefficient way to search the list, albeit a small list
            foreach (InventoryReference invRef in GetInventoryReferenceList(inventoryReferenceType))
            {
                if (invRef.ItemName.Trim() == invItem)
                {
                    return invRef;
                }
            }
            throw new Exception("Asked for an inventory reference : " + invItem + " but it doesn't exist");
        }

        public InventoryReferences()
        {
             invRefsDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<InventoryReference>>>(Ultima5Redux.Properties.Resources.InventoryDetails);
        }

    }

 
    [JsonObject(MemberSerialization.OptIn)]
    public class InventoryReference
    {
        //public string Reagent;
        [JsonProperty]
        public string ItemName { get; set; }
        [JsonProperty]
        public string ItemSprite { get; set; }
        [JsonProperty]
        public string ItemDescription { get; set; }

    
    }
}