using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ShoppeKeeperInteraction : TurnResult
    {
        public ShoppeKeeper ShoppeKeeper { get; }

        public ShoppeKeeperInteraction(ShoppeKeeper shoppeKeeper) :
            base(TurnResultType.ActionShoppeKeeperInteraction, TurnResulActionType.ActionRequired) =>
            ShoppeKeeper = shoppeKeeper;
    }
}