using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.PlayerCharacters.CombatItems;

// ReSharper disable MemberCanBePrivate.Global

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract]
    public class Inventory
    {
        public enum InventoryThings { Grapple = 0x209, MagicCarpets = 0x20A }

        private readonly DataOvlReference _dataOvlRef;
        private readonly List<byte> _gameStateByteArray;
        private readonly InventoryReferences _inventoryReferences;
        private readonly MagicReferences _magicReferences;
        private readonly CombatItemReferences _combatItemReferences;
        private readonly Moongates _moongates;
        private readonly MoonPhaseReferences _moonPhaseReferences;
        private readonly GameState _state;

        internal Inventory(List<byte> gameStateByteArray, DataOvlReference dataOvlRef,
            MoonPhaseReferences moonPhaseReferences, Moongates moongates, GameState state,
            InventoryReferences inventoryReferences, MagicReferences magicReferences,
            ImportedGameState importedGameState, CombatItemReferences combatItemReferences)
        {
            _gameStateByteArray = gameStateByteArray;
            _dataOvlRef = dataOvlRef;
            _moonPhaseReferences = moonPhaseReferences;
            _moongates = moongates;
            _state = state;
            _inventoryReferences = inventoryReferences;
            _magicReferences = magicReferences;
            _combatItemReferences = combatItemReferences;

            RefreshInventoryFromLegacySave();
        }

        [IgnoreDataMember] public int Gold => TheProvisions.Items[Provision.ProvisionTypeEnum.Gold].Quantity;
        [IgnoreDataMember] public int Food => TheProvisions.Items[Provision.ProvisionTypeEnum.Food].Quantity;
        
  
        [IgnoreDataMember] public List<CombatItem> CombatItems { get; } = new List<CombatItem>();
        [IgnoreDataMember] public List<CombatItem> ReadyItems { get; } = new List<CombatItem>();
        [IgnoreDataMember] public List<InventoryItem> AllItems { get; } = new List<InventoryItem>();

        [IgnoreDataMember] public List<InventoryItem> ReadyItemsAsInventoryItem => ReadyItems.Cast<InventoryItem>().ToList();

        [IgnoreDataMember] public List<InventoryItem> UseItems { get; } = new List<InventoryItem>();
        
        [OnDeserialized] internal void OnDeserializedMethod(StreamingContext context)
        {
            // update the statically cached lists 
            RefreshRollupInventory();
            // when deserializing, we have not saved the inventory references because they are static, 
            // so we will add them after the fact
            UpdateAllInventoryReferences();
        }

        [OnSerialized] internal void OnSerializedMethod(StreamingContext context)
        {
        }
        
        [DataMember] public Weapons TheWeapons { get; set; }
        [DataMember] public Armours ProtectiveArmour { get; set; }
        [DataMember] public LordBritishArtifacts Artifacts { get; set; }
        [DataMember] public Moonstones TheMoonstones { get; set; }
        [DataMember] public Potions MagicPotions { get; set; }
        [DataMember] public Provisions TheProvisions { get; set; }
        [DataMember] public Reagents SpellReagents { get; set; }
        [DataMember] public Scrolls MagicScrolls { get; set; }
        [DataMember] public ShadowlordShards Shards { get; set; }
        [DataMember] public SpecialItems SpecializedItems { get; set; }
        [DataMember] public Spells MagicSpells { get; set; }

        public bool SpendGold(int nGold)
        {
            if (TheProvisions.Items[Provision.ProvisionTypeEnum.Gold].Quantity < nGold) return false;
            TheProvisions.Items[Provision.ProvisionTypeEnum.Gold].Quantity -= nGold;
            return true;
        }
        
        /// <summary>
        ///     Gets the attack of a particular piece of equipment
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        private int GetAttack(DataOvlReference.Equipment equipment)
        {
            Weapon weapon = TheWeapons.GetWeaponFromEquipment(equipment);
            if (weapon != null) return weapon.TheCombatItemReference.AttackStat;

            Armour armour = ProtectiveArmour.GetArmourFromEquipment(equipment);
            return armour?.TheCombatItemReference.AttackStat ?? 0;
        }

        /// <summary>
        ///     Gets the defense of a particular piece of equipment
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        private int GetDefense(DataOvlReference.Equipment equipment)
        {
            Weapon weapon = TheWeapons.GetWeaponFromEquipment(equipment);
            if (weapon != null) return weapon.TheCombatItemReference.DefendStat;

            Armour armour = ProtectiveArmour.GetArmourFromEquipment(equipment);
            return armour?.TheCombatItemReference.DefendStat ?? 0;
        }

        /// <summary>
        ///     Gets the characters total attack if left and right hand both attacked successfully
        /// </summary>
        /// <param name="record">Character record</param>
        /// <returns>amount of total damage</returns>
        public int GetCharacterTotalAttack(PlayerCharacterRecord record)
        {
            return GetAttack(record.Equipped.Amulet) + GetAttack(record.Equipped.Armour) +
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
            return GetDefense(record.Equipped.Amulet) + GetDefense(record.Equipped.Armour) +
                   GetDefense(record.Equipped.Helmet) +
                   GetDefense(record.Equipped.LeftHand) + GetDefense(record.Equipped.RightHand) +
                   GetDefense(record.Equipped.Ring);
        }


        /// <summary>
        ///     Gets the Combat Item (inventory item) based on the equipped item
        /// </summary>
        /// <param name="equipment">type of combat equipment</param>
        /// <returns>combat item object</returns>
        public CombatItem GetItemFromEquipment(DataOvlReference.Equipment equipment) =>
            ReadyItems.FirstOrDefault(item => item.SpecificEquipment == equipment);

        private void RefreshRollupInventory()
        {
            AllItems.Clear();
            ReadyItems.Clear();
            UseItems.Clear();
            CombatItems.Clear();
            
            AllItems.AddRange(ProtectiveArmour.GenericItemList);
            ReadyItems.AddRange(ProtectiveArmour.AllCombatItems);
            CombatItems.AddRange(ProtectiveArmour.AllCombatItems);
            
            AllItems.AddRange(TheWeapons.GenericItemList);
            ReadyItems.AddRange(TheWeapons.AllCombatItems);
            CombatItems.AddRange(TheWeapons.AllCombatItems);
            
            AllItems.AddRange(MagicScrolls.GenericItemList);
            UseItems.AddRange(MagicScrolls.GenericItemList);

            AllItems.AddRange(MagicPotions.GenericItemList);
            UseItems.AddRange(MagicPotions.GenericItemList);

            AllItems.AddRange(SpecializedItems.GenericItemList);
            UseItems.AddRange(SpecializedItems.GenericItemList);

            AllItems.AddRange(Artifacts.GenericItemList);
            UseItems.AddRange(Artifacts.GenericItemList);
            
            AllItems.AddRange(Shards.GenericItemList);
            UseItems.AddRange(Shards.GenericItemList);

            AllItems.AddRange(SpellReagents.GenericItemList);

            AllItems.AddRange(MagicSpells.GenericItemList);
            
            AllItems.AddRange(TheMoonstones.GenericItemList);
            UseItems.AddRange(TheMoonstones.GenericItemList);
            
            AllItems.AddRange(TheProvisions.GenericItemList);
        }
        
        private void RefreshInventoryFromLegacySave()
        {
            AllItems.Clear();
            ReadyItems.Clear();
            UseItems.Clear();

            ProtectiveArmour = new Armours(_combatItemReferences, _gameStateByteArray);
            TheWeapons = new Weapons(_combatItemReferences, _gameStateByteArray);
            MagicScrolls = new Scrolls(_gameStateByteArray, _magicReferences);
            MagicPotions = new Potions(_gameStateByteArray);
            SpecializedItems = new SpecialItems(_gameStateByteArray);
            Artifacts = new LordBritishArtifacts(_dataOvlRef, _gameStateByteArray);
            Shards = new ShadowlordShards(_dataOvlRef, _gameStateByteArray);
            SpellReagents = new Reagents( _gameStateByteArray, _state);
            MagicSpells = new Spells(_gameStateByteArray, _magicReferences);
            TheMoonstones = new Moonstones(_moongates);
            TheProvisions = new Provisions(_state);

            RefreshRollupInventory();
            
            UpdateAllInventoryReferences();
        }

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
                            InventoryReferences.InventoryReferenceType.Item, item.InventoryReferenceString);
                        break;
                    case Spell magicSpell:
                        item.InvRef = _inventoryReferences.GetInventoryReference(
                            InventoryReferences.InventoryReferenceType.Spell, item.InventoryReferenceString);
                        break;
                    case Reagent spellReagent:
                        item.InvRef = _inventoryReferences.GetInventoryReference(
                            InventoryReferences.InventoryReferenceType.Reagent, item.InventoryReferenceString);
                        break;
                    case CombatItem combatItem: 
                        //protectiveArmour:
                        //case Weapon weapon:
                        // we have to assign this manually because when we serialize from JSON it is unable to link it 
                        // itself
                        item.InvRef = _inventoryReferences.GetInventoryReference(
                            InventoryReferences.InventoryReferenceType.Armament, item.InventoryReferenceString);
                        break;
                    default:
                        throw new Ultima5ReduxException("Tried to update InventoryReference on " + item.GetType());
                }
            }
        }
    }
}