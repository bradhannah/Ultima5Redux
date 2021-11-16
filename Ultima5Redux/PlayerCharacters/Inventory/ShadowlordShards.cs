using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.References;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public sealed class ShadowlordShards : InventoryItems<ShadowlordShard.ShardType, ShadowlordShard>
    {
        public ShadowlordShards(List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
            Items[ShadowlordShard.ShardType.Falsehood] = new ShadowlordShard(ShadowlordShard.ShardType.Falsehood,
                gameStateByteArray[(int)Offsets.FALSEHOOD],
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                    .GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings.FALSEHOOD_DOT));
            Items[ShadowlordShard.ShardType.Hatred] = new ShadowlordShard(ShadowlordShard.ShardType.Hatred,
                gameStateByteArray[(int)Offsets.HATRED],
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                    .GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings.HATRED_DOT));
            Items[ShadowlordShard.ShardType.Cowardice] = new ShadowlordShard(ShadowlordShard.ShardType.Cowardice,
                gameStateByteArray[(int)Offsets.COWARDICE],
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                    .GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings.COWARDICE_DOT));
        }

        public override Dictionary<ShadowlordShard.ShardType, ShadowlordShard> Items { get; } =
            new Dictionary<ShadowlordShard.ShardType, ShadowlordShard>(3);

        private enum Offsets { FALSEHOOD = 0x210, HATRED = 0x211, COWARDICE = 0x212 }
    }
}