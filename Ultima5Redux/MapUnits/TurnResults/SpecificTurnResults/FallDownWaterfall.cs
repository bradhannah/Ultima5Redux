using System.Diagnostics.CodeAnalysis;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class FallDownWaterfall : TurnResult
    {
        public enum FallDownWaterfallVariant { Underworld, Normal }

        public Point2D StartingPosition { get; }
        public Point2D WaterfallPosition { get; }
        public Point2D FinalPosition { get; }

        public FallDownWaterfallVariant FallDownVariant { get; }

        public FallDownWaterfall(FallDownWaterfallVariant fallDownWaterfallVariant, Point2D startingPosition) : base(
            fallDownWaterfallVariant == FallDownWaterfallVariant.Normal
                ? TurnResultType.FallDownWaterfallVariant_Normal
                : TurnResultType.FallDownWaterfallVariant_Underworld, TurnResulActionType.ActionRequired)
        {
            StartingPosition = startingPosition;
            WaterfallPosition = startingPosition.GetAdjustedPosition(Point2D.Direction.Down);
            FinalPosition = WaterfallPosition.GetAdjustedPosition(Point2D.Direction.Down);
            FallDownVariant = fallDownWaterfallVariant;
        }
    }
}