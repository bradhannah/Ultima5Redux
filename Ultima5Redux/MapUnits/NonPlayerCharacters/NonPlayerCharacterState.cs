using System.Runtime.Serialization;
using Ultima5Redux.Dialogue;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    [DataContract]
    public class NonPlayerCharacterState
    {
        [IgnoreDataMember] private bool _bHasMetAvatar = false;

        [IgnoreDataMember] public NonPlayerCharacterReference NPCRef { get; }
        [DataMember] public int NPCRefIndex => NPCRef.DialogIndex;
        [DataMember] public bool IsDead { get; set; }
        
        [DataMember] public bool HasMetAvatar
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

        public NonPlayerCharacterState(NonPlayerCharacterReference npcRef)
        {
            NPCRef = npcRef;
        }
        
    }
}