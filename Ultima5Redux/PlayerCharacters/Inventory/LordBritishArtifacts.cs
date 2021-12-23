using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract]
    public sealed class LordBritishArtifacts : InventoryItems<LordBritishArtifact.ArtifactType, LordBritishArtifact>
    {
        //private enum Offsets { AMULET = 0x20D, CROWN = 0x20E, SCEPTRE = 0x20F }

        [DataMember]
        public override Dictionary<LordBritishArtifact.ArtifactType, LordBritishArtifact> Items { get; internal set; } =
            new(3);

        [JsonConstructor] private LordBritishArtifacts()
        {
        }

        public LordBritishArtifacts(ImportedGameState importedGameState)
        {
            void addArtifactLegacy(LordBritishArtifact.ArtifactType artifact) => Items[artifact] =
                new LordBritishArtifact(artifact, importedGameState.GetLordBritishArtifactQuantity(artifact));

            addArtifactLegacy(LordBritishArtifact.ArtifactType.Amulet);
            addArtifactLegacy(LordBritishArtifact.ArtifactType.Crown);
            addArtifactLegacy(LordBritishArtifact.ArtifactType.Sceptre);

            // Items[LordBritishArtifact.ArtifactType.Amulet] = new LordBritishArtifact(
            //     LordBritishArtifact.ArtifactType.Amulet, gameStateByteArray[(int)Offsets.AMULET]);
            // Items[LordBritishArtifact.ArtifactType.Crown] = new LordBritishArtifact(
            //     LordBritishArtifact.ArtifactType.Crown, gameStateByteArray[(int)Offsets.CROWN]);
            // Items[LordBritishArtifact.ArtifactType.Sceptre] = new LordBritishArtifact(
            //     LordBritishArtifact.ArtifactType.Sceptre, gameStateByteArray[(int)Offsets.SCEPTRE]);
        }
    }
}