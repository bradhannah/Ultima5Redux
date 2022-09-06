using System.Diagnostics;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class WhoToldMeToDoThis : TurnResult, IStackTrace
    {
        public WhoToldMeToDoThis(TurnResultType theTurnResultType, StackTrace theStacktrace) : base(theTurnResultType)
        {
            TheStacktrace = theStacktrace;
        }

        public StackTrace TheStacktrace { get; }
    }
}