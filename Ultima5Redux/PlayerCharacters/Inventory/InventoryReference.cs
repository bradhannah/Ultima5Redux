using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    /// <summary>
    ///     Specific inventory item reference
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)] public class InventoryReference
    {
        [JsonProperty] public int ItemSpriteExposed { get; set; }

        [JsonProperty] public string ItemDescription { get; set; }

        [JsonProperty] public string ItemDescriptionAttribution { get; set; }
        [JsonProperty] public string ItemName { get; set; }
        [IgnoreDataMember] public string FriendlyItemName => Utils.GetFriendlyString(ItemName);
        [JsonProperty] public string ItemNameHighlight { private get; set; }

        [JsonProperty] public string ItemSprite { get; set; }

        public string[] ItemNameHighLights =>
            ItemNameHighlight.Length == 0 ? Array.Empty<string>() : ItemNameHighlight.Split(',');

        /// <summary>
        ///     Gets the Equipment equivalent if one exists
        /// </summary>
        /// <returns>Equipment enum OR (-1) if non is found</returns>
        public DataOvlReference.Equipment GetAsEquipment()
        {
            bool bWasValid = Enum.TryParse(ItemName, out DataOvlReference.Equipment itemEquipment);
            if (bWasValid) return itemEquipment;
            return (DataOvlReference.Equipment)(-1);
        }

        /// <summary>
        ///     Gets a formatted description including the attribution of the quoted material
        /// </summary>
        /// <returns></returns>
        public string GetRichTextDescription()
        {
            return "<i>\"" + ItemDescription + "</i>\"" + "\n\n" + "<align=right>- " + ItemDescriptionAttribution +
                   "</align>";
        }

        /// <summary>
        ///     Gets a formatted description WITHOUT any attribution
        /// </summary>
        /// <returns></returns>
        public string GetRichTextDescriptionNoAttribution()
        {
            return ItemDescription;
        }
    }
}