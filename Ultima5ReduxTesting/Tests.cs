using System;
using System.Diagnostics;
using NUnit.Framework;
using Ultima5Redux;

namespace Ultima5ReduxTesting
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void AllSmallMapsLoadTest()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            foreach (SmallMapReferences.SingleMapReference smr in world.SmallMapRef.MapReferenceList)
            {
                world.State.TheVirtualMap.LoadSmallMap(
                    world.SmallMapRef.GetSingleMapByLocation(smr.MapLocation, smr.Floor), world.State.CharacterRecords,
                    false);
            }
            
            Assert.True(true);
        }
        
        [Test]
        public void LoadBritishBasement()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            Trace.Write("Starting ");
            //foreach (SmallMapReferences.SingleMapReference smr in world.SmallMapRef.MapReferenceList)
            {
                world.State.TheVirtualMap.LoadSmallMap(
                    world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Britain, 1), world.State.CharacterRecords,
                    false);
            }
            int i = (24 * (60 / 2));
            while (i > 0)
            {
                world.AdvanceTime(2);
                i--;
            }

            TestContext.Out.Write("Ending ");
            //System.Console.WriteLine("Ending ");//+smr.MapLocation + " on floor " + smr.Floor);
            
            Assert.True(true);
        }
        
        
        [Test]
        public void AllSmallMapsLoadWithOneDayTest()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            foreach (SmallMapReferences.SingleMapReference smr in world.SmallMapRef.MapReferenceList)
            {
                Debug.WriteLine("***** Loading "+smr.MapLocation + " on floor " + smr.Floor);
                world.State.TheVirtualMap.LoadSmallMap(
                    world.SmallMapRef.GetSingleMapByLocation(smr.MapLocation, smr.Floor), world.State.CharacterRecords,
                    false);

                int i = (24 * (60 / 2));
                while (i > 0)
                {
                    world.AdvanceTime(2);
                    i--;
                }
                Debug.WriteLine("***** Ending "+smr.MapLocation + " on floor " + smr.Floor);
            }
            
            Assert.True(true);
        }
    }
}