using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class InnKeeperServiceReference
    {
        public class InnKeeperServices
        {
            public SmallMapReferences.SingleMapReference.Location Location { get; }
            public int DialogueOfferIndex { get; }
            public int RestCost { get; }
            public int MonthlyLeaveCost { get; }

            public MapUnitPosition SleepingPosition { get; }
            
            internal InnKeeperServices(int nDialogueOfferIndex, int nRestCost, int nMonthlyLeaveCost, MapUnitPosition sleepingPosition)
            {
                DialogueOfferIndex = nDialogueOfferIndex;
                RestCost = nRestCost;
                MonthlyLeaveCost = nMonthlyLeaveCost;
                SleepingPosition = sleepingPosition;
            }
        }

        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, InnKeeperServices> _innKeeperServices =
            new Dictionary<SmallMapReferences.SingleMapReference.Location, InnKeeperServices>()
            {
                {SmallMapReferences.SingleMapReference.Location.Britain, new InnKeeperServices(186, 4, 40, new MapUnitPosition(21,10,0))},
                {SmallMapReferences.SingleMapReference.Location.Jhelom, new InnKeeperServices(187, 6, 60, new MapUnitPosition(15,7,1))},
                {SmallMapReferences.SingleMapReference.Location.Skara_Brae, new InnKeeperServices(188, 4, 40, new MapUnitPosition(25,9,0))},
                {SmallMapReferences.SingleMapReference.Location.North_Britanny, new InnKeeperServices(188, 6, 60, new MapUnitPosition(20,1,0))},
                {SmallMapReferences.SingleMapReference.Location.Paws, new InnKeeperServices(189, 4, 40, new MapUnitPosition(27,6,0))},
                {SmallMapReferences.SingleMapReference.Location.Buccaneers_Den, new InnKeeperServices(190, 6, 60, new MapUnitPosition(7,26,0))}
            };

        public InnKeeperServices GetInnKeeperServicesByLocation(SmallMapReferences.SingleMapReference.Location location)
        {
            Debug.Assert(_innKeeperServices.ContainsKey(location));

            return _innKeeperServices[location];
        }
    }
}