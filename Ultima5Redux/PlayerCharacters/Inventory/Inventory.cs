using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.PlayerCharacters.CombatItems;

// ReSharper disable MemberCanBePrivate.Global

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Inventory
    {
        public enum InventoryThings { Grapple = 0x209, MagicCarpets = 0x20A }

        private readonly DataOvlReference _dataOvlRef;
        private readonly List<byte> _gameStateByteArray;
        private readonly InventoryReferences _inventoryReferences;
        private readonly Moongates _moongates;
        private readonly MoonPhaseReferences _moonPhaseReferences;
        private readonly GameState _state;

        public Inventory(List<byte> gameStateByteArray, DataOvlReference dataOvlRef,
            MoonPhaseReferences moonPhaseReferences, Moongates moongates, GameState state,
            InventoryReferences inventoryReferences)
        {
            _gameStateByteArray = gameStateByteArray;
            _dataOvlRef = dataOvlRef;
            _moonPhaseReferences = moonPhaseReferences;
            _moongates = moongates;
            _state = state;
            _inventoryReferences = inventoryReferences;
            RefreshInventory();
        }

        public LordBritishArtifacts Artifacts { get; set; }
        public ShadowlordShards Shards { get; set; }
        public Potions MagicPotions { get; set; }
        public Scrolls MagicScrolls { get; set; }
        public Spells MagicSpells { get; set; }
        public SpecialItems SpecializedItems { get; set; }
        public Armours ProtectiveArmour { get; set; }
        public Weapons TheWeapons { get; set; }
        public Reagents SpellReagents { get; set; }
        public Moonstones TheMoonstones { get; set; }
        public List<InventoryItem> AllItems { get; } = new List<InventoryItem>();
        public List<CombatItem> ReadyItems { get; } = new List<CombatItem>();

        public List<InventoryItem> ReadyItemsAsInventoryItem => ReadyItems.Cast<InventoryItem>().ToList();

        //{ get; } = new List<InventoryItem>();
        public List<InventoryItem> UseItems { get; } = new List<InventoryItem>();
        public List<CombatItem> CombatItems { get; } = new List<CombatItem>();
        public Provisions TheProvisions { get; set; }

        public bool Grapple
        {
            get => GetInventoryBool(InventoryThings.Grapple);
            set => SetInventoryBool(InventoryThings.Grapple, value);
        }

        public int MagicCarpets
        {
            get => GetInventoryQuantity(InventoryThings.MagicCarpets);
            set
            {
                int nQuantity = value == 0 || value == 0xFF ? 0 : value;
                SpecializedItems.Items[SpecialItem.ItemTypeSpriteEnum.Carpet].Quantity = nQuantity;
                SetInventoryQuantity(InventoryThings.MagicCarpets, (byte)nQuantity);
            }
        }

        private static byte BoolToByte(bool bBool)
        {
            return bBool ? (byte)1 : (byte)0;
        }

        private void SetInventoryQuantity(InventoryThings thing, byte nThings)
        {
            _gameStateByteArray[(int)thing] = nThings;
        }

        private byte GetInventoryQuantity(InventoryThings thing)
        {
            return _gameStateByteArray[(int)thing];
        }

        private void SetInventoryBool(InventoryThings thing, bool bBool)
        {
            _gameStateByteArray[(int)thing] = BoolToByte(bBool);
        }

        public bool GetInventoryBool(InventoryThings thing)
        {
            return DataChunk
                .CreateDataChunk(DataChunk.DataFormatType.Byte, "", _gameStateByteArray, (int)thing, sizeof(byte))
                .GetChunkAsByte() > 0;
        }

        /// <summary>
        ///     Gets the attack of a particular piece of equipment
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        private int GetAttack(DataOvlReference.Equipment equipment)
        {
            Weapon weapon = TheWeapons.GetWeaponFromEquipment(equipment);
            if (weapon != null) return weapon.AttackStat;

            Armour armour = ProtectiveArmour.GetArmourFromEquipment(equipment);
            return armour?.AttackStat ?? 0;
        }

        /// <summary>
        ///     Gets the defense of a particular piece of equipment
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        private int GetDefense(DataOvlReference.Equipment equipment)
        {
            Weapon weapon = TheWeapons.GetWeaponFromEquipment(equipment);
            if (weapon != null) return weapon.AttackStat;

            Armour armour = ProtectiveArmour.GetArmourFromEquipment(equipment);
            return armour?.DefendStat ?? 0;
        }

        /// <summary>
        ///     Gets the characters total attack if left and right hand both attacked successfully
        /// </summary>
        /// <param name="record">Character record</param>
        /// <returns>amount of total damage</returns>
        public int GetCharacterTotalAttack(PlayerCharacterRecord record)
        {
            return GetAttack(record.Equipped.Amulet) + GetAttack(record.Equipped.Armor) +
                   GetAttack(record.Equipped.Helmet)
                   + GetAttack(record.Equipped.Ring) + GetAttack(record.Equipped.LeftHand) +
                   GetAttack(record.Equipped.RightHand);
        }

        /// <summary>
        ///     Gets the players total defense of all items equipped
        /// </summary>
        /// <param name="record">character record</param>
        /// <returns>the players total defense</returns>
        public int GetCharacterTotalDefense(PlayerCharacterRecord record)
        {
            return GetDefense(record.Equipped.Amulet) + GetDefense(record.Equipped.Armor) +
                   GetDefense(record.Equipped.Helmet) +
                   GetDefense(record.Equipped.LeftHand) + GetDefense(record.Equipped.RightHand) +
                   GetDefense(record.Equipped.Ring);
        }


        /// <summary>
        ///     Gets the Combat Item (inventory item) based on the equipped item
        /// </summary>
        /// <param name="equipment">type of combat equipment</param>
        /// <returns>combat item object</returns>
        public CombatItem GetItemFromEquipment(DataOvlReference.Equipment equipment)
        {
            foreach (CombatItem item in ReadyItems)
            {
                if (item.SpecificEquipment == equipment) return item;
            }

            return null;
            //throw new Ultima5ReduxException("Requested " + equipment + " but is not a combat type");
        }

        public void RefreshInventory()
        {
            AllItems.Clear();
            ReadyItems.Clear();
            UseItems.Clear();

            ProtectiveArmour = new Armours(_dataOvlRef, _gameStateByteArray);
            AllItems.AddRange(ProtectiveArmour.GenericItemList);
            ReadyItems.AddRange(ProtectiveArmour.AllCombatItems);
            CombatItems.AddRange(ProtectiveArmour.AllCombatItems);

            TheWeapons = new Weapons(_dataOvlRef, _gameStateByteArray);
            AllItems.AddRange(TheWeapons.GenericItemList);
            ReadyItems.AddRange(TheWeapons.AllCombatItems);
            CombatItems.AddRange(TheWeapons.AllCombatItems);

            MagicScrolls = new Scrolls(_dataOvlRef, _gameStateByteArray);
            AllItems.AddRange(MagicScrolls.GenericItemList);
            UseItems.AddRange(MagicScrolls.GenericItemList);

            MagicPotions = new Potions(_dataOvlRef, _gameStateByteArray);
            AllItems.AddRange(MagicPotions.GenericItemList);
            UseItems.AddRange(MagicPotions.GenericItemList);

            SpecializedItems = new SpecialItems(_dataOvlRef, _gameStateByteArray);
            AllItems.AddRange(SpecializedItems.GenericItemList);
            UseItems.AddRange(SpecializedItems.GenericItemList);

            Artifacts = new LordBritishArtifacts(_dataOvlRef, _gameStateByteArray);
            AllItems.AddRange(Artifacts.GenericItemList);
            UseItems.AddRange(Artifacts.GenericItemList);

            Shards = new ShadowlordShards(_dataOvlRef, _gameStateByteArray);
            AllItems.AddRange(Shards.GenericItemList);
            UseItems.AddRange(Shards.GenericItemList);

            SpellReagents = new Reagents(_dataOvlRef, _gameStateByteArray, _state);
            AllItems.AddRange(SpellReagents.GenericItemList);

            MagicSpells = new Spells(_dataOvlRef, _gameStateByteArray);
            AllItems.AddRange(MagicSpells.GenericItemList);

            TheMoonstones = new Moonstones(_dataOvlRef, _moonPhaseReferences, _moongates);
            AllItems.AddRange(TheMoonstones.GenericItemList);
            UseItems.AddRange(TheMoonstones.GenericItemList);

            TheProvisions = new Provisions(_dataOvlRef, _state);
            AllItems.AddRange(TheProvisions.GenericItemList);

            UpdateAllInventoryReferences();
        }

        // public void ConsumeItem(InventoryItem item)
        // {
        //     InventoryItem item = AllItems[item];
        //     
        // }


        private void UpdateAllInventoryReferences()
        {
            foreach (InventoryItem item in AllItems)
            {
                switch (item)
                {
                    case LordBritishArtifact artifact:
                    case ShadowlordShard shard:
                    case Potion magicPotion:
                    case Scroll magicScroll:
                    case SpecialItem specializedItem:
                    case Moonstone moonstone:
                    case Provision provision:
                        item.InvRef = _inventoryReferences.GetInventoryReference(
                            InventoryReferences.InventoryReferenceType.Item,
                            item.InventoryReferenceString);
                        break;
                    case Spell magicSpell:
                        item.InvRef = _inventoryReferences.GetInventoryReference(
                            InventoryReferences.InventoryReferenceType.Spell,
                            item.InventoryReferenceString);
                        break;
                    case Reagent spellReagent:
                        item.InvRef = _inventoryReferences.GetInventoryReference(
                            InventoryReferences.InventoryReferenceType.Reagent,
                            item.InventoryReferenceString);
                        break;
                    case Armour protectiveArmour:
                    case Weapon weapon:
                        item.InvRef = _inventoryReferences.GetInventoryReference(
                            InventoryReferences.InventoryReferenceType.Armament,
                            item.InventoryReferenceString);
                        break;
                    default:
                        throw new Ultima5ReduxException("Tried to update InventoryReference on " + item.GetType());
                }
            }
        }
    }
}