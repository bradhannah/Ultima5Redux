using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class AttemptToArrest : TurnResult, INonPlayerCharacterInteraction
    {
        public AttemptToArrest(TurnResultType theTurnResultType, NonPlayerCharacter npc) : base(theTurnResultType,
            TurnResulActionType.ActionRequired) =>
            Npc = npc;

        public NonPlayerCharacter Npc { get; }
    }
}