using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public abstract class InventoryItems <TEnumType, T>
    {
        public abstract Dictionary<TEnumType, T> Items { get; }

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

        protected DataOvlReference DataOvlRef;
        protected List<byte> GameStateByteArray;

        public InventoryItems(DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
        {
            this.DataOvlRef = dataOvlRef;
            this.GameStateByteArray = gameStateByteArray;
        }
    }

    public class Weapons : InventoryItems<Weapon.WeaponTypeEnum, Weapon>
    {
        public override Dictionary<Weapon.WeaponTypeEnum, Weapon> Items { get; } = new Dictionary<Weapon.WeaponTypeEnum, Weapon>();
        private List<string> _equipmentNames;

        private Dictionary<DataOvlReference.Equipment, Weapon> ItemsFromEquipment { get; } = new Dictionary<DataOvlReference.Equipment, Weapon>();
        public Weapon GetWeaponFromEquipment(DataOvlReference.Equipment equipment)
        {
            if (equipment == DataOvlReference.Equipment.Nothing)
            {
                return null;
            }
            if (!ItemsFromEquipment.ContainsKey(equipment))
            {
                return null;
            }
            return ItemsFromEquipment[equipment];
        }

        public Weapons(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            _equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES).GetAsStringListFromIndexes();

            // we need to actually add shields because they can be equipped as weapons
            // but you should not expose shields twice in the UI
            AddWeapon(Weapon.WeaponTypeEnum.SmallShield, Weapon.WeaponTypeSpriteEnum.SmallShield, DataOvlReference.Equipment.SmallShield);
            AddWeapon(Weapon.WeaponTypeEnum.LargeShield, Weapon.WeaponTypeSpriteEnum.LargeShield, DataOvlReference.Equipment.LargeShield);
            AddWeapon(Weapon.WeaponTypeEnum.SpikedShield, Weapon.WeaponTypeSpriteEnum.SpikedShield, DataOvlReference.Equipment.SpikedShield);
            AddWeapon(Weapon.WeaponTypeEnum.MagicShield, Weapon.WeaponTypeSpriteEnum.MagicShield, DataOvlReference.Equipment.MagicShield);
            AddWeapon(Weapon.WeaponTypeEnum.JewelShield, Weapon.WeaponTypeSpriteEnum.JewelShield, DataOvlReference.Equipment.JewelShield);

            AddWeapon(Weapon.WeaponTypeEnum.Dagger, Weapon.WeaponTypeSpriteEnum.Dagger, DataOvlReference.Equipment.Dagger);
            AddWeapon(Weapon.WeaponTypeEnum.Sling, Weapon.WeaponTypeSpriteEnum.Sling, DataOvlReference.Equipment.Sling);
            AddWeapon(Weapon.WeaponTypeEnum.Club, Weapon.WeaponTypeSpriteEnum.Club, DataOvlReference.Equipment.Club);
            AddWeapon(Weapon.WeaponTypeEnum.FlamingOil, Weapon.WeaponTypeSpriteEnum.FlamingOil, DataOvlReference.Equipment.FlamingOil);
            AddWeapon(Weapon.WeaponTypeEnum.MainGauche, Weapon.WeaponTypeSpriteEnum.MainGauche, DataOvlReference.Equipment.MainGauche);
            AddWeapon(Weapon.WeaponTypeEnum.Spear, Weapon.WeaponTypeSpriteEnum.Spear, DataOvlReference.Equipment.Spear);
            AddWeapon(Weapon.WeaponTypeEnum.ThrowingAxe, Weapon.WeaponTypeSpriteEnum.ThrowingAxe, DataOvlReference.Equipment.ThrowingAxe);
            AddWeapon(Weapon.WeaponTypeEnum.ShortSword, Weapon.WeaponTypeSpriteEnum.ShortSword, DataOvlReference.Equipment.ShortSword);
            AddWeapon(Weapon.WeaponTypeEnum.Mace, Weapon.WeaponTypeSpriteEnum.Mace, DataOvlReference.Equipment.Mace);
            AddWeapon(Weapon.WeaponTypeEnum.MorningStar, Weapon.WeaponTypeSpriteEnum.MorningStar, DataOvlReference.Equipment.MorningStar);
            AddWeapon(Weapon.WeaponTypeEnum.Bow, Weapon.WeaponTypeSpriteEnum.Bow, DataOvlReference.Equipment.Bow);
            AddWeapon(Weapon.WeaponTypeEnum.Arrows, Weapon.WeaponTypeSpriteEnum.Arrows, DataOvlReference.Equipment.Arrows);
            AddWeapon(Weapon.WeaponTypeEnum.Crossbow, Weapon.WeaponTypeSpriteEnum.Crossbow, DataOvlReference.Equipment.Crossbow);
            AddWeapon(Weapon.WeaponTypeEnum.Quarrels, Weapon.WeaponTypeSpriteEnum.Quarrels, DataOvlReference.Equipment.Quarrels);
            AddWeapon(Weapon.WeaponTypeEnum.LongSword, Weapon.WeaponTypeSpriteEnum.LongSword, DataOvlReference.Equipment.LongSword);
            AddWeapon(Weapon.WeaponTypeEnum.TwoHHammer, Weapon.WeaponTypeSpriteEnum.TwoHHammer, DataOvlReference.Equipment.TwoHHammer);
            AddWeapon(Weapon.WeaponTypeEnum.TwoHAxe, Weapon.WeaponTypeSpriteEnum.TwoHAxe, DataOvlReference.Equipment.TwoHAxe);
            AddWeapon(Weapon.WeaponTypeEnum.TwoHSword, Weapon.WeaponTypeSpriteEnum.TwoHSword, DataOvlReference.Equipment.TwoHSword);
            AddWeapon(Weapon.WeaponTypeEnum.Halberd, Weapon.WeaponTypeSpriteEnum.Halberd, DataOvlReference.Equipment.Halberd);
            AddWeapon(Weapon.WeaponTypeEnum.SwordofChaos, Weapon.WeaponTypeSpriteEnum.SwordofChaos, DataOvlReference.Equipment.SwordofChaos);
            AddWeapon(Weapon.WeaponTypeEnum.MagicBow, Weapon.WeaponTypeSpriteEnum.MagicBow, DataOvlReference.Equipment.MagicBow);
            AddWeapon(Weapon.WeaponTypeEnum.SilverSword, Weapon.WeaponTypeSpriteEnum.SilverSword, DataOvlReference.Equipment.SilverSword);
            AddWeapon(Weapon.WeaponTypeEnum.MagicAxe, Weapon.WeaponTypeSpriteEnum.MagicAxe, DataOvlReference.Equipment.MagicAxe);
            AddWeapon(Weapon.WeaponTypeEnum.GlassSword, Weapon.WeaponTypeSpriteEnum.GlassSword, DataOvlReference.Equipment.GlassSword);
            AddWeapon(Weapon.WeaponTypeEnum.JeweledSword, Weapon.WeaponTypeSpriteEnum.JeweledSword, DataOvlReference.Equipment.JeweledSword);
            AddWeapon(Weapon.WeaponTypeEnum.MysticSword, Weapon.WeaponTypeSpriteEnum.MysticSword, DataOvlReference.Equipment.MysticSword);
        }

        private void AddWeapon(Weapon.WeaponTypeEnum weapon, Weapon.WeaponTypeSpriteEnum weaponSprite, DataOvlReference.Equipment equipment)
        {
            Weapon newWeapon = new Weapon(weapon, weaponSprite, equipment, DataOvlRef, GameStateByteArray);
            Items.Add(weapon, newWeapon);
            ItemsFromEquipment.Add(equipment, newWeapon);
        }
    }

    public class Armours : InventoryItems<Armours.ArmourTypeEnum, List<Armour>>
    {
        //public List<Shield> Shields = new List<Shield>();
        public List<ChestArmour> ChestArmours = new List<ChestArmour>();
        public List<Helm> Helms = new List<Helm>();
        public List<Amulet> Amulets = new List<Amulet>();
        public List<Ring> Rings = new List<Ring>();
      
        private List<string> _equipmentNames;

        private Dictionary<DataOvlReference.Equipment, Armour> ItemsFromEquipment { get; } = new Dictionary<DataOvlReference.Equipment, Armour>();
        public Armour GetArmourFromEquipment(DataOvlReference.Equipment equipment)
        {
            if (equipment == DataOvlReference.Equipment.Nothing)
            {
                return null;
            }
            if (!ItemsFromEquipment.ContainsKey(equipment))
            {
                return null;
            }
            return ItemsFromEquipment[equipment];
        }

        public enum ArmourTypeEnum { Shield, Chest, Helm, Ring, Amulet }
        public Armours(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            _equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES).GetAsStringListFromIndexes();

            InitializeHelms();
            //InitializeShields();
            InitializeChestArmour();
            InitializeAmulets();
            InitializeRings();
        }

        // override to allow for inserting entire lists
        public override List<InventoryItem> GenericItemList
        {
            get
            {
                List<InventoryItem> itemList = new List<InventoryItem>();
                foreach (Helm helm in Helms) { itemList.Add(helm); }
                //foreach (Shield shield in Shields) { itemList.Add(shield); }
                foreach (ChestArmour chestArmour in ChestArmours) { itemList.Add(chestArmour); }
                foreach (Amulet amulet in Amulets) { itemList.Add(amulet); }
                foreach (Ring ring in Rings) { itemList.Add(ring); }
                return itemList;
            }
        }

        private void AddChestArmour(ChestArmour.ChestArmourEnum chestArmour, DataOvlReference.Equipment equipment)
        {
            ChestArmour armour = new ChestArmour(chestArmour, equipment, DataOvlRef, GameStateByteArray);
            ChestArmours.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        //private void AddShield(Shield.ShieldTypeEnum shield, DataOvlReference.EQUIPMENT equipment)
        //{
        //    Shields.Add(new Shield(shield, equipment, dataOvlRef, gameStateByteArray));
        //}

        private void AddHelm(Helm.HelmEnum helm, DataOvlReference.Equipment equipment)
        {
            Helm armour = new Helm(helm, equipment, DataOvlRef, GameStateByteArray);
            Helms.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        private void AddAmulet(Amulet.AmuletEnum amulet, DataOvlReference.Equipment equipment)
        {
            Amulet armour = new Amulet(amulet, equipment, DataOvlRef, GameStateByteArray);
            Amulets.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        private void AddRing(Ring.RingEnum ring, DataOvlReference.Equipment equipment)
        {
            Ring armour = new Ring(ring, equipment, DataOvlRef, GameStateByteArray);
            Rings.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        private void InitializeRings()
        {
            AddRing(Ring.RingEnum.RingInvisibility, DataOvlReference.Equipment.RingInvis);
            AddRing(Ring.RingEnum.RingProtection, DataOvlReference.Equipment.RingProtection);
            AddRing(Ring.RingEnum.RingRegeneration, DataOvlReference.Equipment.RingRegen);
        }

        private void InitializeAmulets()
        {
            AddAmulet(Amulet.AmuletEnum.AmuletTurning, DataOvlReference.Equipment.Amuletofturning);
            AddAmulet(Amulet.AmuletEnum.SpikeCollar, DataOvlReference.Equipment.SpikedCollar);
            AddAmulet(Amulet.AmuletEnum.Ankh, DataOvlReference.Equipment.Ankh);
        }

        private void InitializeHelms() 
        {
            AddHelm(Helm.HelmEnum.LeatherHelm, DataOvlReference.Equipment.LeatherHelm);
            AddHelm(Helm.HelmEnum.ChainCoif, DataOvlReference.Equipment.ChainCoif);
            AddHelm(Helm.HelmEnum.IronHelm, DataOvlReference.Equipment.IronHelm);
            AddHelm(Helm.HelmEnum.SpikedHelm, DataOvlReference.Equipment.SpikedHelm);
        }

        private void InitializeChestArmour()
        {
            AddChestArmour(ChestArmour.ChestArmourEnum.ClothArmour, DataOvlReference.Equipment.ClothArmour);
            AddChestArmour(ChestArmour.ChestArmourEnum.Ringmail, DataOvlReference.Equipment.Ringmail);
            AddChestArmour(ChestArmour.ChestArmourEnum.ScaleMail, DataOvlReference.Equipment.ScaleMail);
            AddChestArmour(ChestArmour.ChestArmourEnum.ChainMail, DataOvlReference.Equipment.ChainMail);
            AddChestArmour(ChestArmour.ChestArmourEnum.PlateMail, DataOvlReference.Equipment.PlateMail);
            AddChestArmour(ChestArmour.ChestArmourEnum.MysticArmour, DataOvlReference.Equipment.MysticArmour);
        }

        //private void InitializeShields()
        //{
        //    AddShield(Shield.ShieldTypeEnum.SmallShield, DataOvlReference.EQUIPMENT.SmallShield);
        //    AddShield(Shield.ShieldTypeEnum.LargeShield, DataOvlReference.EQUIPMENT.LargeShield);
        //    AddShield(Shield.ShieldTypeEnum.SpikedShield, DataOvlReference.EQUIPMENT.SpikedShield);
        //    AddShield(Shield.ShieldTypeEnum.MagicShield, DataOvlReference.EQUIPMENT.MagicShield);
        //    AddShield(Shield.ShieldTypeEnum.JewelShield, DataOvlReference.EQUIPMENT.JewelShield);
        //}

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
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNamesStrings.MAGIC_CRPT),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNamesStrings.MAGIC_CRPT));
            Items[SpecialItem.ItemTypeSpriteEnum.Grapple] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Grapple,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Grapple],
                "Grappling Hook",
                "Grapple");
            Items[SpecialItem.ItemTypeSpriteEnum.Spyglass] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Spyglass,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Spyglass],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.SPYGLASS),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.SPYGLASS));
            Items[SpecialItem.ItemTypeSpriteEnum.HMSCape] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.HMSCape,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.HMSCape],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.HMS_CAPE_PLAN),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.HMS_CAPE_PLAN));
            Items[SpecialItem.ItemTypeSpriteEnum.Sextant] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.Sextant,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.Sextant],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.SEXTANT),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.SEXTANT));
            Items[SpecialItem.ItemTypeSpriteEnum.PocketWatch] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.PocketWatch,
                //gameStateByteArray[(int)SpecialItem.ItemTypeEnum.PocketWatch],
                1,
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.POCKET_WATCH),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.POCKET_WATCH));
            Items[SpecialItem.ItemTypeSpriteEnum.BlackBadge] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.BlackBadge,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.BlackBadge],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.BLACK_BADGE),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.BLACK_BADGE));
            Items[SpecialItem.ItemTypeSpriteEnum.WoodenBox] = new SpecialItem(SpecialItem.ItemTypeSpriteEnum.WoodenBox,
                gameStateByteArray[(int)SpecialItem.ItemTypeEnum.WoodenBox],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.WOODEN_BOX),
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNames2Strings.WOODEN_BOX));
        }

    }

    public class Spells : InventoryItems<Spell.SpellWords, Spell>
    {
        public Spells(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            int nIndex = 0;
            foreach (Spell.SpellWords spell in Enum.GetValues(typeof(Spell.SpellWords)))
            {
                AddSpell(spell, (DataOvlReference.SpellStrings)nIndex++);
            }
        }

        private void AddSpell(Spell.SpellWords spellWord, DataOvlReference.SpellStrings spellStr)
        {
            Items[spellWord] = new Spell(spellWord, GameStateByteArray[(int)spellWord],
              DataOvlRef.StringReferences.GetString(spellStr),
              DataOvlRef.StringReferences.GetString(spellStr));
        }

        private static TextInfo _ti = new CultureInfo("en-US", false).TextInfo;
        private static Dictionary<string, string> _literalTranslationDictionary = new Dictionary<string, string>
        {
            {"An","Negate"},
            {"Bet","Small"},
            {"Corp","Death"},
            {"Des","Down"},
            {"Ex","Freedom"},
            {"Flam","Flame"},
            {"Grav","Energy"},
            {"Hur","Wind"},
            {"In","Create"},
            {"Kal","Invoke"},
            {"Lor","Light"},
            {"Mani","Life"},
            {"Nox","Poison"},
            {"Por","Movement"},
            {"Quas","Illusion"},
            {"Rel","Change"},
            {"Sanct","Protection"},
            {"Tym","Time"},
            {"Uus","Up"},
            {"Vas","Great"},
            {"Wis","Knowledge"},
            {"Xen","Creature"},
            {"Ylem","Matter"},
            {"Zu","Sleep"}
        };

        static public string GetLiteralTranslation(string syllable)
        {
            return (_literalTranslationDictionary[_ti.ToTitleCase(syllable)]);
        }

        public override Dictionary<Spell.SpellWords, Spell> Items { get; } = new Dictionary<Spell.SpellWords, Spell>();
    }


    public class Scrolls : InventoryItems<Spell.SpellWords, Scroll>
    {
        public override Dictionary<Spell.SpellWords, Scroll> Items { get; } = new Dictionary<Spell.SpellWords, Scroll>(8);
        
        // we apply an offset into the save game file by the number of spells since scrolls come immediatelly after
        // I wouldn't normally like to use offsets like this, but I want spells and scrolls to be linkable by the same enum
        private readonly int _nQuantityIndexAdjust = Enum.GetValues(typeof(Spell.SpellWords)).Length;

        private void AddScroll(Spell.SpellWords spellWord, DataOvlReference.SpellStrings spellStr)
        {
            Scroll.ScrollSpells scrollSpell = (Scroll.ScrollSpells)Enum.Parse(typeof(Scroll.ScrollSpells), spellWord.ToString());

            int nIndex = 0x27A + (int)scrollSpell;
            Items[spellWord] = new Scroll(spellWord, GameStateByteArray[nIndex],
              DataOvlRef.StringReferences.GetString(spellStr),
              DataOvlRef.StringReferences.GetString(spellStr));
        }

        public Scrolls(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            AddScroll(Spell.SpellWords.Vas_Lor, DataOvlReference.SpellStrings.VAS_LOR);
            AddScroll(Spell.SpellWords.Rel_Hur, DataOvlReference.SpellStrings.REL_HUR);
            AddScroll(Spell.SpellWords.In_Sanct, DataOvlReference.SpellStrings.IN_SANCT);
            AddScroll(Spell.SpellWords.In_An, DataOvlReference.SpellStrings.IN_AN);
            AddScroll(Spell.SpellWords.In_Quas_Wis, DataOvlReference.SpellStrings.IN_QUAS_WIS);
            AddScroll(Spell.SpellWords.Kal_Xen_Corp, DataOvlReference.SpellStrings.KAL_XEN_CORP);
            AddScroll(Spell.SpellWords.In_Mani_Corp, DataOvlReference.SpellStrings.IN_MANI_CORP);
            AddScroll(Spell.SpellWords.An_Tym, DataOvlReference.SpellStrings.AN_TYM);
        }
    }

    public class Potions : InventoryItems<Potion.PotionColor, Potion>
    {
        public override Dictionary<Potion.PotionColor, Potion> Items { get; } = new Dictionary<Potion.PotionColor, Potion>(8);

        private void AddPotion(Potion.PotionColor color, DataOvlReference.PotionsStrings potStr)
        {
            Items[color] = new Potion(color, GameStateByteArray[(int)color],
                DataOvlRef.StringReferences.GetString(potStr),
                DataOvlRef.StringReferences.GetString(potStr));
        }

        public Potions(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base (dataOvlRef, gameStateByteArray)
        {
            AddPotion(Potion.PotionColor.Blue, DataOvlReference.PotionsStrings.BLUE);
            AddPotion(Potion.PotionColor.Yellow, DataOvlReference.PotionsStrings.YELLOW);
            AddPotion(Potion.PotionColor.Red, DataOvlReference.PotionsStrings.RED);
            AddPotion(Potion.PotionColor.Green, DataOvlReference.PotionsStrings.GREEN);
            AddPotion(Potion.PotionColor.Orange, DataOvlReference.PotionsStrings.ORANGE);
            AddPotion(Potion.PotionColor.Purple, DataOvlReference.PotionsStrings.PURPLE);
            AddPotion(Potion.PotionColor.Black, DataOvlReference.PotionsStrings.BLACK);
            AddPotion(Potion.PotionColor.White, DataOvlReference.PotionsStrings.WHITE);
        }
    }

    public class Moonstones :  InventoryItems <MoonPhaseReferences.MoonPhases, Moonstone>
    {
        private Moongates _moongates;
        private MoonPhaseReferences _moonPhaseReferences;
        
        
        
        public Moonstones(DataOvlReference dataOvlRef, MoonPhaseReferences moonPhaseReferences, Moongates moongates, InventoryReferences invRefs) 
            : base(dataOvlRef, null)
        {
            _moongates = moongates;
            _moonPhaseReferences = moonPhaseReferences;
            
            // go through each of the moon phases one by one and create a moonstone
            //foreach (Spell.SpellWords spell in Enum.GetValues(typeof(Spell.SpellWords)))
            foreach (MoonPhaseReferences.MoonPhases phase in Enum.GetValues(typeof(MoonPhaseReferences.MoonPhases)))
            {
                // there is no "no moon" moonstone
                if (phase == MoonPhaseReferences.MoonPhases.NoMoon) continue;
                Items[phase] = new Moonstone(phase,
                    dataOvlRef.StringReferences.GetString(DataOvlReference.ZstatsStrings.MOONSTONE_SPACE).TrimEnd(),
                    dataOvlRef.StringReferences.GetString(DataOvlReference.ZstatsStrings.MOONSTONE_SPACE).TrimEnd(),
                    dataOvlRef.StringReferences.GetString(DataOvlReference.ThingsIFindStrings.A_STRANGE_ROCK_BANG_N)
                        .TrimEnd(),
                    moongates, null);
                //invRefs.GetInventoryReference(InventoryReferences.InventoryReferenceType.Item, phase.ToString()));
            }
        }

        public override Dictionary<MoonPhaseReferences.MoonPhases, Moonstone> Items { get; } = 
            new Dictionary<MoonPhaseReferences.MoonPhases, Moonstone>();
    }
    
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


    public class LordBritishArtifacts : InventoryItems <LordBritishArtifact.ArtifactType, LordBritishArtifact>
    {
        private enum Offsets { AMULET = 0x20D, CROWN = 0x20E, SCEPTRE = 0x20F};

        public override Dictionary<LordBritishArtifact.ArtifactType, LordBritishArtifact> Items { get; } =
            new Dictionary<LordBritishArtifact.ArtifactType, LordBritishArtifact>(3);

        public LordBritishArtifacts(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            Items[LordBritishArtifact.ArtifactType.Amulet] = new LordBritishArtifact(LordBritishArtifact.ArtifactType.Amulet,
                gameStateByteArray[(int)Offsets.AMULET],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNamesStrings.AMULET),
                dataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.WEARING_AMULET));
            Items[LordBritishArtifact.ArtifactType.Crown] = new LordBritishArtifact(LordBritishArtifact.ArtifactType.Crown,
                gameStateByteArray[(int)Offsets.CROWN],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNamesStrings.CROWN),
                dataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.DON_THE_CROWN));
            Items[LordBritishArtifact.ArtifactType.Sceptre] = new LordBritishArtifact(LordBritishArtifact.ArtifactType.Sceptre,
                gameStateByteArray[(int)Offsets.SCEPTRE],
                dataOvlRef.StringReferences.GetString(DataOvlReference.SpecialItemNamesStrings.SCEPTRE),
                dataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings.WIELD_SCEPTRE));
        }
    }

}
