using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;

namespace Ultima5Redux.State {
    [DataContract] public class ShrineState {
        public enum ShrineStatus {
            QuestNotStarted = 0, ShrineOrdainedNoCodex = 1, ShrineOrdainedWithCodex = 2, ShrineCompleted = 3
        }

        // [DataMember] public bool ShrineQuestCompleted { get; set; }
        [DataMember] public ShrineStatus TheShrineStatus { get; set; }
        [DataMember] public bool IsShrineDestroyed { get; set; }

        public VirtueReference.VirtueType VirtueType { get; private set; }

        [JsonConstructor] public ShrineState() {
        }

        public ShrineState(VirtueReference.VirtueType virtueType, bool bShrineOrdainedBitSet,
            bool bShrineCodexVisitedBitSet, bool bIsShrineDestroyed) {
            VirtueType = virtueType;
            IsShrineDestroyed = bIsShrineDestroyed;

            if (!bShrineOrdainedBitSet && !bShrineCodexVisitedBitSet) {
                TheShrineStatus = ShrineStatus.QuestNotStarted;
            }
            else if (bShrineOrdainedBitSet && !bShrineCodexVisitedBitSet) {
                TheShrineStatus = ShrineStatus.ShrineOrdainedNoCodex;
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            }
            else if (bShrineOrdainedBitSet && bShrineCodexVisitedBitSet) {
                TheShrineStatus = ShrineStatus.ShrineOrdainedWithCodex;
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            }
            else if (!bShrineOrdainedBitSet && bShrineCodexVisitedBitSet) {
                TheShrineStatus = ShrineStatus.ShrineCompleted;
            }
        }
    }

    [DataContract] public class ShrineStates {
        [DataMember(Name = "Shrines")] private Dictionary<VirtueReference.VirtueType, ShrineState> _shrines = new();

        private const int TOTAL_VIRTUES = 8;

        [JsonConstructor] public ShrineStates() {
        }

        public ShrineState GetNextOrdainedButNotWithCodex() {
            return _shrines.Values.FirstOrDefault(shrine =>
                shrine.TheShrineStatus == ShrineState.ShrineStatus.ShrineOrdainedNoCodex);
        }

        public ShrineState GetNextOrdainedWithCodex() {
            return _shrines.Values.FirstOrDefault(shrine =>
                shrine.TheShrineStatus == ShrineState.ShrineStatus.ShrineOrdainedWithCodex);
        }

        
        public ShrineState GetShrineStateByVirtue(VirtueReference.VirtueType virtueType) => _shrines[virtueType];

        public ShrineStates(List<bool> shrinesOrdained, List<bool> shrinesWithCodexVisited,
            List<byte> destroyedShrines) {
            for (int nShrine = 0; nShrine < TOTAL_VIRTUES; nShrine++) {
                var virtue = (VirtueReference.VirtueType)nShrine;
                var shrineState = new ShrineState(virtue, shrinesOrdained[nShrine], shrinesWithCodexVisited[nShrine],
                    (destroyedShrines[nShrine] & 0x40) > 0);
                // {
                //     // shrine completion is in a single byte, with each bit representing a shrine
                //     ShrineQuestCompleted = shrineQuestionCompletions[nShrine],
                //     // shrine destruction is tracked per byte - how odd!? and only stored in 7th bit
                //     IsShrineDestroyed = (destroyedShrines[nShrine] & 0x40) > 0
                // };
                _shrines.Add(virtue, shrineState);
            }
        }

        public bool AreNoQuestsStartedYet =>
            _shrines.All(s => s.Value.TheShrineStatus == ShrineState.ShrineStatus.QuestNotStarted);

        public bool AtLeastOneOrdainedNoCodex =>
            _shrines.Any(s => s.Value.TheShrineStatus == ShrineState.ShrineStatus.ShrineOrdainedNoCodex);

        public bool AtLeastOneOrdainedWithCodex =>
            _shrines.Any(s => s.Value.TheShrineStatus == ShrineState.ShrineStatus.ShrineOrdainedWithCodex);

        public bool AllShrinesCompleted =>
            _shrines.All(s => s.Value.TheShrineStatus == ShrineState.ShrineStatus.ShrineCompleted);
    }
}