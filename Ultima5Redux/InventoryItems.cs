using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultima5Redux
{
    public abstract class InventoryItems
    {

    }

    public class LordBritishArtifacts : InventoryItems
    {
        Dictionary<InventoryItem.LordBritishArtifact.ArtifactType, InventoryItem.LordBritishArtifact> Artifacts = 
            new Dictionary<InventoryItem.LordBritishArtifact.ArtifactType, InventoryItem.LordBritishArtifact>(3);
        public LordBritishArtifacts(DataOvlReference dataOvlRef, List<byte> gameStateByteArray)
        {
            Artifacts[InventoryItem.LordBritishArtifact.ArtifactType.Crown] = new InventoryItem.LordBritishArtifact(InventoryItem.LordBritishArtifact.ArtifactType.Crown,
                gameStateByteArray[0x20E], 
                dataOvlRef.StringReferences.GetString(DataOvlReference.SPECIAL_ITEM_NAMES_STRINGS.CROWN),
                dataOvlRef.StringReferences.GetString(DataOvlReference.WEAR_USE_ITEM_STRINGS.DON_THE_CROWN));
        }
    }

}
