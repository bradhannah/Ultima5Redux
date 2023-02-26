using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract]
    public sealed class LordBritishArtifact : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ArtifactType
        {
            Amulet = 439,
            Crown = 437,
            Sceptre = 438
        }

        [DataMember]
        public string EquipMessage =>
            Artifact switch
            {
                ArtifactType.Amulet => GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.WEARING_AMULET),
                ArtifactType.Crown => GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.DON_THE_CROWN),
                ArtifactType.Sceptre => GameReferences.Instance.DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.WIELD_SCEPTRE),
                _ => throw new InvalidEnumArgumentException(((int)Artifact).ToString())
            };

        [DataMember] public ArtifactType Artifact { get; private set; }

        [IgnoreDataMember] public override string FindDescription => InvRef.FriendlyItemName;
        [IgnoreDataMember] public override bool HideQuantity => true;

        [IgnoreDataMember] public override string InventoryReferenceString => Artifact.ToString();

        [JsonConstructor]
        private LordBritishArtifact()
        {
        }

        public LordBritishArtifact(ArtifactType artifact, int quantity) : base(quantity, (int)artifact,
            InventoryReferences.InventoryReferenceType.Item) =>
            Artifact = artifact;

        public static int GetLegacySaveQuantityIndex(ArtifactType artifactType)
        {
            return artifactType switch
            {
                ArtifactType.Amulet => 0x20D,
                ArtifactType.Crown => 0x20E,
                ArtifactType.Sceptre => 0x20F,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public bool HasItem() => Quantity != 0;
    }
}