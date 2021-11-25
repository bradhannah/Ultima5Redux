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
        [JsonConverter(typeof(StringEnumConverter))] public enum ShardType { Falsehood, Hatred, Cowardice }

        private const int SHARD_SPRITE = 436;

        [IgnoreDataMember] public override string FindDescription => Shard.ToString();

        [IgnoreDataMember] public override bool HideQuantity => true;

        [IgnoreDataMember] public override string InventoryReferenceString => Shard.ToString();

        [DataMember] public string EquipMessage =>
            Shard switch
            {
                ShardType.Falsehood => GetEquipStr(DataOvlReference.ShadowlordStrings.HATRED_DOT),
                ShardType.Hatred => GetEquipStr(DataOvlReference.ShadowlordStrings.HATRED_DOT),
                ShardType.Cowardice => GetEquipStr(DataOvlReference.ShadowlordStrings.COWARDICE_DOT),
                _ => throw new ArgumentOutOfRangeException()
            };

        private static string GetEquipStr(DataOvlReference.ShadowlordStrings shadowlordShard) =>
            GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.ShadowlordStrings
                .GEM_SHARD_THOU_HOLD_EVIL_SHARD) +
            GameReferences.DataOvlRef.StringReferences.GetString(shadowlordShard);

        [DataMember] public ShardType Shard { get; private set; }

        [JsonConstructor] private ShadowlordShard()
        {
        }

        public ShadowlordShard(ShardType shardType, int quantity) : base(quantity, SHARD_SPRITE,
            InventoryReferences.InventoryReferenceType.Item)
        {
            Debug.WriteLine("Shard: " + shardType);
            Shard = shardType;
        }
    }
}