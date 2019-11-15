using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public abstract class InventoryItems <EnumType, T>
    {
        public abstract Dictionary<EnumType, T> Items { get; }
        public List<InventoryItem> GenericItemList
        {
            get
            {
                List<InventoryItem> itemList = new List<InventoryItem>();
                foreach (object item in Items.Values)
                {
                    itemList.Add((InventoryItem)item);
                }
                return itemList;
            }
        }
    }

    public class ShadowlordShards : InventoryItems <ShadowlordShard.ShardType, ShadowlordShard>
    {
        private enum OFFSETS { FALSEHOOD = 0x210, HATRED = 0x211, COWARDICE = 0x212 };

        public override Dictionary<ShadowlordShard.ShardType, ShadowlordShard> Items { get; } = new Dictionary<ShadowlordShard.ShardType, ShadowlordShard>(3);

        public ShadowlordShards(DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
        {
            Items.Add(ShadowlordShard.ShardType.Falsehood, new ShadowlordShard(ShadowlordShard.ShardType.Falsehood,
                gameStateByteArray[(int)OFFSETS.FALSEHOOD],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHARDS_STRINGS.FALSEHOOD),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.GEM_SHARD_THOU_HOLD_EVIL_SHARD)+
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.FALSEHOOD_DOT)));
            Items[ShadowlordShard.ShardType.Hatred] = new ShadowlordShard(ShadowlordShard.ShardType.Hatred,
                gameStateByteArray[(int)OFFSETS.HATRED],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHARDS_STRINGS.HATRED),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.HATRED_DOT));
            Items[ShadowlordShard.ShardType.Cowardice] = new ShadowlordShard(ShadowlordShard.ShardType.Cowardice,
                gameStateByteArray[(int)OFFSETS.COWARDICE],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHARDS_STRINGS.COWARDICE),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.COWARDICE_DOT));
        }
    }


    public class LordBritishArtifacts : InventoryItems <LordBritishArtifact.ArtifactType, LordBritishArtifact>
    {
        private enum OFFSETS { AMULET = 0x20D, CROWN = 0x20E, SCEPTRE = 0x20F};

        public override Dictionary<LordBritishArtifact.ArtifactType, LordBritishArtifact> Items { get; } =
            new Dictionary<LordBritishArtifact.ArtifactType, LordBritishArtifact>(3);

        public LordBritishArtifacts(DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
        {
            Items[LordBritishArtifact.ArtifactType.Amulet] = new LordBritishArtifact(LordBritishArtifact.ArtifactType.Amulet,
                gameStateByteArray[(int)OFFSETS.AMULET],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES_STRINGS.AMULET),
                dataOvlRef.StringReferences.GetString(DataOvlReference.WEAR_USE_ITEM_STRINGS.WEARING_AMULET));
            Items[LordBritishArtifact.ArtifactType.Crown] = new LordBritishArtifact(LordBritishArtifact.ArtifactType.Crown,
                gameStateByteArray[(int)OFFSETS.CROWN],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES_STRINGS.CROWN),
                dataOvlRef.StringReferences.GetString(DataOvlReference.WEAR_USE_ITEM_STRINGS.DON_THE_CROWN));
            Items[LordBritishArtifact.ArtifactType.Sceptre] = new LordBritishArtifact(LordBritishArtifact.ArtifactType.Sceptre,
                gameStateByteArray[(int)OFFSETS.SCEPTRE],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES_STRINGS.SCEPTRE),
                dataOvlRef.StringReferences.GetString(DataOvlReference.WEAR_USE_ITEM_STRINGS.WIELD_SCEPTRE));
        }
    }

}
