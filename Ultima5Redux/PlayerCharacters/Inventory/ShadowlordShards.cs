using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class ShadowlordShards : InventoryItems<ShadowlordShard.ShardType, ShadowlordShard>
    {
        [DataMember]
        public override Dictionary<ShadowlordShard.ShardType, ShadowlordShard> Items { get; internal set; } =
            new(3);

        [JsonConstructor] private ShadowlordShards()
        {
        }

        public ShadowlordShards(ImportedGameState importedGameState)
        {
            void addShardLegacy(ShadowlordShard.ShardType shard) =>
                Items.Add(shard, new ShadowlordShard(shard, importedGameState.GetShadowlordShardQuantity(shard)));

            addShardLegacy(ShadowlordShard.ShardType.Falsehood);
            addShardLegacy(ShadowlordShard.ShardType.Hatred);
            addShardLegacy(ShadowlordShard.ShardType.Cowardice);

            // Items[ShadowlordShard.ShardType.Falsehood] = new ShadowlordShard(ShadowlordShard.ShardType.Falsehood,
            //     gameStateByteArray[(int)Offsets.FALSEHOOD]);
            // Items[ShadowlordShard.ShardType.Hatred] = new ShadowlordShard(ShadowlordShard.ShardType.Hatred,
            //     gameStateByteArray[(int)Offsets.HATRED]);
            // Items[ShadowlordShard.ShardType.Cowardice] = new ShadowlordShard(ShadowlordShard.ShardType.Cowardice,
            //     gameStateByteArray[(int)Offsets.COWARDICE]);
        }
    }
}