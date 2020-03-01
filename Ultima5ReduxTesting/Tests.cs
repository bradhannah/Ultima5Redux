using System;
using System.Diagnostics;
using NUnit.Framework;
using Ultima5Redux;
using Ultima5Redux3D;
using System.Collections.Generic;

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
                    world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Skara_Brae, 0), world.State.CharacterRecords,
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

        [Test]
        public void Test_TileOverrides()
        {
            TileOverrides to = new TileOverrides();
            
            World world = new World("C:\\games\\ultima_5_late\\britain");

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Lycaeum, 1), world.State.CharacterRecords,
                false);

            world.State.TheVirtualMap.GuessTile(new Point2D(14, 7));
        }


        [Test]
        public void Test_LoadOverworld()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
        }

        [Test]
        public void Test_LoadOverworldOverrideTile()
        {
            World world = new World("C:\\games\\ultima_5_late\\britain");

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
            
            world.State.TheVirtualMap.GuessTile(new Point2D(81, 106));
        }

        [Test]
        public void Test_InventoryReferences()
        {
            InventoryReferences invRefs = new InventoryReferences();
            List<InventoryReference> invList = invRefs.GetInventoryReferenceList(InventoryReferences.InventoryReferenceType.Armament);
            foreach (InventoryReference invRef in invList)
            {
                string str = invRef.GetRichTextDescription();
            }
        }
        
        
    }
}