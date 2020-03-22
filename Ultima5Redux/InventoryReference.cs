using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ultima5Redux;

namespace Ultima5Redux
{
    /// <summary>
    /// Collection of all inventory meta data such as descriptions
    /// </summary>
    public class InventoryReferences
    {
        /// <summary>
        /// All inventory references separated by item types
        /// </summary>
        private readonly Dictionary<string, List<InventoryReference>> invRefsDictionary;
        /// <summary>
        /// All keywords that will be highlighted specifically for reagents 
        /// </summary>
        private readonly List<string> reagentKeywordHighlightList;
        /// <summary>
        /// All keywords that will be highlighted specifically for spells 
        /// </summary>
        private readonly List<string> spellKeywordHighlightList;
        
        /// <summary>
        /// reagents are highlighted green
        /// </summary>
        private const string REAGENT_HIGHLIGHT_COLOR = "<color=#00CC00>";
        /// <summary>
        /// spells are highlighted red
        /// </summary>
        private const string SPELL_HIGHLIGHT_COLOR = "<color=red>";

        /// <summary>
        /// Inventory reference types
        /// </summary>
        public enum InventoryReferenceType { Reagent, Armament, Spell, Item }

        /// <summary>
        /// Return the full list of particular inventory references
        /// </summary>
        /// <param name="inventoryReferenceType"></param>
        /// <returns></returns>
        public List<InventoryReference> GetInventoryReferenceList(InventoryReferenceType inventoryReferenceType)
        {
            return (invRefsDictionary[inventoryReferenceType.ToString()]);
        }

        /// <summary>
        /// Returns a string with all available keywords highlighted
        /// </summary>
        /// <remarks>the string returned is in a richtext format compatible with Unity's TextMeshPro library</remarks>
        /// <param name="description"></param>
        /// <returns>the string with highlight tags</returns>
        public string HighlightKeywords(string description)
        {
            string finalDescription = description;
            
            // highlight all reagents
            foreach (string highlightKeyword in reagentKeywordHighlightList)
            {
                if (!Regex.IsMatch(description, highlightKeyword, RegexOptions.IgnoreCase)) continue;
                
                finalDescription = Regex.Replace(finalDescription, highlightKeyword, REAGENT_HIGHLIGHT_COLOR
                                                                                     + highlightKeyword + "</color>");
                string upperCaseStr = char.ToUpper(highlightKeyword[0]) + highlightKeyword.Substring(1);
                finalDescription = Regex.Replace(finalDescription, upperCaseStr, REAGENT_HIGHLIGHT_COLOR
                                                                                 + upperCaseStr + "</color>");
            }
            // highlight all spell names
            foreach (string highlightKeyword in spellKeywordHighlightList)
            {
                if (!Regex.IsMatch(description, highlightKeyword, RegexOptions.IgnoreCase)) continue;
                
                finalDescription = Regex.Replace(finalDescription, highlightKeyword, SPELL_HIGHLIGHT_COLOR
                                                                                     + highlightKeyword + "</color>");
            }

            return finalDescription;
        }
        
        /// <summary>
        /// Retrieve a specific inventory reference based on reference type and string index
        /// </summary>
        /// <param name="inventoryReferenceType"></param>
        /// <param name="invItem"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
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
            throw new Ultima5ReduxException("Asked for an inventory reference : " + invItem + " but it doesn't exist");
        }

        /// <summary>
        /// Constructor builds reference tables from embedded resources
        /// </summary>
        public InventoryReferences()
        {
            invRefsDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<InventoryReference>>>(Ultima5Redux.Properties.Resources.InventoryDetails);
             
             // we initialize the highlight text list
             reagentKeywordHighlightList = new List<string>();
             spellKeywordHighlightList = new List<string>();

             List<InventoryReference> reagentInvRefs = invRefsDictionary["Reagent"];
             List<InventoryReference> spellInvRefs = invRefsDictionary["Spell"];
             
             // build reagent highlight table
             foreach (InventoryReference invRef in reagentInvRefs)
             {
                 if (invRef.ItemNameHighLights.Length == 0) continue;
                 
                 foreach (string highlightWord in invRef.ItemNameHighLights)
                 {
                     reagentKeywordHighlightList.Add(highlightWord);
                 }
             }
             //build spell name highlight table
             foreach (InventoryReference invRef in spellInvRefs)
             {
                 if (invRef.ItemNameHighLights.Length == 0) continue;
                 foreach (string highlightWord in invRef.ItemNameHighLights)
                 {
                     spellKeywordHighlightList.Add(highlightWord);
                 }
             }
             
        }

    }

 
    /// <summary>
    /// Specific inventory item reference
    /// </summary>
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

        /// <summary>
        /// Gets a formatted description including the attribution of the quoted material
        /// </summary>
        /// <returns></returns>
        public string GetRichTextDescription()
         {
             return "<i>\"" + ItemDescription + "</i>\"" + "\n\n" + "<align=right>- " + ItemDescriptionAttribution + "</align>";
         }   
        
        /// <summary>
        /// Gets a formatted description WITHOUT any attribution
        /// </summary>
        /// <returns></returns>
        public string GetRichTextDescriptionNoAttribution()
        {
            return ItemDescription;
        }
        
    }
}