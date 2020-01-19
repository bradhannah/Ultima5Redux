using System.Collections.Generic;
//using static Ultima5Redux.NonPlayerCharacterReferences;

namespace Ultima5Redux
{
    public partial class NonPlayerCharacterReference
    {
        public class NonPlayerCharacterSchedule
        {
            public enum AIType { Fixed = 0, Wander = 1, BigWander = 2, ChildRunAway = 3, MerchantThing = 4, ExtortOrAttackOrFollow = 6 }

            /// <summary>
            /// Get the index of the scheduled based on the specified time of day
            /// </summary>
            /// <param name="timeOfDay"></param>
            /// <returns></returns>
            private int GetScheduleIndex(TimeOfDay timeOfDay)
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

                throw new System.Exception("GetScheduleIndex fell all the way through which doesn't make sense.");
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

            public AIType GetCharacterAITypeByTime(TimeOfDay timeOfDay)
            {
                int nIndex = GetScheduleIndex(timeOfDay);

                return (AIType)AITypeList[nIndex];
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
            private List<byte> AITypeList = new List<byte>();
            /// <summary>
            /// 3D Coordinates including floor number
            /// </summary>
            private List<Point3D> Coords { get; }
            /// <summary>
            /// Times of day to move to the next scheduled item
            /// TODO: figure out why there are 4 times, but only three xyz's to go to?!
            /// </summary>
            private List<byte> Times { get; }

            /// <summary>
            /// Creates an NPC Schedule object 
            /// This is easier to consume than the structure
            /// </summary>
            /// <param name="sched"></param>
            public NonPlayerCharacterSchedule(NPC_Schedule sched)
            {
                Coords = new List<Point3D>();
                Times = new List<byte>();

                unsafe
                {
                    for (int i = 0; i < 3; i++)
                    {
                        AITypeList.Add(sched.AI_types[i]);
                        Coords.Add(new Point3D(sched.x_coordinates[i], sched.y_coordinates[i], sched.z_coordinates[i]));
                        if (sched.z_coordinates[i] != 0) { System.Console.Write(""); }
                    }
                    // argh, I can't get the size dynamically of the arrays
                    for (int i = 0; i < 4; i++)
                    {
                        Times.Add(sched.times[i]);
                    }
                }
            }
        }
    }
}
