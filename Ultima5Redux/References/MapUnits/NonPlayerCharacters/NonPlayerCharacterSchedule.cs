using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.MapUnits;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.MapUnits.NonPlayerCharacters
{
    public class NonPlayerCharacterSchedule
    {
        public enum AiType
        {
            // stay put
            Fixed = 0,

            // wander a small radius
            Wander = 1,

            // wander a wide radius
            BigWander = 2,

            // children are playful and run away from you
            ChildRunAway = 3,

            // special conversation for merchants
            CustomAi = 4,

            // you better pay the guard or they will attack!
            ExtortOrAttackOrFollow = 6,

            // he's a jerk - he will attack at first opportunity, and get as close to you as possible
            DrudgeWorthThing = 7,

            // horses wander if they aren't tied up
            HorseWander = 100,

            // people who are freed walk close to you and are annoying - but won't talk
            FollowAroundAndBeAnnoyingThenNeverSeeAgain = 101,

            // they will wander in a small radius, but if they are next to you, then they will want to chat
            SmallWanderWantsToChat = 102,

            // beggars wander, and when next to Avatar they want to talk
            Begging = 103,

            // They generally ask for 30-60gp tribute
            GenericExtortingGuard = 104,

            // These jerks want half your gold! Generally found in Minoc
            HalfYourGoldExtortingGuard = 105,

            // Merchant that doesn't move
            MerchantBuyingSelling = 106,

            // I THINK this is a merchant that sells, but also wanders a bit
            MerchantBuyingSellingCustom = 107,

            // Merchants that wander their own store, but still sell
            MerchantBuyingSellingWander = 108,

            // They are Fixed unless wanted by the popo, at which time they seek and attack 
            FixedExceptAttackWhenIsWantedByThePoPo = 109, BlackthornGuardWander = 110, BlackthornGuardFixed = 111
        }


        /// <summary>
        /// </summary>
        public readonly List<byte> AiTypeList = new();

        /// <summary>
        ///     3D Coordinates including floor number
        /// </summary>
        private List<Point3D> Coords { get; }

        /// <summary>
        ///     Times of day to move to the next scheduled item
        /// </summary>
        private List<byte> Times { get; }

        /// <summary>
        ///     Creates an NPC Schedule object
        ///     This is easier to consume than the structure
        /// </summary>
        /// <param name="schedule"></param>
        public NonPlayerCharacterSchedule(NonPlayerCharacterReference.NpcSchedule schedule)
        {
            Coords = new List<Point3D>();
            Times = new List<byte>();

            unsafe
            {
                for (int i = 0; i < 3; i++)
                {
                    AiTypeList.Add(schedule.AI_types[i]);
                    Coords.Add(new Point3D(schedule.x_coordinates[i], schedule.y_coordinates[i],
                        schedule.z_coordinates[i]));
                    if (schedule.z_coordinates[i] != 0) Console.Write("");
                }

                // argh, I can't get the size dynamically of the arrays
                for (int i = 0; i < 4; i++)
                {
                    Times.Add(schedule.times[i]);
                }
            }
        }

        /// <summary>
        ///     This will review the scheduled AI types and give them more specific AI types
        ///     This simplifies later handling of their interactive behaviours
        /// </summary>
        /// <param name="location"></param>
        /// <param name="nonPlayerCharacterReference"></param>
        public void AdaptAiTypesByNpcRef(SmallMapReferences.SingleMapReference.Location location,
            NonPlayerCharacterReference nonPlayerCharacterReference)
        {
            for (int nIndex = 0; nIndex < AiTypeList.Count; nIndex++)
            {
                var aiType = (AiType)AiTypeList[nIndex];

                // These are all FULL overrides, that override every AI behaviour in the schedule
                const int STILLWELT_DIALOG_INDEX = 27;
                if (location == SmallMapReferences.SingleMapReference.Location.Lord_Britishs_Castle
                    && nonPlayerCharacterReference.DialogIndex == STILLWELT_DIALOG_INDEX)
                {
                    // this is Stillwelt, the mean guard
                    AiTypeList[nIndex] = (int)AiType.SmallWanderWantsToChat;
                    continue;
                }

                // the daemon's at Windemere are really like guards
                if (location == SmallMapReferences.SingleMapReference.Location.Windemere &&
                    nonPlayerCharacterReference.NPCKeySprite == (int)TileReference.SpriteIndex.Daemon1_KeyIndex)
                {
                    AiTypeList[nIndex] = (int)AiType.FixedExceptAttackWhenIsWantedByThePoPo;
                    continue;
                }

                if (location == SmallMapReferences.SingleMapReference.Location.Palace_of_Blackthorn
                    && nonPlayerCharacterReference.IsGuard)
                {
                    if (aiType == AiType.Fixed) AiTypeList[nIndex] = (int)AiType.BlackthornGuardFixed;
                    else if (aiType == AiType.CustomAi) AiTypeList[nIndex] = (int)AiType.BlackthornGuardWander;
                    else throw new Ultima5ReduxException($"Blackthorn Guard has odd aitype: {aiType}");

                    continue;
                }

                // In the future, such as Blackthorne's castle, this is where we will
                // will override AIs to have more specific 

                
                // Merchants always work on their 1 and 3 schedules, even though they are marked as 
                // Fixed (0)
                if (nonPlayerCharacterReference.IsShoppeKeeper && aiType == AiType.Fixed && nIndex is 1 or 3)
                {
                    AiTypeList[nIndex] = (int)AiType.MerchantBuyingSelling;
                    continue;
                }

                if (nonPlayerCharacterReference.IsShoppeKeeper && aiType == AiType.Wander && nIndex is 1 or 3)
                {
                    AiTypeList[nIndex] = (int)AiType.MerchantBuyingSellingWander;
                    continue;
                }

                if (nonPlayerCharacterReference.IsShoppeKeeper)
                {
                    Debug.Assert(aiType is AiType.Fixed or AiType.Wander or AiType.CustomAi);
                }

                if (aiType != AiType.CustomAi) continue;

                if (nonPlayerCharacterReference.IsGuard &&
                    location == SmallMapReferences.SingleMapReference.Location.Minoc)
                {
                    // the guard at Minoc doesn't take the regular amount - instead he wants HALF!
                    AiTypeList[nIndex] = (int)AiType.HalfYourGoldExtortingGuard;
                    continue;
                }

                if (nonPlayerCharacterReference.IsGuard)
                {
                    AiTypeList[nIndex] = (int)AiType.GenericExtortingGuard;
                    continue;
                }

                if (nonPlayerCharacterReference.NPCKeySprite == (int)TileReference.SpriteIndex.Beggar_KeyIndex)
                {
                    AiTypeList[nIndex] = (int)AiType.Begging;
                    continue;
                }

                if (nonPlayerCharacterReference.IsShoppeKeeper)
                {
                    AiTypeList[nIndex] = (int)AiType.MerchantBuyingSellingCustom;
                }
                else
                {
                    // just for quick debug
                    _ = "";
                }
                // Donn Piatt, Moonglow, DialogIndex==4 - a lord!?
                // Delwyn, Minoc, DialogIndex == 9 - in jail
                // gorn, DialogNumber==11 BT - in jail
                // Johne, Capatin Johne, Ararat, DialogIndex = 1 - stuck on a boat
                // Balinor, StoneGate, DialogIndex = 4 - jerky Deamon
                
            }
        }

        /// <summary>
        ///     Get the index of the scheduled based on the specified time of day
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        internal int GetScheduleIndex(TimeOfDay timeOfDay)
        {
            int getIndex(int nOrigIndex) => nOrigIndex == 3 ? 1 : nOrigIndex;

            int nHour = timeOfDay.Hour;

            // there are some characters who are apparently always in the exact same location
            if (Times[0] == 0 && Times[1] == 0 && Times[2] == 0 && Times[3] == 0) return 0;

            // if the hour matches, then we are good
            for (int i = 0; i < 4; i++)
            {
                if (Times[i] == nHour) return getIndex(i);
            }

            if (nHour > Times[3] && nHour < Times[0]) return 1;
            if (nHour > Times[0] && nHour < Times[1]) return 0;
            if (nHour > Times[1] && nHour < Times[2]) return 1;
            if (nHour > Times[2] && nHour < Times[3]) return 2;

            // what is the index of the time that is earliest
            int nEarliestTimeIndex = GetEarliestTimeIndex();
            // what is the index of the time before the time that is earliest
            int nIndexPreviousToEarliest = nEarliestTimeIndex == 0 ? 1 : nEarliestTimeIndex - 1;
            // the index of the index that has the latest time
            int nLatestTimeIndex = GetLatestTimeIndex();

            // if it less than the lowest value, then go to the index before the lowest value
            if (nHour < Times[nEarliestTimeIndex]) return nIndexPreviousToEarliest;
            // if it is more than the highest value, then go to the index of the highest value
            if (nHour > Times[nLatestTimeIndex]) return getIndex(nLatestTimeIndex);

            throw new Ultima5ReduxException("GetScheduleIndex fell all the way through which doesn't make sense.");
        }

        /// <summary>
        ///     Gets the index of the earliest time in the daily schedule
        /// </summary>
        /// <returns></returns>
        private int GetEarliestTimeIndex()
        {
            int nEarliest = Times[0];
            int nEarliestIndex = 0;
            for (int i = 1; i < Times.Count; i++)
            {
                if (Times[i] >= nEarliest) continue;

                nEarliestIndex = i;
                nEarliest = Times[i];
            }

            return nEarliestIndex;
        }

        private int GetFloor(int nIndex) => Coords[nIndex].Z;

        /// <summary>
        ///     Gets the index of the latest time in the daily schedule
        /// </summary>
        /// <returns></returns>
        private int GetLatestTimeIndex()
        {
            int nLargest = Times[0];
            int nLargestIndex = 0;
            for (int i = 1; i < Times.Count; i++)
            {
                if (Times[i] <= nLargest) continue;

                nLargestIndex = i;
                nLargest = Times[i];
            }

            return nLargestIndex;
        }

        private int GetRawScheduleIndex(TimeOfDay timeOfDay)
        {
            int nHour = timeOfDay.Hour;

            // there are some characters who are apparently always in the exact same location
            if (Times[0] == 0 && Times[1] == 0 && Times[2] == 0 && Times[3] == 0) return 0;

            // if the hour matches, then we are good
            for (int i = 0; i < 4; i++)
            {
                if (Times[i] == nHour) return i;
            }

            if (nHour > Times[3] && nHour < Times[0]) return 3;
            if (nHour > Times[0] && nHour < Times[1]) return 0;
            if (nHour > Times[1] && nHour < Times[2]) return 1;
            if (nHour > Times[2] && nHour < Times[3]) return 2;

            // what is the index of the time that is earliest
            int nEarliestTimeIndex = GetEarliestTimeIndex();
            // what is the index of the time before the time that is earliest
            int nIndexPreviousToEarliest = nEarliestTimeIndex == 0 ? 1 : nEarliestTimeIndex - 1;
            // the index of the index that has the latest time
            int nLatestTimeIndex = GetLatestTimeIndex();

            // if it less than the lowest value, then go to the index before the lowest value
            if (nHour < Times[nEarliestTimeIndex]) return nIndexPreviousToEarliest;
            // if it is more than the highest value, then go to the index of the highest value
            if (nHour > Times[nLatestTimeIndex]) return nLatestTimeIndex;

            throw new Ultima5ReduxException("GetRawScheduleIndex fell all the way through which doesn't make sense.");
        }

        private Point2D GetXY(int nIndex) => new(Coords[nIndex].X, Coords[nIndex].Y);

        public AiType GetCharacterAiTypeByTime(TimeOfDay timeOfDay)
        {
            int nIndex = GetScheduleIndex(timeOfDay);

            return (AiType)AiTypeList[nIndex];
        }

        /// <summary>
        ///     Gets the characters preferred/default position based on the time of day
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public MapUnitPosition GetCharacterDefaultPositionByTime(TimeOfDay timeOfDay)
        {
            MapUnitPosition mapUnitPosition = new();
            int nIndex = GetScheduleIndex(timeOfDay);

            mapUnitPosition.Floor = GetFloor(nIndex);
            mapUnitPosition.XY = GetXY(nIndex);

            return mapUnitPosition;
        }

        /// <summary>
        ///     Gets the schedule previous to the current one
        ///     Often used for figuring out what floor an NPC would come from
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public MapUnitPosition GetCharacterPreviousPositionByTime(TimeOfDay timeOfDay)
        {
            MapUnitPosition mapUnitPosition = new();
            int nIndex = GetRawScheduleIndex(timeOfDay);

            if (nIndex == 0) nIndex = 1;
            else if (nIndex == 1) nIndex = 0;
            else if (nIndex == 2) nIndex = 1;
            else if (nIndex == 3) nIndex = 2;

            mapUnitPosition.Floor = GetFloor(nIndex);
            mapUnitPosition.XY = GetXY(nIndex);

            return mapUnitPosition;
        }
    }
}