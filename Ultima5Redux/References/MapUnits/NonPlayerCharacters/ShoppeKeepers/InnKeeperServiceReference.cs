﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    public class InnKeeperServiceReference
    {
        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location, InnKeeperServices>
            _innKeeperServices;

        public InnKeeperServiceReference(DataOvlReference dataOvlReference) =>
            _innKeeperServices = new Dictionary<SmallMapReferences.SingleMapReference.Location, InnKeeperServices>
            {
                {
                    SmallMapReferences.SingleMapReference.Location.Britain,
                    new InnKeeperServices(0, 186, 4, 40, dataOvlReference)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.Jhelom,
                    new InnKeeperServices(1, 187, 6, 60, dataOvlReference)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.Skara_Brae,
                    new InnKeeperServices(2, 188, 4, 40, dataOvlReference)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.North_Britanny,
                    new InnKeeperServices(3, 188, 6, 60, dataOvlReference)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.Paws,
                    new InnKeeperServices(4, 189, 4, 40, dataOvlReference)
                },
                {
                    SmallMapReferences.SingleMapReference.Location.Buccaneers_Den,
                    new InnKeeperServices(5, 190, 6, 60, dataOvlReference)
                }
            };

        public InnKeeperServices GetInnKeeperServicesByLocation(SmallMapReferences.SingleMapReference.Location location)
        {
            Debug.Assert(_innKeeperServices.ContainsKey(location));

            return _innKeeperServices[location];
        }

        public class InnKeeperServices
        {
            public int DialogueOfferIndex { get; }
            public int MonthlyLeaveCost { get; }
            public int RestCost { get; }

            [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
            [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
            public Point2D SleepingPosition { get; }

            internal InnKeeperServices(int nOffset, int nDialogueOfferIndex, int nRestCost, int nMonthlyLeaveCost,
                DataOvlReference dataOvlReference)
            {
                List<byte> xBedList = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.INN_BED_X_COORDS)
                    .GetAsByteList();
                List<byte> yBedList = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.INN_BED_Y_COORDS)
                    .GetAsByteList();

                DialogueOfferIndex = nDialogueOfferIndex;
                RestCost = nRestCost;
                MonthlyLeaveCost = nMonthlyLeaveCost;
                SleepingPosition = new Point2D(xBedList[nOffset], yBedList[nOffset]);
            }
        }
    }
}