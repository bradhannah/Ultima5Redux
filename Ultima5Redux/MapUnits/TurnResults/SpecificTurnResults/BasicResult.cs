namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class BasicResult : TurnResult
    {
        public BasicResult(TurnResultType theTurnResultType) : base(theTurnResultType, TurnResulActionType.Unsure)
        {
        }
    }
}