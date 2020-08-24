using System.Collections.Generic;
using Ultima5Redux.DayNightMoon;

//using static Ultima5Redux.NonPlayerCharacterReferences;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters
{
    public partial class NonPlayerCharacterReference
    {
        public class NonPlayerCharacterSchedule
        {
            public enum AiType { Fixed = 0, Wander = 1, BigWander = 2, ChildRunAway = 3, MerchantThing = 4, ExtortOrAttackOrFollow = 6, DrudgeWorthThing = 7 }

            /// <summary>
            /// Get the index of the scheduled based on the specified time of day
            /// </summary>
            /// <param name="timeOfDay"></param>
            /// <returns></returns>
            internal int GetScheduleIndex(TimeOfDay timeOfDay)
            {
                int getIndex(int nOrigIndex)
                {
                    return nOrigIndex == 3 ? 1 : nOrigIndex;
                }

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
                if (nHour > Times[nLatestTimeIndex]) return getIndex(nLatestTimeIndex);// == 3 ? 1: nLatestTimeIndex;

                throw new Ultima5ReduxException("GetScheduleIndex fell all the way through which doesn't make sense.");
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
                if (nHour > Times[nLatestTimeIndex]) return nLatestTimeIndex;// == 3 ? 1: nLatestTimeIndex;

                throw new Ultima5ReduxException("GetRawScheduleIndex fell all the way through which doesn't make sense.");
            }

            /// <summary>
            /// Gets the index of the earliest time in the daily schedule
            /// </summary>
            /// <returns></returns>
            private int GetEarliestTimeIndex()
            {
                int nEarliest = Times[0];
                int nEarliestIndex = 0;
                for (int i = 1; i < Times.Count; i++)
                {
                    if (Times[i] < nEarliest)
                    {
                        nEarliestIndex = i;
                        nEarliest = Times[i];
                    }
                }
                return nEarliestIndex;
            }

            /// <summary>
            /// Gets the index of the latest time in the daily schedule
            /// </summary>
            /// <returns></returns>
            private int GetLatestTimeIndex()
            {
                int nLargest = Times[0];
                int nLargestIndex = 0;
                for (int i = 1; i < Times.Count; i++)
                {
                    if (Times[i] > nLargest)
                    {
                        nLargestIndex = i;
                        nLargest = Times[i];
                    }
                }
                return nLargestIndex;
            }

            public AiType GetCharacterAiTypeByTime(TimeOfDay timeOfDay)
            {
                int nIndex = GetScheduleIndex(timeOfDay);

                return (AiType)_aiTypeList[nIndex];
            }

            /// <summary>
            /// Gets the schedule previous to the current one
            /// Often used for figuring out what floor an NPC would come from
            /// </summary>
            /// <param name="timeOfDay"></param>
            /// <returns></returns>
            public CharacterPosition GetCharacterPreviousPositionByTime(TimeOfDay timeOfDay)
            {
                CharacterPosition characterPosition = new CharacterPosition();
                int nIndex = GetRawScheduleIndex(timeOfDay);

                //return nOrigIndex == 3 ? 1 : nOrigIndex;

                //if (nHour > Times[3] && nHour < Times[0]) return 1;
                //if (nHour > Times[0] && nHour < Times[1]) return 0;
                //if (nHour > Times[1] && nHour < Times[2]) return 1;
                //if (nHour > Times[2] && nHour < Times[3]) return 2;

                
                if (nIndex == 0) nIndex = 1;
                else if (nIndex == 1) nIndex = 0;
                else if (nIndex == 2) nIndex = 1;
                else if (nIndex == 3) nIndex = 2;

                characterPosition.Floor = GetFloor(nIndex);
                characterPosition.XY = GetXY(nIndex);

                return characterPosition;
            }

            /// <summary>
            /// Gets the characters preferred/default position based on the time of day
            /// </summary>
            /// <param name="timeOfDay"></param>
            /// <returns></returns>
            public CharacterPosition GetCharacterDefaultPositionByTime(TimeOfDay timeOfDay)
            {
                CharacterPosition characterPosition = new CharacterPosition();
                int nIndex = GetScheduleIndex(timeOfDay);

                characterPosition.Floor = GetFloor(nIndex);
                characterPosition.XY = GetXY(nIndex);

                return characterPosition;
            }

            private Point2D GetXY(int nIndex)
            {
                return (new Point2D(Coords[nIndex].X, Coords[nIndex].Y));
            }

            private int GetFloor(int nIndex)
            {
                return Coords[nIndex].Z;
            }

            /// <summary>
            /// TODO: Need to figure out what these AI types actually mean
            /// </summary>
            private readonly List<byte> _aiTypeList = new List<byte>();
            /// <summary>
            /// 3D Coordinates including floor number
            /// </summary>
            private List<Point3D> Coords { get; }
            /// <summary>
            /// Times of day to move to the next scheduled item
            /// </summary>
            private List<byte> Times { get; }

            /// <summary>
            /// Creates an NPC Schedule object 
            /// This is easier to consume than the structure
            /// </summary>
            /// <param name="schedule"></param>
            public NonPlayerCharacterSchedule(NPCSchedule schedule)
            {
                Coords = new List<Point3D>();
                Times = new List<byte>();

                unsafe
                {
                    for (int i = 0; i < 3; i++)
                    {
                        _aiTypeList.Add(schedule.AI_types[i]);
                        Coords.Add(new Point3D(schedule.x_coordinates[i], schedule.y_coordinates[i], schedule.z_coordinates[i]));
                        if (schedule.z_coordinates[i] != 0) { System.Console.Write(""); }
                    }
                    // argh, I can't get the size dynamically of the arrays
                    for (int i = 0; i < 4; i++)
                    {
                        Times.Add(schedule.times[i]);
                    }
                }
            }
        }
    }
}
