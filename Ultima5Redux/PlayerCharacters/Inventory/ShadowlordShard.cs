using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class ShadowlordShard : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum ShardType { Falsehood, Hatred, Cowardice }

        private const int SHARD_SPRITE = 436;

        [IgnoreDataMember] public override bool HideQuantity => true;

        [IgnoreDataMember] public override string InventoryReferenceString => Shard.ToString();
        [DataMember] public string EquipMessage { get; }
        [DataMember] public ShardType Shard { get; }

        [JsonConstructor] private ShadowlordShard()
        {
        }

        public ShadowlordShard(ShardType shardType, int quantity, string equipMessage) : base(quantity, SHARD_SPRITE)
        {
            Debug.WriteLine("Shard: " + shardType);
            Shard = shardType;
            EquipMessage = equipMessage;
        }
    }
}