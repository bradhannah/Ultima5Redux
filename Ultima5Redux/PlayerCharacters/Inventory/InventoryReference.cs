using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    /// <summary>
    /// Collection of all inventory meta data such as descriptions
    /// </summary>
    public class InventoryReferences
    {
        /// <summary>
        /// All inventory references separated by item types
        /// </summary>
        private readonly Dictionary<string, List<InventoryReference>> _invRefsDictionary;
        /// <summary>
        /// All inventory references with respective equipment enums
        /// </summary>
        private readonly Dictionary<DataOvlReference.Equipment, InventoryReference> _invRefsByEquipment;
        /// <summary>
        /// a full list of all inventory references
        /// </summary>
        private readonly List<InventoryReference> _invRefs;
        /// <summary>
        /// All keywords that will be highlighted specifically for reagents 
        /// </summary>
        private readonly List<string> _reagentKeywordHighlightList;
        /// <summary>
        /// All keywords that will be highlighted specifically for spells 
        /// </summary>
        private readonly List<string> _spellKeywordHighlightList;
        
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
            return (_invRefsDictionary[inventoryReferenceType.ToString()]);
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
            foreach (string highlightKeyword in _reagentKeywordHighlightList)
            {
                if (!Regex.IsMatch(description, highlightKeyword, RegexOptions.IgnoreCase)) continue;
                
                finalDescription = Regex.Replace(finalDescription, highlightKeyword, REAGENT_HIGHLIGHT_COLOR
                                                                                     + highlightKeyword + "</color>");
                string upperCaseStr = char.ToUpper(highlightKeyword[0]) + highlightKeyword.Substring(1);
                finalDescription = Regex.Replace(finalDescription, upperCaseStr, REAGENT_HIGHLIGHT_COLOR
                                                                                 + upperCaseStr + "</color>");
            }
            // highlight all spell names
            foreach (string highlightKeyword in _spellKeywordHighlightList)
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

        public InventoryReference GetInventoryReference(DataOvlReference.Equipment equipment)
        {
            if (!_invRefsByEquipment.ContainsKey(equipment))
            {
                throw new Ultima5ReduxException("You requested an equipment item that doesn't exist in the dictionary: "+(int)equipment);
            }
            return _invRefsByEquipment[equipment];
        }

        /// <summary>
        /// Constructor builds reference tables from embedded resources
        /// </summary>
        public InventoryReferences()
        {
            _invRefsDictionary = JsonConvert.DeserializeObject<Dictionary<string, List<InventoryReference>>>(Ultima5Redux.Properties.Resources.InventoryDetails);
             
             // we initialize the highlight text list
             _reagentKeywordHighlightList = new List<string>();
             _spellKeywordHighlightList = new List<string>();
             
             _invRefs = new List<InventoryReference>();
             
             foreach (InventoryReference invRef in GetInventoryReferenceList(InventoryReferenceType.Armament))
             {
                 _invRefs.Add(invRef);
             }
             foreach (InventoryReference invRef in GetInventoryReferenceList(InventoryReferenceType.Item))
             {
                 _invRefs.Add(invRef);
             }
             
             // build reagent highlight table
             foreach (InventoryReference invRef in GetInventoryReferenceList(InventoryReferenceType.Reagent))//reagentInvRefs)
             {
                 _invRefs.Add(invRef);
                 
                 if (invRef.ItemNameHighLights.Length == 0) continue;
                 
                 foreach (string highlightWord in invRef.ItemNameHighLights)
                 {
                     _reagentKeywordHighlightList.Add(highlightWord);
                 }
             }
             //build spell name highlight table
             foreach (InventoryReference invRef in GetInventoryReferenceList(InventoryReferenceType.Spell))//spellInvRefs)
             {
                 _invRefs.Add(invRef);

                 if (invRef.ItemNameHighLights.Length == 0) continue;
                 foreach (string highlightWord in invRef.ItemNameHighLights)
                 {
                     _spellKeywordHighlightList.Add(highlightWord);
                 }
             }
             
             _invRefsByEquipment= new Dictionary<DataOvlReference.Equipment, InventoryReference>();
             foreach (InventoryReference invRef in _invRefs)
             {
                 DataOvlReference.Equipment equipment = invRef.GetAsEquipment();
                 if (Enum.IsDefined(typeof(DataOvlReference.Equipment), equipment))
                 {
                     _invRefsByEquipment.Add(equipment, invRef);
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
        public int ItemSpriteExposed { get; set; }
        [JsonProperty]
        public string ItemDescription { get; set; }
        [JsonProperty]
        public string ItemDescriptionAttribution { get; set; }
        [JsonProperty]
        public string ItemNameHighlight { private get; set; }
        [JsonProperty]
        public int MerchantIndex { get; private set; }
        public string[] ItemNameHighLights => ItemNameHighlight.Length == 0 ? new string[0] : ItemNameHighlight.Split(',');

        /// <summary>
        /// Gets the Equipment equivalent if one exists
        /// </summary>
        /// <returns>Equipment enum OR (-1) if non is found</returns>
        public DataOvlReference.Equipment GetAsEquipment() 
        {
            bool bWasValid = Enum.TryParse<DataOvlReference.Equipment>(ItemName, out DataOvlReference.Equipment itemEquipment);
            if (bWasValid)
            {
                return itemEquipment;
            }
            return (DataOvlReference.Equipment)(-1);
        }
        
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