using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class ShadowlordShards : InventoryItems<ShadowlordShard.ShardType, ShadowlordShard>
    {
        private enum Offsets { FALSEHOOD = 0x210, HATRED = 0x211, COWARDICE = 0x212 }

        [DataMember] public override Dictionary<ShadowlordShard.ShardType, ShadowlordShard> Items { get; } =
            new Dictionary<ShadowlordShard.ShardType, ShadowlordShard>(3);

        [JsonConstructor] private ShadowlordShards()
        {
        }
        
        public ShadowlordShards(List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
            Items[ShadowlordShard.ShardType.Falsehood] = new ShadowlordShard(ShadowlordShard.ShardType.Falsehood,
                gameStateByteArray[(int)Offsets.FALSEHOOD]);
            Items[ShadowlordShard.ShardType.Hatred] = new ShadowlordShard(ShadowlordShard.ShardType.Hatred,
                gameStateByteArray[(int)Offsets.HATRED]);
            Items[ShadowlordShard.ShardType.Cowardice] = new ShadowlordShard(ShadowlordShard.ShardType.Cowardice,
                gameStateByteArray[(int)Offsets.COWARDICE]);
        }
    }
}