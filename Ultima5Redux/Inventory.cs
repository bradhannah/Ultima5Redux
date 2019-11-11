using System;
using System.Collections;
using System.Collections.Generic;
using static Ultima5Redux.GameState;

namespace Ultima5Redux
{
    public class Inventory
    {
        private List<byte> gameStateByteArray;
        private DataChunks<DataChunkName> dataChunks;

        List<InventoryItem.LordBritishArtifact> Artifacts;

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

            Artifacts = new List<InventoryItem.LordBritishArtifact>(3);
            LordBritishArtifacts artifacts = new LordBritishArtifacts(dataOvlRef, gameStateByteArray);
            

        }
    }
}
