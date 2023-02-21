using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.Dialogue;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    [DataContract] public sealed class NonPlayerCharacterState
    {
        [DataMember] internal bool HasExtortedAvatar { get; set; }
        [DataMember] internal bool OverrideAiType { get; private set; }
        [DataMember] internal int PissedOffCountDown { get; set; } = -1;

        [DataMember]
        public bool HasMetAvatar
        {
            get
            {
                if (NpcRef.Script == null) return false;
                int nScriptLines = NpcRef.Script.NumberOfScriptLines;

                // two steps - first if the NPC Has met flag is flipped in saved.gam then we know they have met the Avatar
                // secondly, if the AskName command is not present in their entire script, then we can surmise that they must already know the Avatar (from the old days)

                if (_bHasMetAvatar) return true;

                for (int i = 0; i < nScriptLines; i++)
                {
                    if (NpcRef.Script.GetScriptLine(i).ContainsCommand(TalkScript.TalkCommand.AskName)) return false;
                }

                return true;
            }
            set => _bHasMetAvatar = value;
        }

        [DataMember] public bool IsDead { get; set; }
        [DataMember] public SmallMapReferences.SingleMapReference.Location NpcLocation { get; private set; }
        [DataMember] public int NpcRefIndex { get; private set; }

        [DataMember]
        public NonPlayerCharacterSchedule.AiType OverridenAiType { get; private set; } =
            NonPlayerCharacterSchedule.AiType.Fixed;

        [IgnoreDataMember] private bool _bHasMetAvatar;

        [IgnoreDataMember]
        public NonPlayerCharacterReference NpcRef
        {
            get =>
                _npcRefOverride ??
                GameReferences.Instance.NpcRefs.GetNonPlayerCharactersByLocation(NpcLocation)[NpcRefIndex];
            private set
            {
                NpcRefIndex = value.DialogIndex;
                NpcLocation = value.MapLocation;
            }
        }

        private readonly NonPlayerCharacterReference _npcRefOverride;

        [JsonConstructor] private NonPlayerCharacterState()
        {
        }

        public NonPlayerCharacterState(NonPlayerCharacterReference npcRef, bool bTemporaryReference = false)
        {
            if (bTemporaryReference)
            {
                // there is a special condition for temporary references.
                // because the class saves index values instead of the npc reference for the sake of saving files
                // if it's temporary (like a wishing well) then we will add an override
                _npcRefOverride = npcRef;
            }
            else
            {
                NpcRef = npcRef;
            }
        }

        public void OverrideAi(NonPlayerCharacterSchedule.AiType aiType)
        {
            OverrideAiType = true;
            OverridenAiType = aiType;
        }

        public void UnsetOverridenAi()
        {
            OverrideAiType = false;
        }
    }
}