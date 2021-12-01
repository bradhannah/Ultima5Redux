using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Potion : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum PotionColor
        {
            Blue = 0x282, Yellow = 0x283, Red = 0x284, Green = 0x285, Orange = 0x286, Purple = 0x287, Black = 0x288,
            White = 0x289
        }

        private const int POTION_SPRITE = 259;

        [DataMember] public PotionColor Color { get; private set; }

        [IgnoreDataMember] public override string FindDescription => Color + " potion";

        [IgnoreDataMember] public override bool HideQuantity => false;

        [IgnoreDataMember] public override string InventoryReferenceString => Color.ToString();

        [JsonConstructor] private Potion()
        {
        }

        public Potion(PotionColor color, int quantity) : base(quantity, POTION_SPRITE,
            InventoryReferences.InventoryReferenceType.Item)
        {
            Color = color;
        }
    }
}