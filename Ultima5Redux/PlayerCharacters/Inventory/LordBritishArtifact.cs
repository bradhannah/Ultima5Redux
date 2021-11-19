namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public sealed class LordBritishArtifact : InventoryItem
    {
        public enum ArtifactType { Amulet = 439, Crown = 437, Sceptre = 438 }

        public override bool HideQuantity { get; } = true;

        public override string InventoryReferenceString => Artifact.ToString();

        public ArtifactType Artifact { get; }

        public string EquipMessage { get; }

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