using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.Dialogue;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    [DataContract] public class NonPlayerCharacterState
    {
        [DataMember]
        public bool HasMetAvatar
        {
            get
            {
                if (NPCRef.Script == null) return false;
                int nScriptLines = NPCRef.Script.NumberOfScriptLines;

                // two steps - first if the NPC Has met flag is flipped in saved.gam then we know they have met the Avatar
                // secondly, if the AskName command is not present in their entire script, then we can surmise that they must already know the Avatar (from the old days)

                if (_bHasMetAvatar) return true;

                for (int i = 0; i < nScriptLines; i++)
                {
                    if (NPCRef.Script.GetScriptLine(i).ContainsCommand(TalkScript.TalkCommand.AskName)) return false;
                }

                return true;
            }
            set => _bHasMetAvatar = value;
        }

        [DataMember] public bool IsDead { get; set; }
        [DataMember] public SmallMapReferences.SingleMapReference.Location NPCLocation { get; private set; }
        [DataMember] public int NPCRefIndex { get; private set; }

        [IgnoreDataMember] private bool _bHasMetAvatar;

        [IgnoreDataMember]
        public NonPlayerCharacterReference NPCRef
        {
            get => GameReferences.NpcRefs.GetNonPlayerCharactersByLocation(NPCLocation)[NPCRefIndex];
            private set
            {
                NPCRefIndex = value.DialogIndex;
                NPCLocation = value.MapLocation;
            }
        }

        [JsonConstructor] private NonPlayerCharacterState()
        {
        }

        public NonPlayerCharacterState(NonPlayerCharacterReference npcRef) => NPCRef = npcRef;
    }
}