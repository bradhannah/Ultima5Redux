using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters
{
    public class ShadowlordShards : InventoryItems <ShadowlordShard.ShardType, ShadowlordShard>
    {
        private enum Offsets { FALSEHOOD = 0x210, HATRED = 0x211, COWARDICE = 0x212 };

        public override Dictionary<ShadowlordShard.ShardType, ShadowlordShard> Items { get; } = new Dictionary<ShadowlordShard.ShardType, ShadowlordShard>(3);

        public ShadowlordShards(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base (dataOvlRef, gameStateByteArray)
        {
            Items[ShadowlordShard.ShardType.Falsehood] = new ShadowlordShard(ShadowlordShard.ShardType.Falsehood,
                gameStateByteArray[(int)Offsets.FALSEHOOD],
                dataOvlRef.StringReferences.GetString(DataOvlReference.ShardsStrings.FALSEHOOD),
                dataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings.GEM_SHARD_THOU_HOLD_EVIL_SHARD)+
                dataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings.FALSEHOOD_DOT));
            Items[ShadowlordShard.ShardType.Hatred] = new ShadowlordShard(ShadowlordShard.ShardType.Hatred,
                gameStateByteArray[(int)Offsets.HATRED],
                dataOvlRef.StringReferences.GetString(DataOvlReference.ShardsStrings.HATRED),
                dataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings.GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
                dataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings.HATRED_DOT));
            Items[ShadowlordShard.ShardType.Cowardice] = new ShadowlordShard(ShadowlordShard.ShardType.Cowardice,
                gameStateByteArray[(int)Offsets.COWARDICE],
                dataOvlRef.StringReferences.GetString(DataOvlReference.ShardsStrings.COWARDICE),
                dataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings.GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
                dataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings.COWARDICE_DOT));
        }
    }
}