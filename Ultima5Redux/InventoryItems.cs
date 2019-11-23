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
        public virtual List<InventoryItem> GenericItemList
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

        protected DataOvlReference dataOvlRef;
        protected List<byte> gameStateByteArray;

        public InventoryItems(DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
        {
            this.dataOvlRef = dataOvlRef;
            this.gameStateByteArray = gameStateByteArray;
        }
    }


    public class Armours : InventoryItems<Armours.ArmourTypeEnum, List<Armour>>
    {
        public List<Armour> Shields = new List<Armour>();
        public List<Armour> ChestArmours = new List<Armour>();

        public enum ArmourTypeEnum { Shield, Chest, Helm, Ring, Amulet }
        public Armours(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            InitializeShields();
            InitializeChestArmour();
        }

        // override to allow for inserting entire lists
        public override List<InventoryItem> GenericItemList
        {
            get
            {
                List<InventoryItem> itemList = new List<InventoryItem>();
                foreach (Shield shield in Shields) { itemList.Add(shield); }
                foreach (ChestArmour chestArmour in ChestArmours) { itemList.Add(chestArmour); }
                return itemList;
            }
        }

        private void InitializeChestArmour()
        {
            ChestArmours.Add(new ChestArmour(ChestArmour.ChestArmourEnum.ClothArmour,
                gameStateByteArray[(int)ChestArmour.ChestArmourEnum.ClothArmour],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.CLOTH_ARMOUR),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.CLOTH_ARMOUR),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.ClothArmour],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.ClothArmour]));
            ChestArmours.Add(new ChestArmour(ChestArmour.ChestArmourEnum.LeatherArmour,
                gameStateByteArray[(int)ChestArmour.ChestArmourEnum.LeatherArmour],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.LEATHER_ARMOUR),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.LEATHER_ARMOUR),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.LeatherArmour],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.LeatherArmour]));
            ChestArmours.Add(new ChestArmour(ChestArmour.ChestArmourEnum.Ringmail,
                gameStateByteArray[(int)ChestArmour.ChestArmourEnum.Ringmail],
                "Ring Mail", "Ring Mail",
                   //dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.),
                   //dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.CLOTH_ARMOUR),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.Ringmail],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.Ringmail]));
            ChestArmours.Add(new ChestArmour(ChestArmour.ChestArmourEnum.ScaleMail,
                gameStateByteArray[(int)ChestArmour.ChestArmourEnum.ScaleMail],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.SCALE_MAIL),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.SCALE_MAIL),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.ScaleMail],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.ScaleMail]));
            ChestArmours.Add(new ChestArmour(ChestArmour.ChestArmourEnum.ChainMail,
                gameStateByteArray[(int)ChestArmour.ChestArmourEnum.ChainMail],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.CHAIN_MAIL),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.CHAIN_MAIL),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.ChainMail],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.ChainMail]));
            ChestArmours.Add(new ChestArmour(ChestArmour.ChestArmourEnum.PlateMail,
                gameStateByteArray[(int)ChestArmour.ChestArmourEnum.PlateMail],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.PLATE_MAIL),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.PLATE_MAIL),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.PlateMail],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.PlateMail]));
            ChestArmours.Add(new ChestArmour(ChestArmour.ChestArmourEnum.MysticArmour,
                gameStateByteArray[(int)ChestArmour.ChestArmourEnum.MysticArmour],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.MYSTIC_ARMOUR),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.MYSTIC_ARMOUR),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.MysticArmour],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.MysticArmour]));

        }

        private void InitializeShields()
        {
            Shields.Add(new Shield(Shield.ShieldEnum.SmallShield,
                gameStateByteArray[(int)Shield.ShieldEnum.SmallShield],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.SMALL_SHIELD),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.SMALL_SHIELD),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.SmallShield],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.SmallShield]));
            Shields.Add(new Shield(Shield.ShieldEnum.LargeShield,
                gameStateByteArray[(int)Shield.ShieldEnum.LargeShield],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.LARGE_SHIELD),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.LARGE_SHIELD),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.LargeShield],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.LargeShield]));
            Shields.Add(new Shield(Shield.ShieldEnum.SpikedShield,
                gameStateByteArray[(int)Shield.ShieldEnum.SpikedShield],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.SPIKED_SHIELD),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.SPIKED_SHIELD),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.SpikedShield],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.SpikedShield]));
            Shields.Add(new Shield(Shield.ShieldEnum.MagicShield,
                gameStateByteArray[(int)Shield.ShieldEnum.MagicShield],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.MAGIC_SHIELD),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.MAGIC_SHIELD),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.MagicShield],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.MagicShield]));
            Shields.Add(new Shield(Shield.ShieldEnum.JewelShield,
                gameStateByteArray[(int)Shield.ShieldEnum.JewelShield],
                   dataOvlRef.StringReferences.GetString(DataOvlReference.LONG_ARMOUR_STRING.JEWEL_SHIELD),
                   dataOvlRef.StringReferences.GetString(DataOvlReference.SHORT_ARMOUR_STRING.JEWEL_SHIELD),
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.ATTACK_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.JewelShield],
                   dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.DEFENSE_VALUES).GetAsByteList()[(int)DataOvlReference.EQUIPMENT.JewelShield]));
        }

        public override Dictionary<ArmourTypeEnum, List<Armour>> Items => new Dictionary<ArmourTypeEnum, List<Armour>>();
    }


    public class SpecialItems : InventoryItems<SpecialItem.ItemTypeSpriteEnum, SpecialItem>
    {
        public override Dictionary<SpecialItem.ItemTypeSpriteEnum, SpecialItem> Items { get; } = new Dictionary<SpecialItem.ItemTypeSpriteEnum, SpecialItem>();

        public SpecialItems(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            //   Carpet = 170, Grapple = 12, Spyglass = 89, HMSCape = 260, PocketWatch = 232, BlackBadge = 281,
            //WoodenBox = 270, Sextant = 256
            Items[SpecialItem.ItemTypeSpriteEnum.Carpet] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Carpet,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Carpet],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES_STRINGS.MAGIC_CRPT),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES_STRINGS.MAGIC_CRPT));
            Items[SpecialItem.ItemTypeSpriteEnum.Grapple] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Grapple,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Grapple],
                "Grappling Hook",
                "Grapple");
            Items[SpecialItem.ItemTypeSpriteEnum.Spyglass] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Spyglass,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Spyglass],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.SPYGLASS),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.SPYGLASS));
            Items[SpecialItem.ItemTypeSpriteEnum.HMSCape] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.HMSCape,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.HMSCape],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.HMS_CAPE_PLAN),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.HMS_CAPE_PLAN));
            Items[SpecialItem.ItemTypeSpriteEnum.Sextant] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Sextant,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Sextant],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.SEXTANT),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.SEXTANT));
            Items[SpecialItem.ItemTypeSpriteEnum.PocketWatch] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.PocketWatch,
                //gameStateByteArray[(int)SpecialItem.ItemTypeEnum.PocketWatch],
                1,
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.POCKET_WATCH),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.POCKET_WATCH));
            Items[SpecialItem.ItemTypeSpriteEnum.BlackBadge] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.BlackBadge,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.BlackBadge],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.BLACK_BADGE),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.BLACK_BADGE));
            Items[SpecialItem.ItemTypeSpriteEnum.WoodenBox] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.WoodenBox,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.WoodenBox],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.WOODEN_BOX),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES2_STRINGS.WOODEN_BOX));

        }

    }

    public class Scrolls : InventoryItems<Spell.SpellWords, Scroll>
    {
        public override Dictionary<Spell.SpellWords, Scroll> Items { get; } = new Dictionary<Spell.SpellWords, Scroll>(8);
        
        // we apply an offset into the save game file by the number of spells since scrolls come immediatelly after
        // I wouldn't normally like to use offsets like this, but I want spells and scrolls to be linkable by the same enum
        private readonly int nQuantityIndexAdjust = Enum.GetValues(typeof(Spell.SpellWords)).Length;

        private void AddScroll(Spell.SpellWords spell, DataOvlReference.SPELL_STRINGS spellStr)
        {
            Scroll.ScrollSpells scrollSpell = (Scroll.ScrollSpells)Enum.Parse(typeof(Scroll.ScrollSpells), spell.ToString());

            int nIndex = 0x27A + (int)scrollSpell;
            Items[spell] = new Scroll(spell, gameStateByteArray[nIndex],
              dataOvlRef.StringReferences.GetString(spellStr),
              dataOvlRef.StringReferences.GetString(spellStr));
        }

        public Scrolls(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            AddScroll(Spell.SpellWords.Vas_Lor, DataOvlReference.SPELL_STRINGS.VAS_LOR);
            AddScroll(Spell.SpellWords.Rel_Hur, DataOvlReference.SPELL_STRINGS.REL_HUR);
            AddScroll(Spell.SpellWords.In_Sanct, DataOvlReference.SPELL_STRINGS.IN_SANCT);
            AddScroll(Spell.SpellWords.In_An, DataOvlReference.SPELL_STRINGS.IN_AN);
            AddScroll(Spell.SpellWords.In_Quas_Wis, DataOvlReference.SPELL_STRINGS.IN_QUAS_WIS);
            AddScroll(Spell.SpellWords.Kal_Xen_Corp, DataOvlReference.SPELL_STRINGS.KAL_XEN_CORP);
            AddScroll(Spell.SpellWords.In_Mani_Corp, DataOvlReference.SPELL_STRINGS.IN_MANI_CORP);
            AddScroll(Spell.SpellWords.An_Tym, DataOvlReference.SPELL_STRINGS.AN_TYM);
        }
    }

    public class Potions : InventoryItems<Potion.PotionColor, Potion>
    {
        public override Dictionary<Potion.PotionColor, Potion> Items { get; } = new Dictionary<Potion.PotionColor, Potion>(8);

        private void AddPotion(Potion.PotionColor color, DataOvlReference.POTIONS_STRINGS potStr)
        {
            Items[color] = new Potion(color, gameStateByteArray[(int)color],
                dataOvlRef.StringReferences.GetString(potStr),
                dataOvlRef.StringReferences.GetString(potStr));
        }

        public Potions(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base (dataOvlRef, gameStateByteArray)
        {
            AddPotion(Potion.PotionColor.Blue, DataOvlReference.POTIONS_STRINGS.BLUE);
            AddPotion(Potion.PotionColor.Yellow, DataOvlReference.POTIONS_STRINGS.YELLOW);
            AddPotion(Potion.PotionColor.Red, DataOvlReference.POTIONS_STRINGS.RED);
            AddPotion(Potion.PotionColor.Green, DataOvlReference.POTIONS_STRINGS.GREEN);
            AddPotion(Potion.PotionColor.Orange, DataOvlReference.POTIONS_STRINGS.ORANGE);
            AddPotion(Potion.PotionColor.Purple, DataOvlReference.POTIONS_STRINGS.PURPLE);
            AddPotion(Potion.PotionColor.Black, DataOvlReference.POTIONS_STRINGS.BLACK);
            AddPotion(Potion.PotionColor.White, DataOvlReference.POTIONS_STRINGS.WHITE);
        }
    }


    public class ShadowlordShards : InventoryItems <ShadowlordShard.ShardType, ShadowlordShard>
    {
        private enum OFFSETS { FALSEHOOD = 0x210, HATRED = 0x211, COWARDICE = 0x212 };

        public override Dictionary<ShadowlordShard.ShardType, ShadowlordShard> Items { get; } = new Dictionary<ShadowlordShard.ShardType, ShadowlordShard>(3);

        public ShadowlordShards(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base (dataOvlRef, gameStateByteArray)
        {
            Items[ShadowlordShard.ShardType.Falsehood] = new ShadowlordShard(ShadowlordShard.ShardType.Falsehood,
                gameStateByteArray[(int)OFFSETS.FALSEHOOD],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHARDS_STRINGS.FALSEHOOD),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.GEM_SHARD_THOU_HOLD_EVIL_SHARD)+
                dataOvlRef.StringReferences.GetString(DataOvlReference.SHADOWLORD_STRINGS.FALSEHOOD_DOT));
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

        public LordBritishArtifacts(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
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
