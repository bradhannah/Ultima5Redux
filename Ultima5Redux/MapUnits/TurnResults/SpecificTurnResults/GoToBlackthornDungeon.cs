using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class GoToBlackthornDungeon : TurnResult, INonPlayerCharacterInteraction
    {
        public GoToBlackthornDungeon(NonPlayerCharacter npc) : base(TurnResultType.GoToBlackthornDungeon) => Npc = npc;

        public NonPlayerCharacter Npc { get; }
    }
}