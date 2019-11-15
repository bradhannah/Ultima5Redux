using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using static Ultima5Redux.GameState;


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

        public List<InventoryItem> AllItems { get; } = new List<InventoryItem>();

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


        public Inventory(List<byte> gameStateByteArray, DataOvlReference dataOvlRef)
        {
            this.gameStateByteArray = gameStateByteArray;

            DataChunk.CreateDataChunk(DataChunk.DataFormatType.Byte, "Grapple", gameStateByteArray, 0x209, sizeof(byte));
            DataChunk.CreateDataChunk(DataChunk.DataFormatType.Byte, "Magic Carpet", gameStateByteArray, 0x20A, sizeof(byte));

            

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
