using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class LordBritishArtifact : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ArtifactType { Amulet = 439, Crown = 437, Sceptre = 438 }

        [DataMember]
        public string EquipMessage =>
            Artifact switch
            {
                ArtifactType.Amulet => GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference
                    .WearUseItemStrings.WEARING_AMULET),
                ArtifactType.Crown => GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference
                    .WearUseItemStrings.DON_THE_CROWN),
                ArtifactType.Sceptre => GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference
                    .WearUseItemStrings.WIELD_SCEPTRE),
                _ => throw new ArgumentOutOfRangeException()
            };

        [DataMember] public ArtifactType Artifact { get; private set; }

        [IgnoreDataMember] public override string FindDescription => InvRef.FriendlyItemName;
        [IgnoreDataMember] public override bool HideQuantity => true;

        [IgnoreDataMember] public override string InventoryReferenceString => Artifact.ToString();

        [JsonConstructor] private LordBritishArtifact()
        {
        }

        public LordBritishArtifact(ArtifactType artifact, int quantity) : base(quantity, (int)artifact,
            InventoryReferences.InventoryReferenceType.Item)
        {
            Artifact = artifact;
        }

        public bool HasItem()
        {
            return Quantity != 0;
        }
    }
}