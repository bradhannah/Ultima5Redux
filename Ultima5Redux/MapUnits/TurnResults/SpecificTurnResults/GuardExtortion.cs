using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class GuardExtortion : TurnResult, INonPlayerCharacterInteraction
    {
        public enum ExtortionType { Generic, HalfGold }

        public GuardExtortion(NonPlayerCharacter npc, ExtortionType theExtortionType) : base(TurnResultType
            .GuardExtortion)
        {
            NPC = npc;
            TheExtortionType = theExtortionType;
        }

        public NonPlayerCharacter NPC { get; }
        public ExtortionType TheExtortionType { get; }
    }
}