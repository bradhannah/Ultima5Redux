using System.Diagnostics;

namespace Ultima5Redux.PlayerCharacters
{
    public class ShadowlordShard : InventoryItem
    {
        public enum ShardType { Falsehood, Hatred, Cowardice};

        public ShardType Shard { get; }
        public string EquipMessage { get; }

        private const int SHARD_SPRITE = 436;
        public override bool HideQuantity { get; } = true;

        public ShadowlordShard(ShardType shardType, int quantity, string longName, string equipMessage) : base(quantity, longName, longName, SHARD_SPRITE)
        {
            Debug.WriteLine("Shard: " + shardType.ToString());
            Shard = shardType;
            EquipMessage = equipMessage;
        }
    }
}