using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public abstract class InventoryItems
    {

    }

    public class ShadowlordShards : InventoryItems
    {
        private enum OFFSETS { FALSEHOOD = 0x210, HATRED = 0x211, COWARDICE = 0x212 };

        public Dictionary<ShadowlordShard.ShardType, ShadowlordShard> Shards =
            new Dictionary<ShadowlordShard.ShardType, ShadowlordShard>(3);

        public ShadowlordShards(DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
        {
            Shards.Add(ShadowlordShard.ShardType.Falsehood, new ShadowlordShard(ShadowlordShard.ShardType.Falsehood,
                gameStateByteArray[(int)OFFSETS.FALSEHOOD],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHARDS_STRINGS.FALSEHOOD),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.GEM_SHARD_THOU_HOLD_EVIL_SHARD)+
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.FALSEHOOD_DOT)));
            Shards[ShadowlordShard.ShardType.Hatred] = new ShadowlordShard(ShadowlordShard.ShardType.Hatred,
                gameStateByteArray[(int)OFFSETS.HATRED],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHARDS_STRINGS.HATRED),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.HATRED_DOT));
            Shards[ShadowlordShard.ShardType.Cowardice] = new ShadowlordShard(ShadowlordShard.ShardType.Cowardice,
                gameStateByteArray[(int)OFFSETS.COWARDICE],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHARDS_STRINGS.COWARDICE),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.COWARDICE_DOT));
        }
    }


    public class LordBritishArtifacts : InventoryItems
    {
        private enum OFFSETS { AMULET = 0x20D, CROWN = 0x20E, SCEPTRE = 0x20F};

        public Dictionary<LordBritishArtifact.ArtifactType, LordBritishArtifact> Artifacts = 
            new Dictionary<LordBritishArtifact.ArtifactType, LordBritishArtifact>(3);
        public LordBritishArtifacts(DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
        {
            Artifacts[LordBritishArtifact.ArtifactType.Amulet] = new LordBritishArtifact(LordBritishArtifact.ArtifactType.Amulet,
                gameStateByteArray[(int)OFFSETS.AMULET],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES_STRINGS.AMULET),
                dataOvlRef.StringReferences.GetString(DataOvlReference.WEAR_USE_ITEM_STRINGS.WEARING_AMULET));
            Artifacts[LordBritishArtifact.ArtifactType.Crown] = new LordBritishArtifact(LordBritishArtifact.ArtifactType.Crown,
                gameStateByteArray[(int)OFFSETS.CROWN],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES_STRINGS.CROWN),
                dataOvlRef.StringReferences.GetString(DataOvlReference.WEAR_USE_ITEM_STRINGS.DON_THE_CROWN));
            Artifacts[LordBritishArtifact.ArtifactType.Sceptre] = new LordBritishArtifact(LordBritishArtifact.ArtifactType.Sceptre,
                gameStateByteArray[(int)OFFSETS.SCEPTRE],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES_STRINGS.SCEPTRE),
                dataOvlRef.StringReferences.GetString(DataOvlReference.WEAR_USE_ITEM_STRINGS.WIELD_SCEPTRE));
        }
    }

}
