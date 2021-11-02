using System.Diagnostics;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class ShadowlordShard : InventoryItem
    {
        public enum ShardType { Falsehood, Hatred, Cowardice }

        private const int SHARD_SPRITE = 436;

        public ShadowlordShard(ShardType shardType, int quantity, string longName, string equipMessage) : base(quantity,
             SHARD_SPRITE)
        {
            Debug.WriteLine("Shard: " + shardType);
            Shard = shardType;
            EquipMessage = equipMessage;
        }

        public override bool HideQuantity { get; } = true;

        public ShardType Shard { get; }
        public string EquipMessage { get; }

        public override string InventoryReferenceString => Shard.ToString();
    }
}