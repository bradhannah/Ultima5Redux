using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class NpcTalkInteraction : TurnResult, INonPlayerCharacterInteraction
    {
        public NonPlayerCharacter NPC { get; }

        public NpcTalkInteraction(NonPlayerCharacter npc) : base(TurnResultType.NpcTalkInteraction) => NPC = npc;
    }
}