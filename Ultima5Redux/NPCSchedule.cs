using System.Collections.Generic;

namespace Ultima5Redux
{
    public partial class NonPlayerCharacters
    {
        public partial class NonPlayerCharacter
        {
            public class NPCSchedule
            {
                public Point2D GetHardCoord(int index)
                {
                    return (new Point2D(Coords[index].X, Coords[index].Y));
                }

                /// <summary>
                /// TODO: Need to figure out what these AI types actually mean
                /// </summary>
                protected internal List<byte> AIType = new List<byte>();
                /// <summary>
                /// 3D Coordinates including floor number
                /// </summary>
                protected internal List<Point3D> Coords  { get;  }
                /// <summary>
                /// Times of day to move to the next scheduled item
                /// TODO: figure out why there are 4 times, but only three xyz's to go to?!
                /// </summary>
                protected internal List<byte> Times { get; }

                /// <summary>
                /// Creates an NPC Schedule object 
                /// This is easier to consume than the structure
                /// </summary>
                /// <param name="sched"></param>
                public NPCSchedule(NPC_Schedule sched)
                {
                    Coords = new List<Point3D>();
                    Times = new List<byte>();

                    unsafe
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            AIType.Add(sched.AI_types[i]);
                            Coords.Add(new Point3D(sched.x_coordinates[i], sched.y_coordinates[i], sched.z_coordinates[i]));
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
}
