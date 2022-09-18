namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class KarmaChanged : TurnResult, IQuantityChanged
    {
        public KarmaChanged(int adjustedBy, int previousQuantity) : base(TurnResultType.KarmaChanged)
        {
            AdjustedBy = adjustedBy;
            PreviousQuantity = previousQuantity;
        }

        public int AdjustedBy { get; }
        public int PreviousQuantity { get; }
    }
}