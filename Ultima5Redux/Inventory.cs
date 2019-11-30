using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using static Ultima5Redux.GameState;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class Inventory
    {
        private List<byte> gameStateByteArray;

        public LordBritishArtifacts Artifacts { get; }
        public ShadowlordShards Shards { get; }
        public Potions MagicPotions { get; }
        public Scrolls MagicScrolls { get; }
        public SpecialItems SpecializedItems { get; }
        public Armours ProtectiveArmour { get; }
        public Weapons TheWeapons { get; }
        public List<InventoryItem> AllItems { get; } = new List<InventoryItem>();
        public List<InventoryItem> ReadyItems { get; } = new List<InventoryItem>();

        public enum InventoryThings { Grapple = 0x209, MagicCarpets = 0x20A };

        private byte BoolToByte(bool bBool)
        {
            return bBool ? (byte)1 : (byte)0;
        }

        private void SetInventoryQuantity(InventoryThings thing, byte nThings)
        {
            gameStateByteArray[(int)thing] = nThings;
        }

        private byte GetInventoryQuantity(InventoryThings thing)
        {
            return gameStateByteArray[(int)thing];
        }

        private void SetInventoryBool(InventoryThings thing, bool bBool)
        {
            gameStateByteArray[(int)thing] = BoolToByte(bBool);
        }

        public bool GetInvetoryBool(InventoryThings thing)
        {
            return DataChunk.CreateDataChunk(DataChunk.DataFormatType.Byte, "", gameStateByteArray, (int)thing, sizeof(byte)).GetChunkAsByte() > 0;
        }

        public bool Grapple 
        { 
            get
            {
                return GetInvetoryBool(InventoryThings.Grapple);
            }
            set
            {
                SetInventoryBool(InventoryThings.Grapple, value);
            }
        }

        public int MagicCarpets
        { 
            get
            {
                return GetInventoryQuantity(InventoryThings.MagicCarpets);
            }
            set
            {
                SetInventoryQuantity(InventoryThings.MagicCarpets, (byte)value);
            }
        }

        public CombatItem GetItemFromEquipment(DataOvlReference.EQUIPMENT equipment)
        {
            foreach (CombatItem item in ReadyItems)
            {
                if (item.SpecificEquipment == equipment)
                {
                    return item;
                }
            }
            throw new Exception("Requested " + equipment.ToString() + " but is not a combat type");
        }

        public Inventory(List<byte> gameStateByteArray, DataOvlReference dataOvlRef)
        {
            this.gameStateByteArray = gameStateByteArray;

            DataChunk.CreateDataChunk(DataChunk.DataFormatType.Byte, "Grapple", gameStateByteArray, 0x209, sizeof(byte));
            DataChunk.CreateDataChunk(DataChunk.DataFormatType.Byte, "Magic Carpet", gameStateByteArray, 0x20A, sizeof(byte));

            ProtectiveArmour = new Armours(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(ProtectiveArmour.GenericItemList);
            ReadyItems.AddRange(ProtectiveArmour.GenericItemList);

            TheWeapons = new Weapons(dataOvlRef, gameStateByteArray);
            ReadyItems.AddRange(TheWeapons.GenericItemList);

            MagicScrolls = new Scrolls(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(MagicScrolls.GenericItemList);

            MagicPotions = new Potions(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(MagicPotions.GenericItemList);

            SpecializedItems = new SpecialItems(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(SpecializedItems.GenericItemList);    

            Artifacts = new LordBritishArtifacts(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(Artifacts.GenericItemList);

            Shards = new ShadowlordShards(dataOvlRef, gameStateByteArray);
            AllItems.AddRange(Shards.GenericItemList);


        }

    }
}
