using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class GuardExtortion : TurnResult, INonPlayerCharacterInteraction
    {
        public enum ExtortionType { Generic, HalfGold, BlackthornPassword }

        public GuardExtortion(NonPlayerCharacter npc, ExtortionType theExtortionType, int extortionAmount) :
            base(TurnResultType.GuardExtortion)
        {
            NPC = npc;
            TheExtortionType = theExtortionType;
            ExtortionAmount = extortionAmount;
        }

        public NonPlayerCharacter NPC { get; }
        public ExtortionType TheExtortionType { get; }

        public int ExtortionAmount { get; }
    }
}