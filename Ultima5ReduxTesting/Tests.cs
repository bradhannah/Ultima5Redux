using System;
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
    }
}