using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class ShadowlordShard : InventoryItem
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum ShardType
        {
            Falsehood = 0x210, Hatred = 0x211, Cowardice = 0x212
        }
        // private enum Offsets { FALSEHOOD = 0x210, HATRED = 0x211, COWARDICE = 0x212 }

        private const int SHARD_SPRITE = 436;

        [DataMember] public string EquipMessage =>
            Shard switch
            {
                ShardType.Falsehood => GetEquipStr(DataOvlReference.ShadowlordStrings.HATRED_DOT),
                ShardType.Hatred => GetEquipStr(DataOvlReference.ShadowlordStrings.HATRED_DOT),
                ShardType.Cowardice => GetEquipStr(DataOvlReference.ShadowlordStrings.COWARDICE_DOT),
                _ => throw new ArgumentOutOfRangeException()
            };

        [DataMember] public ShardType Shard { get; private set; }

        [IgnoreDataMember] public override string FindDescription => Shard.ToString();

        [IgnoreDataMember] public override bool HideQuantity => true;

        [IgnoreDataMember] public override string InventoryReferenceString => Shard.ToString();

        [JsonConstructor] private ShadowlordShard()
        {
        }

        public ShadowlordShard(ShardType shardType, int quantity) : base(quantity, SHARD_SPRITE,
            InventoryReferences.InventoryReferenceType.Item)
        {
            Debug.WriteLine("Shard: " + shardType);
            Shard = shardType;
        }

        private static string GetEquipStr(DataOvlReference.ShadowlordStrings shadowlordShard) =>
            GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                .GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
            GameReferences.DataOvlRef.StringReferences.GetString(shadowlordShard);
    }
}