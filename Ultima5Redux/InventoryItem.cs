using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public class InventoryItem
    {
        //public enum ItemClass { Helm, Shield, ChestPlate, Weapon, Ring, Amulet, Shard, Special, Reagents, Potions, Scrolls, Moonstone };

        public int Quantity { get; set; }

        public string LongName { get; }

        public string ShortName { get; }

        public InventoryItem(int quantity, string longName, string shortName)
        {
            this.Quantity = quantity;
            this.LongName = longName;
            this.ShortName = shortName;
        }

        public class LordBritishArtifact : InventoryItem
        {
            public enum ArtifactType { Amulet, Crown, Sceptre };

            public string EquipMessage { get; }

            public bool HasItem()
            {
                return Quantity != 0;
            }

            public ArtifactType Artifact { get; } 
            public LordBritishArtifact(ArtifactType artifact, int quantity, string longName, string equipMessage) : base (quantity, longName, longName)
            {
                Artifact = artifact;
                EquipMessage = equipMessage;
            }
        }

    }
}
