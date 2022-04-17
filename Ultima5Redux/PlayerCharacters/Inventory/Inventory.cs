using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

// ReSharper disable MemberCanBePrivate.Global

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class Inventory
    {
        [DataMember] public LordBritishArtifacts Artifacts { get; set; }
        [DataMember] public Potions MagicPotions { get; set; }
        [DataMember] public Scrolls MagicScrolls { get; set; }
        [DataMember] public Spells MagicSpells { get; set; }
        [DataMember] public Armours ProtectiveArmour { get; set; }
        [DataMember] public ShadowlordShards Shards { get; set; }
        [DataMember] public SpecialItems SpecializedItems { get; set; }
        [DataMember] public Reagents SpellReagents { get; set; }
        [DataMember] public Moonstones TheMoonstones { get; set; }
        [DataMember] public Provisions TheProvisions { get; set; }
        [DataMember] public Weapons TheWeapons { get; set; }
        [IgnoreDataMember] public List<InventoryItem> AllItems { get; } = new();
        [IgnoreDataMember] public List<CombatItem> CombatItems { get; } = new();

        [IgnoreDataMember]
        public int Food => TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Food].Quantity;

        [IgnoreDataMember]
        public int Gold => TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Gold].Quantity;

        [IgnoreDataMember] public List<CombatItem> ReadyItems { get; } = new();

        [IgnoreDataMember] public IEnumerable<InventoryItem> ReadyItemsAsInventoryItem => ReadyItems;

        [IgnoreDataMember] public List<InventoryItem> UseItems { get; } = new();
        private readonly ImportedGameState _importedGameState;

        internal Inventory(ImportedGameState importedGameState)
        {
            _importedGameState = importedGameState;

            RefreshInventoryFromLegacySave();
        }

        [JsonConstructor] private Inventory()
        {
        }

        [OnDeserialized] internal void OnDeserializedMethod(StreamingContext context)
        {
            // update the statically cached lists 
            RefreshRollupInventory();
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

        private void RefreshInventoryFromLegacySave()
        {
            AllItems.Clear();
            ReadyItems.Clear();
            UseItems.Clear();

            ProtectiveArmour = new Armours(_importedGameState);
            TheWeapons = new Weapons(_importedGameState);
            MagicScrolls = new Scrolls(_importedGameState);
            MagicPotions = new Potions(_importedGameState);
            SpecializedItems = new SpecialItems(_importedGameState);
            Artifacts = new LordBritishArtifacts(_importedGameState);
            Shards = new ShadowlordShards(_importedGameState);
            SpellReagents = new Reagents(_importedGameState);
            MagicSpells = new Spells(_importedGameState);
            TheProvisions = new Provisions(_importedGameState);
            TheMoonstones = new Moonstones();

            RefreshRollupInventory();
        }

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

        /// <summary>
        ///     Gets the characters total attack if left and right hand both attacked successfully
        /// </summary>
        /// <param name="record">Character record</param>
        /// <returns>amount of total damage</returns>
        public int GetCharacterTotalAttack(PlayerCharacterRecord record)
        {
            return GetAttack(record.Equipped.Amulet) + GetAttack(record.Equipped.Armour) +
                   GetAttack(record.Equipped.Helmet) + GetAttack(record.Equipped.Ring) +
                   GetAttack(record.Equipped.LeftHand) + GetAttack(record.Equipped.RightHand);
        }

        /// <summary>
        ///     Gets the players total defense of all items equipped
        /// </summary>
        /// <param name="record">character record</param>
        /// <returns>the players total defense</returns>
        public int GetCharacterTotalDefense(PlayerCharacterRecord record)
        {
            return GetDefense(record.Equipped.Amulet) + GetDefense(record.Equipped.Armour) +
                   GetDefense(record.Equipped.Helmet) + GetDefense(record.Equipped.LeftHand) +
                   GetDefense(record.Equipped.RightHand) + GetDefense(record.Equipped.Ring);
        }

        /// <summary>
        ///     Gets the Combat Item (inventory item) based on the equipped item
        /// </summary>
        /// <param name="equipment">type of combat equipment</param>
        /// <returns>combat item object</returns>
        public CombatItem GetItemFromEquipment(DataOvlReference.Equipment equipment)
        {
            if (equipment == DataOvlReference.Equipment.Nothing) return null;
            return ReadyItems.FirstOrDefault(item => item.SpecificEquipment == equipment) ??
                   throw new Ultima5ReduxException("Tried to get " + equipment + " but wasn't in my ReadyItems");
        }

        public bool SpendGold(int nGold)
        {
            if (TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Gold].Quantity < nGold) return false;
            TheProvisions.Items[ProvisionReferences.SpecificProvisionType.Gold].Quantity -= nGold;
            return true;
        }

        /// <summary>
        ///     Finds a corresponding inventoryItem and adds the quantity, or just plain ole adds it to the
        ///     the inventory if didn't already exist
        /// </summary>
        /// <param name="inventoryItem"></param>
        public void AddInventoryItemToInventory(InventoryItem inventoryItem)
        {
            //void updateByEquipment(inv)

            switch (inventoryItem)
            {
                case Armour armour:
                    // case ChestArmour chestArmour:
                    // case Helm helm:
                    // case Amulet amulet:
                    // case Ring ring:
                    ProtectiveArmour.GetArmourFromEquipment(armour.SpecificEquipment).Quantity += armour.Quantity;
                    break;
                case Weapon weapon:
                    TheWeapons.GetWeaponFromEquipment(weapon.SpecificEquipment).Quantity += weapon.Quantity;
                    break;
                case LordBritishArtifact lordBritishArtifact:
                    Debug.Assert(lordBritishArtifact.Quantity == 1);
                    Artifacts.Items[lordBritishArtifact.Artifact].Quantity += lordBritishArtifact.Quantity;
                    break;
                case Moonstone moonstone:
                    // OOF this is a different cup of tea
                    Debug.Assert(moonstone.Quantity == 1);
                    TheMoonstones.Items[moonstone.Phase].Quantity += moonstone.Quantity;
                    break;
                case Potion potion:
                    MagicPotions.Items[potion.Color].Quantity += potion.Quantity;
                    break;
                case Provision provision:
                    TheProvisions.Items[provision.ProvisionType].Quantity += provision.Quantity;
                    break;
                case Reagent reagent:
                    SpellReagents.Items[reagent.ReagentType].Quantity += reagent.Quantity;
                    break;
                case Scroll scroll:
                    MagicScrolls.Items[scroll.ScrollSpell].Quantity += scroll.Quantity;
                    break;
                case ShadowlordShard shadowlordShard:
                    Debug.Assert(shadowlordShard.Quantity == 1);
                    Shards.Items[shadowlordShard.Shard].Quantity += shadowlordShard.Quantity;
                    break;
                case SpecialItem specialItem:
                    SpecializedItems.Items[specialItem.ItemType].Quantity += specialItem.Quantity;
                    break;
                case Spell spell:
                    // I don't think this should happen - but I guess we could use this when mixing for new spells
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inventoryItem));
            }
            //     [DataMember] public LordBritishArtifacts Artifacts { get; set; }
            // [DataMember] public Potions MagicPotions { get; set; }
            // [DataMember] public Scrolls MagicScrolls { get; set; }
            // [DataMember] public Spells MagicSpells { get; set; }
            // [DataMember] public Armours ProtectiveArmour { get; set; }
            // [DataMember] public ShadowlordShards Shards { get; set; }
            // [DataMember] public SpecialItems SpecializedItems { get; set; }
            // [DataMember] public Reagents SpellReagents { get; set; }
            // [DataMember] public Moonstones TheMoonstones { get; set; }
            // [DataMember] public Provisions TheProvisions { get; set; }
            // [DataMember] public Weapons TheWeapons { get; set; }           
        }
    }
}