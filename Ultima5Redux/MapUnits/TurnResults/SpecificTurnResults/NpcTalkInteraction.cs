using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class NpcTalkInteraction : TurnResult, INonPlayerCharacterInteraction
    {
        public readonly string CustomDialogueId = "";

        public NpcTalkInteraction(NonPlayerCharacter npc) : base(TurnResultType.NpcTalkInteraction) => Npc = npc;

        public NpcTalkInteraction(NonPlayerCharacter npc, string customDialogueId) : base(TurnResultType
            .NpcTalkInteraction)
        {
            CustomDialogueId = customDialogueId;
            Npc = npc;
        }

        public NonPlayerCharacter Npc { get; }
    }
}