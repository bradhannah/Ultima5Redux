using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class InventoryItem
    {
        //public enum ItemClass { Helm, Shield, ChestPlate, Weapon, Ring, Amulet, Shard, Special, Reagents, Potions, Scrolls, Moonstone };

        public int Quantity { get; set; }

        public string LongName { get; }

        public string ShortName { get; }

        public int SpriteNum { get; }

        public InventoryItem(int quantity, string longName, string shortName, int spriteNum)
        {
            this.Quantity = quantity;
            this.LongName = longName;
            this.ShortName = shortName;
            this.SpriteNum = spriteNum;
        }
    }

    public class ShadowlordShard : InventoryItem
    {
        public enum ShardType { Falsehood, Hatred, Cowardice};

        public ShardType Shard { get; }
        public string EquipMessage { get; }

        private const int SHARD_SPRITE = 436;
        public ShadowlordShard(ShardType shardType, int quantity, string longName, string equipMessage) : base(quantity, longName, longName, SHARD_SPRITE)
        {
            Debug.WriteLine("Shard: " + shardType.ToString());
            Shard = shardType;
            EquipMessage = equipMessage;
        }
    }


    public class LordBritishArtifact : InventoryItem
    {
        public enum ArtifactType { Amulet = 439, Crown = 437, Sceptre = 438};

        public string EquipMessage { get; }

        public bool HasItem()
        {
            return Quantity != 0;
        }

        public ArtifactType Artifact { get; }
        
        public LordBritishArtifact(ArtifactType artifact, int quantity, string longName, string equipMessage) : base(quantity, longName, longName, (int)artifact)
        {
            Artifact = artifact;
            EquipMessage = equipMessage;
        }
    }
}
