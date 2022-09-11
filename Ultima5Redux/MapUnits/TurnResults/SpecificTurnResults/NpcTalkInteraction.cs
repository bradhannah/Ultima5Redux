using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class NpcTalkInteraction : TurnResult
    {
        public NonPlayerCharacter Npc { get; }

        public NpcTalkInteraction(NonPlayerCharacter npc) : base(TurnResultType.NpcTalkInteraction)
        {
            Npc = npc;
        }
    }
}