using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class ProvisionQuantityChanged : TurnResult, IQuantityChanged
    {
        public ProvisionReferences.SpecificProvisionType TheProvision { get; }

        public ProvisionQuantityChanged(int adjustedBy, int previousQuantity,
            ProvisionReferences.SpecificProvisionType theProvision) :
            base(TurnResultType.ProvisionQuantityChanged)
        {
            AdjustedBy = adjustedBy;
            PreviousQuantity = previousQuantity;
            TheProvision = theProvision;
        }

        public int AdjustedBy { get; }
        public int PreviousQuantity { get; }
    }
}