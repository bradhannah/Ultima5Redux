using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public sealed class LordBritishArtifacts : InventoryItems <LordBritishArtifact.ArtifactType, LordBritishArtifact>
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