using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class LordBritishArtifact : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum ArtifactType
        {
            Amulet = 439, Crown = 437, Sceptre = 438
        }

        [IgnoreDataMember] public override bool HideQuantity => true;

        [IgnoreDataMember] public override string InventoryReferenceString => Artifact.ToString();

        [DataMember] public ArtifactType Artifact { get; }

        [DataMember] public string EquipMessage { get; }

        [JsonConstructor] private LordBritishArtifact()
        {
        }
        
        public LordBritishArtifact(ArtifactType artifact, int quantity, string equipMessage) : base(quantity,
            (int)artifact)
        {
            Artifact = artifact;
            EquipMessage = equipMessage;
        }

        public bool HasItem()
        {
            return Quantity != 0;
        }
    }
}