using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class ShoppeKeeperInteraction : TurnResult
    {
        public ShoppeKeeper ShoppeKeeper { get; }

        public ShoppeKeeperInteraction(ShoppeKeeper shoppeKeeper) : base(TurnResultType.ActionShoppeKeeperInteraction)
        {
            ShoppeKeeper = shoppeKeeper;
        }
    }
}