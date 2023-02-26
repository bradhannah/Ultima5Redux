using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class GuardExtortion : TurnResult, INonPlayerCharacterInteraction
    {
        public enum ExtortionType
        {
            Generic,
            HalfGold,
            BlackthornPassword
        }

        public int ExtortionAmount { get; }
        public ExtortionType TheExtortionType { get; }

        public GuardExtortion(NonPlayerCharacter npc, ExtortionType theExtortionType, int extortionAmount) :
            base(TurnResultType.GuardExtortion)
        {
            Npc = npc;
            TheExtortionType = theExtortionType;
            ExtortionAmount = extortionAmount;
        }

        public NonPlayerCharacter Npc { get; }
    }
}