using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.References;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public sealed class LordBritishArtifacts : InventoryItems<LordBritishArtifact.ArtifactType, LordBritishArtifact>
    {
        private enum Offsets { AMULET = 0x20D, CROWN = 0x20E, SCEPTRE = 0x20F }

        public override Dictionary<LordBritishArtifact.ArtifactType, LordBritishArtifact> Items { get; } =
            new Dictionary<LordBritishArtifact.ArtifactType, LordBritishArtifact>(3);

        public LordBritishArtifacts(List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
            Items[LordBritishArtifact.ArtifactType.Amulet] = new LordBritishArtifact(
                LordBritishArtifact.ArtifactType.Amulet,
                gameStateByteArray[(int)Offsets.AMULET],
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings
                    .WEARING_AMULET));
            Items[LordBritishArtifact.ArtifactType.Crown] = new LordBritishArtifact(
                LordBritishArtifact.ArtifactType.Crown,
                gameStateByteArray[(int)Offsets.CROWN],
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings
                    .DON_THE_CROWN));
            Items[LordBritishArtifact.ArtifactType.Sceptre] = new LordBritishArtifact(
                LordBritishArtifact.ArtifactType.Sceptre,
                gameStateByteArray[(int)Offsets.SCEPTRE],
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.WearUseItemStrings
                    .WIELD_SCEPTRE));
        }
    }
}