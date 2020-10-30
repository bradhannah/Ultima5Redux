using System.Collections.Generic;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class BarKeeperStockReference
    {
        public enum FoodType {Mutton, Boar, Fruit, Cheese, Rations}
        public enum DrinkType {Ale, Rum, Stout, Wine}

        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, BarKeeperStock> _barKeeperStockByLocation =
            new Dictionary<SmallMapReferences.SingleMapReference.Location, BarKeeperStock>()
            {
                {SmallMapReferences.SingleMapReference.Location.Moonglow, new BarKeeperStock(FoodType.Mutton, 3, DrinkType.Ale, 1, true, 20)},
                {SmallMapReferences.SingleMapReference.Location.Britain, new BarKeeperStock(FoodType.Mutton, 4, DrinkType.Ale, 1, true, 30)},
                {SmallMapReferences.SingleMapReference.Location.Jhelom, new BarKeeperStock(FoodType.Mutton, 5, DrinkType.Ale, 1, true, 40)},
                {SmallMapReferences.SingleMapReference.Location.Yew, new BarKeeperStock(FoodType.Boar, 3, DrinkType.Rum, 1, false, 0)},
                {SmallMapReferences.SingleMapReference.Location.New_Magincia, new BarKeeperStock(FoodType.Fruit, 2, DrinkType.Stout, 1, true, 60)},
                {SmallMapReferences.SingleMapReference.Location.West_Britanny, new BarKeeperStock(FoodType.Cheese, 5, DrinkType.Wine, 1, false, 0 )},
                {SmallMapReferences.SingleMapReference.Location.Paws, new BarKeeperStock(FoodType.Mutton, 3, DrinkType.Ale, 1, true, 40)},
                {SmallMapReferences.SingleMapReference.Location.Buccaneers_Den, new BarKeeperStock(FoodType.Boar, 4, DrinkType.Rum, 1, false, 0)},
                {SmallMapReferences.SingleMapReference.Location.Lycaeum, new BarKeeperStock(FoodType.Mutton, 5, DrinkType.Ale, 1, true, 60)}
            };

        public BarKeeperStock GetBarKeeperStock(SmallMapReferences.SingleMapReference.Location location)
        {
            if (!_barKeeperStockByLocation.ContainsKey(location))
            {
                throw new Ultima5ReduxException("Asked for BarKeeperStock from "+location+" but that location is not listed.");
            }

            return _barKeeperStockByLocation[location];
        }

        public static int GetAdjustedPrice(int nIntelligence, int nPrice)
        {
            return (int) (nPrice - (nPrice * 0.015 * nIntelligence));
        }
        
        public class BarKeeperStock
        {
            public FoodType FoodType { get; }
            public int FoodPrice { get; }
            public DrinkType DrinkType { get; }
            public int DrinkPrice { get; }
            public bool Rations { get; }
            public int RationPrice { get; }

            public BarKeeperStock(FoodType foodType, int nFoodPrice, DrinkType drinkType, int nDrinkPrice,
                bool bRations, int nRationPrice)
            {
                FoodType = foodType;
                FoodPrice = nFoodPrice;
                DrinkType = drinkType;
                DrinkPrice = nDrinkPrice;
                Rations = bRations;
                RationPrice = nRationPrice;
            }
        }
    }
}