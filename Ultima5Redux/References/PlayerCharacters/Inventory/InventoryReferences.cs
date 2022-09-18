using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.Properties;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    /// <summary>
    ///     Collection of all inventory meta data such as descriptions
    /// </summary>
    public class InventoryReferences
    {
        /// <summary>
        ///     Inventory reference types
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum InventoryReferenceType { Reagent, Armament, Spell, Item }

        /// <summary>
        ///     reagents are highlighted green
        /// </summary>
        private const string REAGENT_HIGHLIGHT_COLOR = "<color=#00CC00>";

        /// <summary>
        ///     spells are highlighted red
        /// </summary>
        private const string SPELL_HIGHLIGHT_COLOR = "<color=red>";

        /// <summary>
        ///     All inventory references with respective equipment enums
        /// </summary>
        private readonly Dictionary<DataOvlReference.Equipment, InventoryReference> _invRefsByEquipment;

        /// <summary>
        ///     All keywords that will be highlighted specifically for reagents
        /// </summary>
        private readonly List<string> _reagentKeywordHighlightList;

        /// <summary>
        ///     All keywords that will be highlighted specifically for spells
        /// </summary>
        private readonly List<string> _spellKeywordHighlightList;

        /// <summary>
        ///     All inventory references separated by item types
        /// </summary>
        public readonly Dictionary<string, List<InventoryReference>> _invRefsDictionary;

        /// <summary>
        ///     Constructor builds reference tables from embedded resources
        /// </summary>
        public InventoryReferences()
        {
            _invRefsDictionary =
                JsonConvert.DeserializeObject<Dictionary<string, List<InventoryReference>>>(Resources.InventoryDetails);

            // we initialize the highlight text list
            _reagentKeywordHighlightList = new List<string>();
            _spellKeywordHighlightList = new List<string>();

            List<InventoryReference> invRefs = new();

            foreach (InventoryReference invRef in GetInventoryReferenceList(InventoryReferenceType.Armament))
            {
                invRef.InvRefType = InventoryReferenceType.Armament;
                invRefs.Add(invRef);
            }

            foreach (InventoryReference invRef in GetInventoryReferenceList(InventoryReferenceType.Item))
            {
                invRef.InvRefType = InventoryReferenceType.Item;
                invRefs.Add(invRef);
            }

            // build reagent highlight table
            foreach (InventoryReference invRef in GetInventoryReferenceList(InventoryReferenceType.Reagent))
            {
                invRef.InvRefType = InventoryReferenceType.Reagent;
                invRefs.Add(invRef);

                if (invRef.ItemNameHighLights.Length == 0) continue;

                foreach (string highlightWord in invRef.ItemNameHighLights)
                {
                    _reagentKeywordHighlightList.Add(highlightWord);
                }
            }

            //build spell name highlight table
            foreach (InventoryReference invRef in GetInventoryReferenceList(InventoryReferenceType.Spell))
            {
                invRef.InvRefType = InventoryReferenceType.Spell;
                invRefs.Add(invRef);

                if (invRef.ItemNameHighLights.Length == 0) continue;
                foreach (string highlightWord in invRef.ItemNameHighLights)
                {
                    _spellKeywordHighlightList.Add(highlightWord);
                }
            }

            _invRefsByEquipment = new Dictionary<DataOvlReference.Equipment, InventoryReference>();
            foreach (InventoryReference invRef in invRefs)
            {
                DataOvlReference.Equipment equipment = invRef.GetAsEquipment();
                if (Enum.IsDefined(typeof(DataOvlReference.Equipment), equipment))
                    _invRefsByEquipment.Add(equipment, invRef);
            }
        }

        /// <summary>
        ///     Retrieve a specific inventory reference based on reference type and string index
        /// </summary>
        /// <param name="inventoryReferenceType"></param>
        /// <param name="invItem"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public InventoryReference GetInventoryReference(InventoryReferenceType inventoryReferenceType, string invItem)
        {
            foreach (InventoryReference invRef in GetInventoryReferenceList(inventoryReferenceType))
            {
                if (invRef.ItemName.Trim() == invItem) return invRef;
            }

            throw new Ultima5ReduxException("Asked for an inventory reference : " + invItem + " but it doesn't exist");
        }

        public InventoryReference GetInventoryReference(DataOvlReference.Equipment equipment)
        {
            if (!_invRefsByEquipment.ContainsKey(equipment))
                throw new Ultima5ReduxException(
                    "You requested an equipment item that doesn't exist in the dictionary: " + (int)equipment);
            return _invRefsByEquipment[equipment];
        }

        /// <summary>
        ///     Return the full list of particular inventory references
        /// </summary>
        /// <param name="inventoryReferenceType"></param>
        /// <returns></returns>
        public List<InventoryReference> GetInventoryReferenceList(InventoryReferenceType inventoryReferenceType) =>
            _invRefsDictionary[inventoryReferenceType.ToString()];

        public IEnumerable<InventoryReference> GetInventoryReferences(int nSpriteIndex) =>
            from innerInvRefs in _invRefsDictionary.Values.ToList()
            from invRef in innerInvRefs
            where invRef.ItemSpriteExposed == nSpriteIndex
            select invRef;

        /// <summary>
        ///     Returns a string with all available keywords highlighted
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

                finalDescription = Regex.Replace(finalDescription, highlightKeyword,
                    REAGENT_HIGHLIGHT_COLOR + highlightKeyword + "</color>");
                string upperCaseStr = char.ToUpper(highlightKeyword[0]) + highlightKeyword.Substring(1);
                finalDescription = Regex.Replace(finalDescription, upperCaseStr,
                    REAGENT_HIGHLIGHT_COLOR + upperCaseStr + "</color>");
            }

            // highlight all spell names
            foreach (string highlightKeyword in _spellKeywordHighlightList)
            {
                if (!Regex.IsMatch(description, highlightKeyword, RegexOptions.IgnoreCase)) continue;

                finalDescription = Regex.Replace(finalDescription, highlightKeyword,
                    SPELL_HIGHLIGHT_COLOR + highlightKeyword + "</color>");
            }

            return finalDescription;
        }
    }
}