using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ultima5Redux3D
{


    //[DataContract]
    public class InventoryReferences
    {
        private Dictionary<string, List<InventoryReference>> invRefsDictionary;
        private List<string> keywordHighlightList;
        public enum InventoryReferenceType { Reagent, Armament, Spell, Item }

        public List<InventoryReference> GetInventoryReferenceList(InventoryReferenceType inventoryReferenceType)
        {
            return (invRefsDictionary[inventoryReferenceType.ToString()]);
        }

        private const string HIGHLIGHT_COLOR = "<color=#00CC00>";

        public string HighlightKeywords(string description)
        {
            string finalDescription = description;
            foreach (string highlightKeyword in keywordHighlightList)
            {
                if (!Regex.IsMatch(description, highlightKeyword, RegexOptions.IgnoreCase)) continue;
                
                finalDescription = Regex.Replace(finalDescription, highlightKeyword, HIGHLIGHT_COLOR
                                                                                     + highlightKeyword + "</color>");
                string upperCaseStr = char.ToUpper(highlightKeyword[0]) + highlightKeyword.Substring(1);
                finalDescription = Regex.Replace(finalDescription, upperCaseStr, HIGHLIGHT_COLOR
                                                                                 + upperCaseStr + "</color>");
            }

            return finalDescription;
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
             
             // we initialize the highlight text list
             keywordHighlightList = new List<string>();
             foreach (List<InventoryReference> invRefs in invRefsDictionary.Values)
             {
                 foreach (InventoryReference invRef in invRefs)
                 {
                     if (invRef.ItemNameHighLights.Length > 0)
                     {
                         foreach (string highlightWord in invRef.ItemNameHighLights)
                         {
                             keywordHighlightList.Add(highlightWord);
                         }
                     }
                 }
             }
             
        }

    }

 
    [JsonObject(MemberSerialization.OptIn)]
    public class InventoryReference
    {
        [JsonProperty]
        public string ItemName { get; set; }
        [JsonProperty]
        public string ItemSprite { get; set; }
        [JsonProperty]
        public string ItemDescription { get; set; }
        [JsonProperty]
        public string ItemDescriptionAttribution { get; set; }
        [JsonProperty]
        public string ItemNameHighlight { private get; set; }

        public string[] ItemNameHighLights => ItemNameHighlight.Length == 0 ? new string[0] : ItemNameHighlight.Split(',');

        public string GetRichTextDescription()
        {
            return "<i>\"" + ItemDescription + "</i>\"" + "\n\n" + "<align=right>- " + ItemDescriptionAttribution +
                   "</align>";
        }
        
        public string GetRichTextDescriptionNoAttribution()
        {
            return ItemDescription;
        }
        
    }
}