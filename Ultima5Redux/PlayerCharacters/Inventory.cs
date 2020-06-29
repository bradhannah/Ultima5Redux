using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
// ReSharper disable MemberCanBePrivate.Global

namespace Ultima5Redux.PlayerCharacters
{
    public class Inventory
    {
        private readonly List<byte> _gameStateByteArray;

        public LordBritishArtifacts Artifacts { get; }
        public ShadowlordShards Shards { get; }
        public Potions MagicPotions { get; }
        public Scrolls MagicScrolls { get; }
        public Spells MagicSpells { get; }
        public SpecialItems SpecializedItems { get; }
        public Armours ProtectiveArmour { get; }
        public Weapons TheWeapons { get; }
        public Reagents SpellReagents { get; }
        public Moonstones TheMoonstones { get; }
        public List<InventoryItem> AllItems { get; } = new List<InventoryItem>();
        public List<InventoryItem> ReadyItems { get; } = new List<InventoryItem>();
        public List<InventoryItem> UseItems { get; } = new List<InventoryItem>();
        public List<CombatItem> CombatItems { get; } = new List<CombatItem>();
        public enum InventoryThings { Grapple = 0x209, MagicCarpets = 0x20A };

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
            return DataChunk.CreateDataChunk(DataChunk.DataFormatType.Byte, "", _gameStateByteArray, (int)thing, sizeof(byte)).GetChunkAsByte() > 0;
        }

        public bool Grapple 
        { 
            get => GetInventoryBool(InventoryThings.Grapple);
            set => SetInventoryBool(InventoryThings.Grapple, value);
        }

        public int MagicCarpets
        { 
            get => GetInventoryQuantity(InventoryThings.MagicCarpets);
            set => SetInventoryQuantity(InventoryThings.MagicCarpets, (byte)value);
        }

        /// <summary>
        /// Gets the attack of a particular piece of equipment
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        private int GetAttack(DataOvlReference.Equipment equipment)
        {
            Weapon weapon = TheWeapons.GetWeaponFromEquipment(equipment);
            if (weapon != null)
            {
                return weapon.AttackStat;
            }

            Armour armour = ProtectiveArmour.GetArmourFromEquipment(equipment);
            return armour?.AttackStat ?? 0;
        }

        /// <summary>
        /// Gets the defense of a particular piece of equipment
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        private int GetDefense(DataOvlReference.Equipment equipment)
        {
            Weapon weapon = TheWeapons.GetWeaponFromEquipment(equipment);
            if (weapon != null)
            {
                return weapon.AttackStat;
            }

            Armour armour = ProtectiveArmour.GetArmourFromEquipment(equipment);
            return armour?.DefendStat ?? 0;
        }

        /// <summary>
        /// Gets the characters total attack if left and right hand both attacked successfully
        /// </summary>
        /// <param name="record">Character record</param>
        /// <returns>amount of total damage</returns>
        public int GetCharacterTotalAttack (PlayerCharacterRecord record)
        {
            return GetAttack(record.Equipped.Amulet) + GetAttack(record.Equipped.Armor) + GetAttack(record.Equipped.Helmet)
                + GetAttack(record.Equipped.Ring) + GetAttack(record.Equipped.LeftHand) + GetAttack(record.Equipped.RightHand);
        }

        /// <summary>
        /// Gets the players total defense of all items equipped
        /// </summary>
        /// <param name="record">character record</param>
        /// <returns>the players total defense</returns>
        public int GetCharacterTotalDefense (PlayerCharacterRecord record)
        {
            return GetDefense(record.Equipped.Amulet) + GetDefense(record.Equipped.Armor) + GetDefense(record.Equipped.Helmet) +
                GetDefense(record.Equipped.LeftHand) + GetDefense(record.Equipped.RightHand) + GetDefense(record.Equipped.Ring);
        }


        /// <summary>
        /// Gets the Combat Item (inventory item) based on the equipped item
        /// </summary>
        /// <param name="equipment">type of combat equipment</param>
        /// <returns>combat item object</returns>
        public CombatItem GetItemFromEquipment(DataOvlReference.Equipment equipment)
        {
            foreach (CombatItem item in ReadyItems)
            {
                if (item.SpecificEquipment == equipment)
                {
                    return item;
                }
            }
            throw new Ultima5ReduxException("Requested " + equipment + " but is not a combat type");
        }

        public Inventory(List<byte> gameStateByteArray, DataOvlReference dataOvlRef,  
            MoonPhaseReferences moonPhaseReferences, Moongates moongates, GameState state)
        {
            this._gameStateByteArray = gameStateByteArray;

            _ = DataChunk.CreateDataChunk(DataChunk.DataFormatType.Byte, "Grapple", gameStateByteArray, 0x209, sizeof(byte));
            _ = DataChunk.CreateDataChunk(DataChunk.DataFormatType.Byte, "Magic Carpet", gameStateByteArray, 0x20A, sizeof(byte));

            ProtectiveArmour = new Armours(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(ProtectiveArmour.GenericItemList);
            ReadyItems.AddRange(ProtectiveArmour.GenericItemList);
            CombatItems.AddRange(ProtectiveArmour.AllCombatItems);

            TheWeapons = new Weapons(dataOvlRef, gameStateByteArray);
            ReadyItems.AddRange(TheWeapons.GenericItemList);
            CombatItems.AddRange(TheWeapons.AllCombatItems);

            MagicScrolls = new Scrolls(dataOvlRef, gameStateByteArray);
            UseItems.AddRange(MagicScrolls.GenericItemList);

            MagicPotions = new Potions(dataOvlRef, gameStateByteArray);
            UseItems.AddRange(MagicPotions.GenericItemList);

            SpecializedItems = new SpecialItems(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(SpecializedItems.GenericItemList);
            UseItems.AddRange(SpecializedItems.GenericItemList);

            Artifacts = new LordBritishArtifacts(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(Artifacts.GenericItemList);
            UseItems.AddRange(Artifacts.GenericItemList);

            Shards = new ShadowlordShards(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(Shards.GenericItemList);
            UseItems.AddRange(Shards.GenericItemList);

            SpellReagents = new Reagents(dataOvlRef, gameStateByteArray, state);
            AllItems.AddRange(SpellReagents.GenericItemList);

            MagicSpells = new Spells(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(MagicSpells.GenericItemList);

            TheMoonstones = new Moonstones(dataOvlRef, moonPhaseReferences, moongates);
            AllItems.AddRange(TheMoonstones.GenericItemList);
            UseItems.AddRange(TheMoonstones.GenericItemList);
        }

    }
}
