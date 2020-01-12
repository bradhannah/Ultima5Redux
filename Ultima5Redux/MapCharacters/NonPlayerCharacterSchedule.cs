using System.Collections.Generic;
//using static Ultima5Redux.NonPlayerCharacterReferences;

namespace Ultima5Redux
{
    public partial class NonPlayerCharacterReference
    {
        public class NonPlayerCharacterSchedule
        {
            private int GetScheduleIndex(TimeOfDay timeOfDay)
            {
                return 1;
            }

            public CharacterPosition GetCharacterDefaultPositionByTime(TimeOfDay timeOfDay)
            {
                CharacterPosition characterPosition = new CharacterPosition();
                int nIndex = GetScheduleIndex(timeOfDay);
                return characterPosition;
            }

            public Point2D GetHardCoord(int nIndex)
            {
                return (new Point2D(Coords[nIndex].X, Coords[nIndex].Y));
            }

            public int GetFloor(int nIndex)
            {
                return Coords[nIndex].Z;
            }

            /// <summary>
            /// TODO: Need to figure out what these AI types actually mean
            /// </summary>
            public List<byte> AIType = new List<byte>();
            /// <summary>
            /// 3D Coordinates including floor number
            /// </summary>
            public List<Point3D> Coords { get; }
            /// <summary>
            /// Times of day to move to the next scheduled item
            /// TODO: figure out why there are 4 times, but only three xyz's to go to?!
            /// </summary>
            public List<byte> Times { get; }

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
                        AIType.Add(sched.AI_types[i]);
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
