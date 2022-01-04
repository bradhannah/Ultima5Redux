using System.Collections.Generic;
using Ultima5Redux.References.Maps;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class BarKeeperStockReference
    {
        public enum DrinkType { Ale, Rum, Stout, Wine }

        public enum FoodType { Mutton, Boar, Fruit, Cheese, Rations }

        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, BarKeeperStock>
            _barKeeperStockByLocation = new()
            {
                {
                    SmallMapReferences.SingleMapReference.Location.Moonglow,
                    new BarKeeperStock(FoodType.Mutton, 3, DrinkType.Ale, 1, true, 20)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.Britain,
                    new BarKeeperStock(FoodType.Mutton, 4, DrinkType.Ale, 1, true, 30)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.Jhelom,
                    new BarKeeperStock(FoodType.Mutton, 5, DrinkType.Ale, 1, true, 40)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.Yew,
                    new BarKeeperStock(FoodType.Boar, 3, DrinkType.Rum, 1, false, 0)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.New_Magincia,
                    new BarKeeperStock(FoodType.Fruit, 2, DrinkType.Stout, 1, true, 60)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.West_Britanny,
                    new BarKeeperStock(FoodType.Cheese, 5, DrinkType.Wine, 1, false, 0)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.Paws,
                    new BarKeeperStock(FoodType.Mutton, 3, DrinkType.Ale, 1, true, 40)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.Buccaneers_Den,
                    new BarKeeperStock(FoodType.Boar, 4, DrinkType.Rum, 1, false, 0)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.Lycaeum,
                    new BarKeeperStock(FoodType.Mutton, 5, DrinkType.Ale, 1, true, 60)
                }
            };

        public static int GetAdjustedPrice(int nIntelligence, int nPrice)
        {
            return (int)(nPrice - nPrice * 0.015 * nIntelligence);
        }

        public BarKeeperStock GetBarKeeperStock(SmallMapReferences.SingleMapReference.Location location)
        {
            if (!_barKeeperStockByLocation.ContainsKey(location))
                throw new Ultima5ReduxException("Asked for BarKeeperStock from " + location +
                                                " but that location is not listed.");

            return _barKeeperStockByLocation[location];
        }

        public class BarKeeperStock
        {
            public int DrinkPrice { get; }
            public DrinkType DrinkType { get; }
            public int FoodPrice { get; }
            public FoodType FoodType { get; }
            public int RationPrice { get; }
            public bool Rations { get; }

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