namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class LordBritishArtifact : InventoryItem
    {
        public enum ArtifactType { Amulet = 439, Crown = 437, Sceptre = 438 }

        public LordBritishArtifact(ArtifactType artifact, int quantity, string longName, string equipMessage) : base(
            quantity, longName, longName, (int) artifact)
        {
            Artifact = artifact;
            EquipMessage = equipMessage;
        }

        public string EquipMessage { get; }

        public override bool HideQuantity { get; } = true;

        public ArtifactType Artifact { get; }

        public bool HasItem()
        {
            return Quantity != 0;
        }
    }
}