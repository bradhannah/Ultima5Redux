using System;
using System.Runtime.Serialization;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    /// <summary>
    ///     Specific inventory item reference
    /// </summary>
    [DataContract]
    public class InventoryReference
    {
        [DataMember] public InventoryReferences.InventoryReferenceType InvRefType { get; internal set; }
        [DataMember] public string ItemDescription { get; set; }

        [DataMember] public string ItemDescriptionAttribution { get; set; }
        [DataMember] public string ItemName { get; set; }
        [DataMember] public string ItemNameHighlight { private get; set; }

        [DataMember] public string ItemSprite { get; set; }
        [DataMember] public int ItemSpriteExposed { get; set; }
        [IgnoreDataMember] public string FriendlyItemName => Utils.GetFriendlyString(ItemName);

        [IgnoreDataMember]
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
        public string GetRichTextDescription() =>
            "<i>\"" + ItemDescription + "</i>\"" + "\n\n" + "<align=right>- " + ItemDescriptionAttribution +
            "</align>";

        /// <summary>
        ///     Gets a formatted description WITHOUT any attribution
        /// </summary>
        /// <returns></returns>
        public string GetRichTextDescriptionNoAttribution() => ItemDescription;
    }
}