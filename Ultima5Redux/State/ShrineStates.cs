using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;

namespace Ultima5Redux.State {
    [DataContract] public class ShrineState {
        [DataMember] public bool ShrineQuestCompleted { get; set; }
        [DataMember] public bool ShrineDestroyed { get; set; }

        public VirtueReference.VirtueType VirtueType { get; private set; }

        public ShrineState(VirtueReference.VirtueType virtueType) => VirtueType = virtueType;
    }

    [DataContract] public class ShrineStates {
        [DataMember] private Dictionary<VirtueReference.VirtueType, ShrineState> _shrines = new();

        private const int TOTAL_VIRTUES = 8;

        [JsonConstructor] public ShrineStates() {
        }

        public ShrineStates(List<bool> shrineQuestionCompletions, List<byte> destroyedShrines) {
            for (int nShrine = 0; nShrine < TOTAL_VIRTUES; nShrine++) {
                var virtue = (VirtueReference.VirtueType)nShrine;
                var shrineState = new ShrineState(virtue) {
                    // shrine completion is in a single byte, with each bit representing a shrine
                    ShrineQuestCompleted = shrineQuestionCompletions[nShrine],
                    // shrine destruction is tracked per byte - how odd!? and only stored in 7th bit
                    ShrineDestroyed = (destroyedShrines[nShrine] & 0x40) > 0
                };
                _shrines.Add(virtue, shrineState);
            }
        }
    }
}