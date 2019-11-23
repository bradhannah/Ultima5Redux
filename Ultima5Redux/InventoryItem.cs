using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Ultima5Redux
{
    public abstract class InventoryItem
    {
       
        public abstract bool HideQuantity { get; }
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

    public class SpecialItem : InventoryItem
    {
        public override bool HideQuantity
        {
            get
            {
                if (ItemType == ItemTypeSpriteEnum.Carpet) return false;
                return true;
            }
        }

        public enum ItemTypeEnum
        {
            Carpet = 0x20A, Grapple = 0x209, Spyglass = 0x214, HMSCape = 0x215, PocketWatch = 0, BlackBadge = 0x218,
            WoodenBox = 0x219, Sextant = 0x216
        };

        public enum ItemTypeSpriteEnum
        {
            Carpet = 170, Grapple = 12, Spyglass = 89, HMSCape = 260, PocketWatch = 232, BlackBadge = 281,
            WoodenBox = 270, Sextant = 256
        }

        public ItemTypeSpriteEnum ItemType { get; }

        public static ItemTypeEnum GetItemOffset(ItemTypeSpriteEnum itemTypeSpriteEnum)
        {
            ItemTypeEnum ItemType = (ItemTypeEnum)Enum.Parse(typeof(ItemTypeEnum), itemTypeSpriteEnum.ToString());
            return ItemType;
        }

        public SpecialItem(ItemTypeSpriteEnum itemType, int quantity, string longName, string shortName) : 
            base(quantity, longName, shortName, (int)itemType)
        {
            ItemType = itemType;
        }
    }

    public class ChestArmour : Armour
    {
        public enum ChestArmourEnum { ClothArmour, LeatherArmour, Ringmail, ScaleMail, ChainMail, PlateMail, MysticArmour }

        public ChestArmourEnum ChestArmourType;

        private const int CHEST_ARMOUR_SPRITE = 267;

        public override int AttackStat { get; }

        public override int DefendStat { get; }

        public override bool HideQuantity => false;

        public ChestArmour(ChestArmourEnum chestArmourType, int quantity, string longName, string shortName, int attackStat, int defendStat) : 
            base(quantity, longName, shortName, CHEST_ARMOUR_SPRITE, attackStat, defendStat)
        {
            ChestArmourType = chestArmourType;
        }
    }


    public class Shield : Armour
    {
        public enum ShieldEnum { SmallShield = 0x21E, LargeShield = 0x21F,  SpikedShield = 0x220, MagicShield = 0x221, JewelShield = 0x222 }
        
        private const int SHIELD_SPRITE = 262;
        public Shield(ShieldEnum shieldType, int quantity, string longName, string shortName, int attackStat, int defendStat) : 
            base(quantity, longName, shortName, SHIELD_SPRITE, attackStat, defendStat)
        {
            ShieldType = shieldType;
        }

        public ShieldEnum ShieldType { get; }
        public override int AttackStat { get; }

        public override int DefendStat { get; }

        public override bool HideQuantity => false;
    }

    abstract public class Weapon : CombatItem
    {
        public Weapon(int quantity, string longName, string shortName, int nSpriteNum, int attackStat, int defendStat) : 
            base(quantity, longName, shortName, nSpriteNum, attackStat, defendStat)
        {
        }
    }

    abstract public class Armour : CombatItem
    {
        public Armour(int quantity, string longName, string shortName, int nSpriteNum, int attackStat, int defendStat) : 
            base(quantity, longName, shortName, nSpriteNum, attackStat, defendStat)
        {
        }
    }

    abstract public class CombatItem : InventoryItem
    {
        abstract public int AttackStat { get; }
        abstract public int DefendStat { get; }
        public CombatItem(int quantity, string longName, string shortName, int nSpriteNum, int attackStat, int defendStat) : base(quantity, longName, shortName, nSpriteNum)
        {
            attackStat = AttackStat;
            defendStat = DefendStat;
        }
    }

    public class Potion : InventoryItem
    {
        public enum PotionColor { Blue = 0x282, Yellow = 0x283, Red = 0x284, Green = 0x285, Orange = 0x286, Purple = 0x287, Black = 0x288, White = 0x289 }

        public PotionColor Color { get; }

        private const int POTION_SPRITE = 259;
        
        public override bool HideQuantity { get; } = false;

        public Potion(PotionColor color, int quantity, string longName, string shortName) : base(quantity, longName, shortName, POTION_SPRITE)
        {
            Color = color;
        }
    }

    public class Spell : InventoryItem
    {
        public override bool HideQuantity { get; } = false;

        public enum SpellWords
        {
            // taking a bit of a risk and just let the subsequent values be assigned since they should be in order
            In_Lor = 0x24A, Grav_Por, An_Zu, An_Nox, Mani, An_Ylem, An_Sanct, An_Xen_Corp, Rel_Hur, In_Wis, Kal_Xen,
            In_Xen_Mani, Vas_Lor, Vas_Flam, In_Flam_Grav, In_Nox_Grav, In_Zu_Grav, In_Por, An_Grav, In_Sanct, In_Sanct_Grav, Uus_Por,
            Des_Por, Wis_Quas, In_Bet_Xen, An_Ex_Por, In_Ex_Por, Vas_Mani, In_Zu, Rel_Tym, In_Vas_Por_Ylem, Quas_An_Wis, In_An,
            Wis_An_Ylem, An_Xen_Ex, Rel_Xen_Bet, Sanct_Lor, Xen_Corp, In_Quas_Xen, In_Quas_Wis, In_Nox_Hur, In_Quas_Corp, In_Mani_Corp,
            Kal_Xen_Corp, In_Vas_Grav_Corp, In_Flam_Hur, Vas_Rel_Por, An_Tym
        }
    
        public Spell(int quantity, string longName, string shortName, int spriteNum) : base(quantity, longName, shortName, spriteNum)
        {
        }

    }


    public class Scroll : InventoryItem
    {
        public enum ScrollSpells { Vas_Lor = 0 , Rel_Hur, In_Sanct, In_An, In_Quas_Wis, Kal_Xen_Corp, In_Mani_Corp, An_Tym }
        
        private const int SCROLL_SPRITE = 260;

        public Spell.SpellWords ScrollSpell { get; }

        public override bool HideQuantity { get; } = false;

        public Scroll(Spell.SpellWords spell, int quantity, string longName, string shortName) : base(quantity, longName, shortName, SCROLL_SPRITE)
        {
            this.ScrollSpell = spell;
        }
    }

    public class ShadowlordShard : InventoryItem
    {
        public enum ShardType { Falsehood, Hatred, Cowardice};

        public ShardType Shard { get; }
        public string EquipMessage { get; }

        private const int SHARD_SPRITE = 436;
        public override bool HideQuantity { get; } = true;

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

        public override bool HideQuantity { get; } = true;

        public ArtifactType Artifact { get; }
        
        public LordBritishArtifact(ArtifactType artifact, int quantity, string longName, string equipMessage) : base(quantity, longName, longName, (int)artifact)
        {
            Artifact = artifact;
            EquipMessage = equipMessage;
        }
    }
}
