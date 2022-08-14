using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Ultima5Redux;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Dialogue;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;
using Ultima5Redux.References.PlayerCharacters.Inventory;
using Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes;

// ReSharper disable UnusedVariable
// ReSharper disable RedundantAssignment
// ReSharper disable NotAccessedVariable
// ReSharper disable RedundantArgumentDefaultValue

namespace Ultima5ReduxTesting
{
    [TestFixture] public class Tests
    {
        [SetUp] public void Setup()
        {
            // TestContext.Out.WriteLine("CWD: " + Directory.GetCurrentDirectory());
            // TestContext.Out.WriteLine("CWD Dump: " + Directory.EnumerateDirectories(Directory.GetCurrentDirectory()));
            // OutputDirectories(Directory.GetCurrentDirectory());
        }

        [TearDown] public void TearDown()
        {
        }

        public enum SaveFiles
        {
            Britain, Britain2, Britain3, BucDen1, BucDen3, b_carpet, b_frigat, b_horse, b_skiff, quicksave, fresh,
            blackt
        }

        // ReSharper disable once UnusedMember.Local
        private void OutputDirectories(string dir)
        {
            foreach (string subDir in Directory.EnumerateDirectories(dir))
            {
                TestContext.Out.WriteLine(subDir);
            }
        }

        private string GetSaveDirectory(SaveFiles saveFiles)
        {
            if (!Enum.IsDefined(typeof(SaveFiles), saveFiles))
                throw new InvalidEnumArgumentException(nameof(saveFiles), (int)saveFiles, typeof(SaveFiles));
            return Path.Combine(SaveRootDirectory, saveFiles.ToString());
        }

        private string DataDirectory => TestContext.Parameters.Get("DataDirectory",
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? @"/Users/bradhannah/Documents/GitHub/Ultima5ReduxTestDependancies/Saves/Britain2"
                //@"/Users/bradhannah/games/u5tests/Britain2"
                : @"C:\games\ultima5tests\Britain2");

        private string SaveRootDirectory =>
            TestContext.Parameters.Get("SaveRootDirectory",
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? @"/Users/bradhannah/Documents/GitHub/Ultima5ReduxTestDependancies/Saves"
                    //@"/Users/bradhannah/games/u5tests"
                    : @"C:\games\ultima5tests");

        private string NewSaveRootDirectory => TestContext.Parameters.Get("NewSaveRootDirectory",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "UltimaVRedux"));

        private string GetNewSaveDirectory(SaveFiles saveFiles) =>
            Path.Combine(NewSaveRootDirectory, saveFiles.ToString());

        private World CreateWorldFromLegacy(SaveFiles saveFiles, bool bUseExtendedSprites = true,
            bool bLoadInitGam = false) =>
            new World(true, GetSaveDirectory(saveFiles), dataDirectory: DataDirectory,
                bUseExtendedSprites: bUseExtendedSprites, bLoadedInitGam: bLoadInitGam);

        private World CreateWorldFromNewSave(SaveFiles saveFiles, bool bUseExtendedSprites = true,
            bool bLoadInitGam = false) =>
            new World(false, Path.Combine(NewSaveRootDirectory, saveFiles.ToString()), dataDirectory: DataDirectory,
                bUseExtendedSprites: bUseExtendedSprites, bLoadedInitGam: bLoadInitGam);

        [Test] [TestCase(SaveFiles.Britain)] public void AllSmallMapsLoadTest(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";
            foreach (SmallMapReferences.SingleMapReference smr in GameReferences.SmallMapRef.MapReferenceList)
            {
                SmallMapReferences.SingleMapReference singleMap =
                    GameReferences.SmallMapRef.GetSingleMapByLocation(smr.MapLocation, smr.Floor);
                // we don't test dungeon maps here
                if (singleMap.MapType == Map.Maps.Dungeon) continue;
                world.State.TheVirtualMap.LoadSmallMap(singleMap);
            }

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain)] public void test_LoadTrinsicSmallMapsLoadTest(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Trinsic, 0));

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain)] public void test_LoadMinocBuyFrigateAndCheck(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Minoc,
                    0));
            world.State.TheVirtualMap.TheMapUnits.CreateFrigateAtDock(
                SmallMapReferences.SingleMapReference.Location.Minoc);
            Point2D dockLocation =
                VirtualMap.GetLocationOfDock(SmallMapReferences.SingleMapReference.Location.Minoc);
            List<MapUnit> mapUnits = world.State.TheVirtualMap.TheMapUnits.GetMapUnitsByPosition(
                Map.Maps.Overworld,
                dockLocation, 0);

            Frigate frigate2 = world.State.TheVirtualMap.TheMapUnits.GetSpecificMapUnitByLocation<Frigate>(
                Map.Maps.Overworld,
                dockLocation, 0);
            Assert.True(frigate2 != null);

            Assert.True(
                world.State.TheVirtualMap.IsShipOccupyingDock(SmallMapReferences.SingleMapReference.Location.Minoc));

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            world.State.TheVirtualMap.MoveAvatar(new Point2D(frigate2.MapUnitPosition.X, frigate2.MapUnitPosition.Y));
            TurnResults turnResults = new TurnResults();
            world.TryToBoard(out bool bWasSuccessful, turnResults);
            Assert.True(bWasSuccessful);

            Assert.True(frigate2 != null);
            Assert.True(mapUnits[0] is Frigate);
            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.BucDen3)] public void test_LoadMinocBuySkiffAndCheck(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Minoc,
                    0));
            world.State.TheVirtualMap.TheMapUnits.CreateSkiffAtDock(
                SmallMapReferences.SingleMapReference.Location.Minoc);
            Point2D dockLocation =
                VirtualMap.GetLocationOfDock(SmallMapReferences.SingleMapReference.Location.Minoc);
            List<MapUnit> mapUnits = world.State.TheVirtualMap.TheMapUnits.GetMapUnitsByPosition(
                Map.Maps.Overworld,
                dockLocation, 0);

            Skiff skiff = world.State.TheVirtualMap.TheMapUnits.GetSpecificMapUnitByLocation<Skiff>(Map.Maps.Overworld,
                dockLocation, 0);

            Assert.True(
                world.State.TheVirtualMap.IsShipOccupyingDock(SmallMapReferences.SingleMapReference.Location.Minoc));

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            world.State.TheVirtualMap.MoveAvatar(new Point2D(skiff.MapUnitPosition.X,
                skiff.MapUnitPosition.Y)); //-V3095
            TurnResults turnResults = new TurnResults();

            world.TryToBoard(out bool bWasSuccessful, turnResults);
            Assert.True(bWasSuccessful);

            Assert.True(skiff != null);
            Assert.True(mapUnits[0] is Skiff);
            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain)] public void test_LoadSkaraBraeSmallMapsLoadTest(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Skara_Brae, 0));

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void LoadBritishBasement(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            Trace.Write("Starting ");
            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lord_Britishs_Castle, 0));
            int i = (24 * (60 / 2));
            while (i > 0)
            {
                TurnResults turnResults = new TurnResults();
                world.AdvanceTime(2, turnResults);
                i--;
            }

            TestContext.Out.Write("Ending ");

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void AllSmallMapsLoadWithOneDayTest(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            foreach (SmallMapReferences.SingleMapReference smr in GameReferences.SmallMapRef.MapReferenceList)
            {
                Debug.WriteLine("***** Loading " + smr.MapLocation + " on floor " + smr.Floor);
                SmallMapReferences.SingleMapReference singleMapReference =
                    GameReferences.SmallMapRef.GetSingleMapByLocation(smr.MapLocation, smr.Floor);
                if (singleMapReference.MapType == Map.Maps.Dungeon) continue;
                world.State.TheVirtualMap.LoadSmallMap(
                    GameReferences.SmallMapRef.GetSingleMapByLocation(smr.MapLocation, smr.Floor));

                int i = (24 * (60 / 2));
                while (i > 0)
                {
                    TurnResults turnResults = new TurnResults();
                    world.AdvanceTime(2, turnResults);
                    i--;
                }

                Debug.WriteLine("***** Ending " + smr.MapLocation + " on floor " + smr.Floor);
            }
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void CarpetOverworldDayWandering(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            world.MonsterAi = false;
            int i = 24 * (60 / 2) / 4;
            while (i > 0)
            {
                TurnResults turnResults = new TurnResults();
                world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                    true);
                world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition
                    .XY);
                world.TryToMove(Point2D.Direction.Left, false, false, turnResults,
                    true);
                world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition
                    .XY);
                world.TryToMove(Point2D.Direction.Up, false, false, turnResults,
                    true);
                world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition
                    .XY);
                world.TryToMove(Point2D.Direction.Up, false, false, turnResults,
                    true);
                world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition
                    .XY);
                i--;
            }

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void SingleSmallMapWithDayWandering(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lord_Britishs_Castle, 0));
            int i = 24 * (60 / 2) / 4;
            while (i > 0)
            {
                TurnResults turnResults = new TurnResults();

                world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                    true);
                world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition
                    .XY);
                world.TryToMove(Point2D.Direction.Left, false, false, turnResults,
                    true);
                world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition
                    .XY);
                world.TryToMove(Point2D.Direction.Up, false, false, turnResults,
                    true);
                world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition
                    .XY);
                world.TryToMove(Point2D.Direction.Up, false, false, turnResults,
                    true);
                world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition
                    .XY);
                i--;
            }

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_TileOverrides(SaveFiles saveFiles)
        {
            TileOverrideReferences to = new TileOverrideReferences();

            World world = CreateWorldFromLegacy(saveFiles);

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lycaeum, 1));

            world.State.TheVirtualMap.GuessTile(new Point2D(14, 7));
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_LoadOverworld(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_LoadOverworldOverrideTile(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            world.State.TheVirtualMap.GuessTile(new Point2D(81, 106));
        }

        [Test] public void Test_InventoryReferences()
        {
            InventoryReferences invRefs = new InventoryReferences();
            List<InventoryReference> invList =
                invRefs.GetInventoryReferenceList(InventoryReferences.InventoryReferenceType.Armament);
            foreach (InventoryReference invRef in invList)
            {
                string str = invRef.GetRichTextDescription();
                str = invRefs.HighlightKeywords(str);
            }
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_PushPull_WontBudge(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Britain, 0));
            TurnResults turnResults = new TurnResults();
            world.TryToPushAThing(new Point2D(5, 7), Point2D.Direction.Down, out bool bWasPushed, turnResults);
            Assert.False(bWasPushed);

            world.TryToPushAThing(new Point2D(22, 2), Point2D.Direction.Left, out bWasPushed, turnResults);
            Assert.True(bWasPushed);
            string derp = world.State.Serialize();

            world.TryToPushAThing(new Point2D(2, 8), Point2D.Direction.Right, out bWasPushed, turnResults);
            Assert.True(bWasPushed);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_FreeMoveAcrossWorld(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            world.MonsterAi = false;
            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            Point2D startLocation = world.State.TheVirtualMap.CurrentPosition.XY.Copy();

            for (int i = 0; i < 256; i++)
            {
                TurnResults turnResults = new TurnResults();

                world.TryToMove(Point2D.Direction.Up, false, true, turnResults);
            }

            Point2D finalLocation = world.State.TheVirtualMap.CurrentPosition.XY.Copy();

            Assert.True(finalLocation == startLocation);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_CheckAlLTilesForMoongates(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            Point2D startLocation = world.State.TheVirtualMap.CurrentPosition.XY.Copy();
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    TileReference tileReference = world.State.TheVirtualMap.GetTileReference(x, y);
                }
            }

            Point2D finalLocation = world.State.TheVirtualMap.CurrentPosition.XY.Copy();

            Assert.True(finalLocation == startLocation);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_LookupMoonstoneInInventory(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            foreach (MoonPhaseReferences.MoonPhases phase in Enum.GetValues(typeof(MoonPhaseReferences.MoonPhases)))
            {
                if (phase == MoonPhaseReferences.MoonPhases.NoMoon) continue;
                bool bBuried = world.State.TheMoongates.IsMoonstoneBuried((int)phase);
                int nMoonstonesInInv = world.State.PlayerInventory.TheMoonstones.Items[phase].Quantity;
                string desc = GameReferences.InvRef.GetInventoryReference(
                    InventoryReferences.InventoryReferenceType.Item,
                    phase.ToString()).ItemDescription;
            }
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_GetAndUseMoonstone(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
            world.State.TheTimeOfDay.Hour = 12;

            Point2D moongatePosition = new Point2D(166, 19);
            world.State.TheVirtualMap.CurrentPosition.XY = moongatePosition;
            // first search should find moonstone
            TurnResults turnResults = new TurnResults();

            world.TryToSearch(moongatePosition, out bool bWasSuccessful, turnResults);
            Assert.True(bWasSuccessful);

            world.TryToGetAThing(moongatePosition, out bWasSuccessful, out InventoryItem item, turnResults);
            Assert.True(bWasSuccessful);
            Assert.True(item != null);
            Assert.True(item.GetType() == typeof(Moonstone));

            world.TryToUseMoonstone((Moonstone)item, out bWasSuccessful, turnResults);
            Assert.True(bWasSuccessful);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_SearchForMoonstoneAndGet(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            world.State.TheTimeOfDay.Hour = 12;

            Point2D moongatePosition = new Point2D(166, 19);
            // first search should find moonstone
            TurnResults turnResults = new TurnResults();

            world.TryToSearch(moongatePosition, out bool bWasSuccessful, turnResults);
            Assert.True(bWasSuccessful);
            // second search should be empty
            world.TryToSearch(moongatePosition, out bWasSuccessful, turnResults);
            Assert.True(!bWasSuccessful);
            string derp = world.State.Serialize();

            TileReference tileRef = world.State.TheVirtualMap.GetTileReference(moongatePosition);
            Assert.True(tileRef.Index == 281);

            int nSprite = world.State.TheVirtualMap.GuessTile(moongatePosition);

            // can't get it twice!
            world.TryToGetAThing(moongatePosition, out bWasSuccessful, out InventoryItem item, turnResults);
            Assert.True(bWasSuccessful);
            Assert.True(item != null);
            world.TryToGetAThing(moongatePosition, out bWasSuccessful, out item, turnResults);
            Assert.True(!bWasSuccessful);
            Assert.True(item == null);

            world.TryToSearch(moongatePosition, out bWasSuccessful, turnResults);
            Assert.True(!bWasSuccessful);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_MoongateHunting(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            world.State.TheVirtualMap.CurrentPosition.XY = new Point2D(167, 22);
            bool bOnMoongate = world.IsAvatarOnActiveMoongate();
            world.State.TheTimeOfDay.Hour = 23;
            world.State.TheTimeOfDay.Day = 1;
            for (int i = 1; i <= 28; i++)
            {
                world.State.TheTimeOfDay.Day = (byte)i;
                world.State.TheTimeOfDay.Hour = 23;
                Point3D p3d = world.GetMoongateTeleportLocation();
                world.State.TheTimeOfDay.Hour = 4;
                p3d = world.GetMoongateTeleportLocation();
            }
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_TestCorrectMoons(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            MoonPhaseReferences moonPhaseReferences = new MoonPhaseReferences(GameReferences.DataOvlRef);

            world.State.TheTimeOfDay.Day = 25;
            world.State.TheTimeOfDay.Hour = 4;

            MoonPhaseReferences.MoonPhases trammelPhase =
                moonPhaseReferences.GetMoonPhasesByTimeOfDay(world.State.TheTimeOfDay,
                    MoonPhaseReferences.MoonsAndSun.Trammel);
            MoonPhaseReferences.MoonPhases feluccaPhase =
                moonPhaseReferences.GetMoonPhasesByTimeOfDay(world.State.TheTimeOfDay,
                    MoonPhaseReferences.MoonsAndSun.Felucca);
            Assert.True(trammelPhase == MoonPhaseReferences.MoonPhases.GibbousWaning);
            Assert.True(feluccaPhase == MoonPhaseReferences.MoonPhases.LastQuarter);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_MoonPhaseReference(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            MoonPhaseReferences moonPhaseReferences = new MoonPhaseReferences(GameReferences.DataOvlRef);

            for (byte nDay = 1; nDay <= 28; nDay++)
            {
                for (byte nHour = 0; nHour < 24; nHour++)
                {
                    world.State.TheTimeOfDay.Day = nDay;
                    world.State.TheTimeOfDay.Hour = nHour;

                    MoonPhaseReferences.MoonPhases moonPhase =
                        moonPhaseReferences.GetMoonGateMoonPhase(world.State.TheTimeOfDay);

                    float fMoonAngle = MoonPhaseReferences.GetMoonAngle(world.State.TheTimeOfDay);
                    Assert.True(fMoonAngle >= 0 && fMoonAngle < 360);
                }
            }
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_TalkToSmith(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            SmallMapReferences.SingleMapReference.Location location =
                SmallMapReferences.SingleMapReference.Location.Iolos_Hut;
            NonPlayerCharacterState npcState =
                world.State.TheNonPlayerCharacterStates.GetStateByLocationAndIndex(location, 4);

            world.CreateConversationAndBegin(npcState, OnUpdateOfEnqueuedScriptItem);
        }

        private static void OnUpdateOfEnqueuedScriptItem(Conversation conversation)
        {
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_KlimbMountain(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
            world.State.TheVirtualMap.CurrentPosition.XY = new Point2D(166, 21);
            TurnResults turnResults = new TurnResults();
            world.TryToKlimb(out World.KlimbResult klimbResult, turnResults);
            Assert.True(klimbResult == World.KlimbResult.RequiresDirection);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_MoveALittle(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
            world.State.TheVirtualMap.CurrentPosition.XY = new Point2D(166, 21);
            TurnResults turnResults = new TurnResults();

            world.TryToMove(Point2D.Direction.Up, false, false, turnResults);
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_MoveALittleOverworld(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
            world.State.TheVirtualMap.CurrentPosition.XY = new Point2D(166, 21);
            TurnResults turnResults = new TurnResults();

            world.TryToMove(Point2D.Direction.Up, false, false, turnResults);
            world.TryToMove(Point2D.Direction.Left, false, false, turnResults);
            world.TryToMove(Point2D.Direction.Down, false, false, turnResults);
            world.TryToMove(Point2D.Direction.Left, false, false, turnResults);
            world.TryToMove(Point2D.Direction.Up, false, false, turnResults);
            world.TryToMove(Point2D.Direction.Right, false, false, turnResults);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_TalkToDelwyn(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            SmallMapReferences.SingleMapReference.Location location =
                SmallMapReferences.SingleMapReference.Location.Minoc;

            NonPlayerCharacterState npcState =
                world.State.TheNonPlayerCharacterStates.GetStateByLocationAndIndex(location, 9);

            Conversation convo = world.CreateConversationAndBegin(npcState,
                OnUpdateOfEnqueuedScriptItemHandleDelwyn);
            convo.BeginConversation();
            convo.AddUserResponse("yes");
            convo.AddUserResponse("yes");
            convo.AddUserResponse("yes");
            convo.AddUserResponse("bye");

            Conversation convo2 = world.CreateConversationAndBegin(npcState,
                OnUpdateOfEnqueuedScriptItemHandleDelwyn);
            convo2.BeginConversation();
            convo2.AddUserResponse("yes");
            convo2.AddUserResponse("yes");
            convo2.AddUserResponse("yes");
            convo2.AddUserResponse("bye");
        }

        private void OnUpdateOfEnqueuedScriptItemHandleDelwyn(Conversation conversation)
        {
            TalkScript.ScriptItem item = conversation.DequeueFromOutputBuffer();
            switch (item.Command)
            {
                case TalkScript.TalkCommand.PlainString:
                    Debug.WriteLine(item.Str);
                    break;
                case TalkScript.TalkCommand.AvatarsName:
                    break;
                case TalkScript.TalkCommand.EndConversation:
                    break;
                case TalkScript.TalkCommand.Pause:
                    break;
                case TalkScript.TalkCommand.JoinParty:
                    break;
                case TalkScript.TalkCommand.Gold:
                    break;
                case TalkScript.TalkCommand.Change:
                    break;
                case TalkScript.TalkCommand.Or:
                    break;
                case TalkScript.TalkCommand.AskName:
                    break;
                case TalkScript.TalkCommand.KarmaPlusOne:
                    break;
                case TalkScript.TalkCommand.KarmaMinusOne:
                    break;
                case TalkScript.TalkCommand.CallGuards:
                    break;
                case TalkScript.TalkCommand.IfElseKnowsName:
                    break;
                case TalkScript.TalkCommand.NewLine:
                    break;
                case TalkScript.TalkCommand.Rune:
                    break;
                case TalkScript.TalkCommand.KeyWait:
                    break;
                case TalkScript.TalkCommand.StartLabelDefinition:
                    break;
                case TalkScript.TalkCommand.StartNewSection:
                    break;
                case TalkScript.TalkCommand.Unknown_Enter:
                    break;
                case TalkScript.TalkCommand.GotoLabel:
                    break;
                case TalkScript.TalkCommand.DefineLabel:
                    break;
                case TalkScript.TalkCommand.DoNothingSection:
                    break;
                case TalkScript.TalkCommand.PromptUserForInput_NPCQuestion:
                    // userResponse = "yes";
                    // conversation.AddUserResponse(userResponse);
                    break;
                case TalkScript.TalkCommand.PromptUserForInput_UserInterest:
                    break;
                case TalkScript.TalkCommand.UserInputNotRecognized:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_BasicBlackSmithDialogue(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            BlackSmith blacksmith = GameReferences.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Minoc,
                NonPlayerCharacterReference.SpecificNpcDialogType.Blacksmith, null,
                world.State.PlayerInventory) as BlackSmith;

            Assert.True(blacksmith != null, nameof(blacksmith) + " != null");
            string purchaseStr2 = blacksmith.GetEquipmentBuyingOutput(DataOvlReference.Equipment.LeatherHelm, 100);
            string purchaseStr = blacksmith.GetEquipmentBuyingOutput(DataOvlReference.Equipment.AmuletOfTurning, 100);

            for (int i = 0; i < 10; i++)
            {
                string pissedOff = blacksmith.GetPissedOffShoppeKeeperGoodbyeResponse();
                string happy = blacksmith.GetHappyShoppeKeeperGoodbyeResponse();
                string selling = blacksmith.GetEquipmentSellingOutput(100, "Big THING");
                string buying =
                    blacksmith.GetEquipmentBuyingOutput(DataOvlReference.Equipment.Arrows, 100);
            }

            string hello = blacksmith.GetHelloResponse(world.State.TheTimeOfDay);
            blacksmith.GetForSaleList();
            _ = blacksmith.GetDoneResponse();
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_BasicHealerDialogue(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            Healer healer = (Healer)GameReferences.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Cove,
                NonPlayerCharacterReference.SpecificNpcDialogType.Healer, null,
                world.State.PlayerInventory);

            _ = healer.NoNeedForMyArt();

            int price = healer.GetPrice(Healer.RemedyTypes.Heal);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_BasicTavernDialogue(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            BarKeeper barKeeper = (BarKeeper)GameReferences.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Paws,
                NonPlayerCharacterReference.SpecificNpcDialogType.Barkeeper, null, world.State.PlayerInventory);

            string myOppressionTest = barKeeper.GetGossipResponse("oppr", true);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_BasicMagicSellerDialogue(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            MagicSeller magicSeller = (MagicSeller)GameReferences.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Cove,
                NonPlayerCharacterReference.SpecificNpcDialogType.MagicSeller, null, world.State.PlayerInventory);
            List<Reagent> reagents = magicSeller.GetReagentsForSale();

            int price1 = reagents[0].GetAdjustedBuyPrice(world.State.CharacterRecords,
                SmallMapReferences.SingleMapReference.Location.Cove);
            int price2 = reagents[1].GetAdjustedBuyPrice(world.State.CharacterRecords,
                SmallMapReferences.SingleMapReference.Location.Cove);

            string hello = magicSeller.GetHelloResponse(world.State.TheTimeOfDay);
            string buyThing = magicSeller.GetReagentBuyingOutput(reagents[0]);
            buyThing = magicSeller.GetReagentBuyingOutput(reagents[1]);
            buyThing = magicSeller.GetReagentBuyingOutput(reagents[2]);
            buyThing = magicSeller.GetReagentBuyingOutput(reagents[3]);
            buyThing = magicSeller.GetReagentBuyingOutput(reagents[4]);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_AdjustedMerchantPrices(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            int nCrossbowBuy = world.State.PlayerInventory.TheWeapons.Items[WeaponReference.SpecificWeaponType.Crossbow]
                .GetAdjustedBuyPrice(world.State.CharacterRecords,
                    ((RegularMap)world.State.TheVirtualMap.CurrentMap).CurrentSingleMapReference.MapLocation);
            int nCrossbowSell = world.State.PlayerInventory.TheWeapons
                .Items[WeaponReference.SpecificWeaponType.Crossbow]
                .GetAdjustedSellPrice(world.State.CharacterRecords,
                    ((RegularMap)world.State.TheVirtualMap.CurrentMap).CurrentSingleMapReference.MapLocation);
            Assert.True(nCrossbowBuy > 0);
            Assert.True(nCrossbowSell > 0);

            int nKeysPrice = world.State.PlayerInventory.TheProvisions
                .Items[ProvisionReferences.SpecificProvisionType.Keys]
                .GetAdjustedBuyPrice(world.State.CharacterRecords,
                    SmallMapReferences.SingleMapReference.Location.Buccaneers_Den);
            GuildMaster guildMaster = (GuildMaster)GameReferences.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Buccaneers_Den,
                NonPlayerCharacterReference.SpecificNpcDialogType.GuildMaster, null, world.State.PlayerInventory);
            string buyKeys = guildMaster.GetProvisionBuyOutput(ProvisionReferences.SpecificProvisionType.Keys, 240);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_SimpleStringTest(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            string str =
                GameReferences.DataOvlRef.StringReferences.GetString(DataOvlReference.Battle2Strings.N_VICTORY_BANG_N);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void Test_ShipwrightDialogue(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            Shipwright shipwright = (Shipwright)GameReferences.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Buccaneers_Den,
                NonPlayerCharacterReference.SpecificNpcDialogType.Shipwright, null, world.State.PlayerInventory);

            string hi = shipwright.GetHelloResponse(world.State.TheTimeOfDay);
        }

        [Test] [TestCase(SaveFiles.Britain)] public void Test_EnterBuilding(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";
            TurnResults turnResults = new TurnResults();

            world.TryToEnterBuilding(new Point2D(159, 20), out bool bWasSuccessful, turnResults);

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain)] public void Test_EnterYewAndLookAtMonster(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";

            // yew
            TurnResults turnResults = new TurnResults();
            world.TryToEnterBuilding(new Point2D(58, 43), out bool bWasSuccessful, turnResults);

            foreach (MapUnit mapUnit in world.State.TheVirtualMap.TheMapUnits.CurrentMapUnits.AllMapUnits)
            {
                _ = mapUnit.GetNonBoardedTileReference();
            }

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.BucDen1)] public void Test_GetInnStuffAtBucDen(SaveFiles saveFiles)
        {
            // World world = new World(SaveDirectory);
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";

            TurnResults turnResults = new TurnResults();
            world.TryToEnterBuilding(new Point2D(159, 20), out bool bWasSuccessful, turnResults);

            Innkeeper innKeeper = (Innkeeper)GameReferences.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Buccaneers_Den,
                NonPlayerCharacterReference.SpecificNpcDialogType.InnKeeper, null, world.State.PlayerInventory);

            Point2D bedPosition = innKeeper.InnKeeperServices.SleepingPosition;

            IEnumerable<PlayerCharacterRecord> records =
                world.State.CharacterRecords.GetPlayersAtInn(SmallMapReferences.SingleMapReference.Location
                    .Buccaneers_Den);

            string noRoom = innKeeper.GetNoRoomAtTheInn(world.State.CharacterRecords);
            foreach (PlayerCharacterRecord record in records)
            {
                world.State.CharacterRecords.JoinPlayerCharacter(record);
            }

            List<PlayerCharacterRecord> activeRecords = world.State.CharacterRecords.GetActiveCharacterRecords();

            string goodbye = innKeeper.GetPissedOffShoppeKeeperGoodbyeResponse();
            string pissed = innKeeper.GetPissedOffNotEnoughMoney();
            string howMuch = innKeeper.GetThatWillBeGold(activeRecords[1]);
            string shoppeName = innKeeper.TheShoppeKeeperReference.ShoppeName;

            _ = world.State.CharacterRecords.GetCharacterFromParty(4);
            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain3)] public void Test_GetInnStuffAtBritain(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";

            TurnResults turnResults = new TurnResults();
            world.TryToEnterBuilding(new Point2D(159, 20), out bool bWasSuccessful, turnResults);

            Innkeeper innKeeper = (Innkeeper)GameReferences.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Britain,
                NonPlayerCharacterReference.SpecificNpcDialogType.InnKeeper, null, world.State.PlayerInventory);

            Point2D bedPosition = innKeeper.InnKeeperServices.SleepingPosition;

            IEnumerable<PlayerCharacterRecord> records =
                world.State.CharacterRecords.GetPlayersAtInn(SmallMapReferences.SingleMapReference.Location.Britain);

            string noRoom = innKeeper.GetNoRoomAtTheInn(world.State.CharacterRecords);
            foreach (PlayerCharacterRecord record in records)
            {
                world.State.CharacterRecords.JoinPlayerCharacter(record);
            }

            List<PlayerCharacterRecord> activeRecords = world.State.CharacterRecords.GetActiveCharacterRecords();

            string goodbye = innKeeper.GetPissedOffShoppeKeeperGoodbyeResponse();
            string pissed = innKeeper.GetPissedOffNotEnoughMoney();
            string howmuch = innKeeper.GetThatWillBeGold(activeRecords[1]);
            string shoppename = innKeeper.TheShoppeKeeperReference.ShoppeName;
            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain)] public void Test_MakeAHorse(SaveFiles saveFiles)
        {
            // World world = new World(SaveDirectory);
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";

            TurnResults turnResults = new TurnResults();
            world.TryToEnterBuilding(new Point2D(159, 20), out bool bWasSuccessful, turnResults);

            Horse horse = world.State.TheVirtualMap.CreateHorseAroundAvatar();
            Assert.True(horse != null);
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_MoveWithExtendedSprites(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            world.MonsterAi = false;
            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();
            TurnResults turnResults = new TurnResults();

            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
            // make sure it is using the extended sprite
            //GetCurrentTileReference

            Assert.True(world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().CurrentBoardedMapUnit
                .GetBoardedTileReference()
                .Index == 515);
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_CheckedBoardedTileCarpet(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();
            Assert.True(avatar.IsAvatarOnBoardedThing);
            Assert.True(avatar.CurrentBoardedMapUnit != null);

            int nCarpets = world.State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet]
                .Quantity;
            TurnResults turnResults = new TurnResults();
            world.TryToXit(out bool bWasSuccessful, turnResults);
            Assert.True(nCarpets == world.State.PlayerInventory.SpecializedItems
                .Items[SpecialItem.SpecificItemType.Carpet].Quantity);
            Assert.True(bWasSuccessful);
            world.TryToBoard(out bool bWasSuccessfulBoard, turnResults);
            Assert.True(bWasSuccessfulBoard);
            world.TryToXit(out bWasSuccessful, turnResults);

            nCarpets = world.State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet]
                .Quantity;
            Point2D curPos = world.State.TheVirtualMap.CurrentPosition.XY;
            world.TryToMove(Point2D.Direction.Left, false, false, turnResults);
            world.TryToGetAThing(curPos, out bool bGotACarpet, out InventoryItem carpet, turnResults);
            Assert.True(bGotACarpet);
            //Assert.True(carpet!=null);
            Assert.True(nCarpets + 1 == world.State.PlayerInventory.SpecializedItems
                .Items[SpecialItem.SpecificItemType.Carpet].Quantity);
            world.TryToGetAThing(curPos, out bGotACarpet, out carpet, turnResults);
            Assert.True(!bGotACarpet);

            world.TryToUseSpecialItem(
                world.State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet],
                out bool bAbleToUseItem, turnResults);
            Assert.True(bAbleToUseItem);
            world.TryToXit(out bWasSuccessful, turnResults);
            Assert.True(bWasSuccessful);
            world.TryToMove(Point2D.Direction.Left, false, false, turnResults);
            curPos = world.State.TheVirtualMap.CurrentPosition.XY;
            world.TryToGetAThing(curPos, out bGotACarpet, out carpet, turnResults);
            Assert.True(bGotACarpet);

            _ = "";
        }

        [Test] [TestCase(SaveFiles.BucDen3)] public void Test_CheckUseCarpet(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            int nCarpets = world.State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet]
                .Quantity;
            TurnResults turnResults = new TurnResults();

            world.TryToUseSpecialItem(
                world.State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet],
                out bool bAbleToUseItem, turnResults);
            Assert.True(bAbleToUseItem);
            Assert.True(world.State.PlayerInventory.SpecializedItems.Items[SpecialItem.SpecificItemType.Carpet]
                .Quantity == nCarpets - 1);
        }

        [Test] [TestCase(SaveFiles.b_horse)] public void Test_CheckedBoardedTileHorse(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();
            Assert.True(avatar.IsAvatarOnBoardedThing);
            Assert.True(avatar.CurrentBoardedMapUnit != null);

            TurnResults turnResults = new TurnResults();
            world.TryToXit(out bool bWasSuccessful, turnResults);

            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_skiff)] public void Test_CheckedBoardedTileSkiff(SaveFiles saveFiles)
        {
            // World world = new World(SaveDirectory);
            World world = CreateWorldFromLegacy(saveFiles);

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();
            Assert.True(avatar.IsAvatarOnBoardedThing);
            Assert.True(avatar.CurrentBoardedMapUnit != null);

            TurnResults turnResults = new TurnResults();
            world.TryToXit(out bool bWasSuccessful, turnResults);

            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_skiff)] public void Test_CheckedBoardedTileSkiffMoveOntoSkiff(SaveFiles saveFiles)
        {
            // World world = new World(SaveDirectory);
            World world = CreateWorldFromLegacy(saveFiles);

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();
            Assert.True(avatar.IsAvatarOnBoardedThing);
            Assert.True(avatar.CurrentBoardedMapUnit != null);
            TurnResults turnResults = new TurnResults();

            world.TryToMove(Point2D.Direction.Down, false, true, turnResults);
            //Assert.True(moveResult == World.TryToMoveResult.Blocked);
            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_frigat)] public void Test_CheckedBoardedTileFrigate(SaveFiles saveFiles)
        {
            // World world = new World(SaveDirectory);
            World world = CreateWorldFromLegacy(saveFiles);

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit();
            Assert.True(avatar.IsAvatarOnBoardedThing);
            Assert.True(avatar.CurrentBoardedMapUnit != null);

            TurnResults turnResults = new TurnResults();

            world.TryToXit(out bool bWasSuccessful, turnResults);

            _ = "";
        }

        [Test] [TestCase(SaveFiles.BucDen1)] public void Test_ForceVisibleRecalculationInBucDen(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            Point2D startSpot = new Point2D(159, 20);
            TurnResults turnResults = new TurnResults();
            world.TryToEnterBuilding(startSpot, out bool bWasSuccessful, turnResults);

            world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition.XY);
            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_ForceVisibleRecalculationInLargeMap(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.MoveAvatar(new Point2D(128, 0));
            world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition.XY);
        }

        [Test] [TestCase(SaveFiles.b_carpet)]
        public void Test_LookupPrimaryAndSecondaryEnemyReferences(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            EnemyReference enemyReference =
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(448));

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia,
                    0),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords, enemyReference);

            EnemyReference secondEnemyReference = GameReferences.EnemyRefs.GetFriendReference(enemyReference);
            //GetEnemyReference(enemyReference.FriendIndex);
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadCampFireCombatMap(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia,
                    0),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords,
                // orcs
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(448)),
                1,
                // troll
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(484)),
                1);

            TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;

            Assert.True(player.Record.Class == PlayerCharacterRecord.CharacterClass.Avatar);
            TurnResults turnResults = new TurnResults();
            world.TryToMoveCombatMap(Point2D.Direction.Up, turnResults);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Left, turnResults);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Up, turnResults);
            // Assert.True(tryToMoveResult == World.TryToMoveResult.Blocked);

            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_Dungeon0WithDivide(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon, 0),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords);

            TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;

            CombatMap combatMap = world.State.TheVirtualMap.CurrentCombatMap;
            Assert.NotNull(combatMap);
            combatMap.DivideEnemy(combatMap.AllEnemies.ToList()[2]);

            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_HammerCombatMapInitiativeTest(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            // right sided hammer
            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon, 4),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords);
            TurnResults turnResults = new TurnResults();
            world.TryToMoveCombatMap(Point2D.Direction.Up, turnResults);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Left, turnResults);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Up, turnResults);
            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_EscapeCombatMap(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia,
                    4),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords);
            TurnResults turnResults = new TurnResults();

            world.TryToMoveCombatMap(Point2D.Direction.Up, turnResults);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Left, turnResults);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Up, turnResults);

            do
            {
                world.State.TheVirtualMap.CurrentCombatMap.NextCharacterEscape(out CombatPlayer combatPlayer);
                CombatPlayer newCombatPlayer = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;

                if (combatPlayer == null) break;
            } while (true);

            world.State.TheVirtualMap.ReturnToPreviousMapAfterCombat();
            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadFirstCombatMap(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia,
                    11),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords,
                // orcs
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(448)),
                5,
                // troll
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(484)),
                1);
            TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;
            {
                Assert.True(player.Record.Class == PlayerCharacterRecord.CharacterClass.Avatar);
                TurnResults turnResults = new TurnResults();

                world.TryToMoveCombatMap(player, Point2D.Direction.Up, turnResults);
            }

            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadCombatMapThenBack(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            TurnResults turnResults = new TurnResults();

            world.TryToEnterBuilding(new Point2D(159, 20), out bool bWasSuccessful, turnResults);

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia,
                    2),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords,
                // orcs
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(448)),
                5,
                // troll
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(484)),
                1);

            var thing = GameReferences.CombatMapRefs
                .GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, 2)
                .GetEntryDirections();

            world.State.TheVirtualMap.ReturnToPreviousMapAfterCombat();

            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadAllCombatMapsWithMonsters(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            //List<CombatMap> worldMaps = new List<CombatMap>();
            for (int i = 0; i < 16; i++)
            {
                SingleCombatMapReference singleCombatMapReference =
                    GameReferences.CombatMapRefs.GetSingleCombatMapReference(
                        SingleCombatMapReference.Territory.Britannia, i);
                foreach (SingleCombatMapReference.EntryDirection worldEntryDirection in singleCombatMapReference
                             .GetEntryDirections())
                {
                    world.State.TheVirtualMap.LoadCombatMap(
                        GameReferences.CombatMapRefs.GetSingleCombatMapReference(
                            SingleCombatMapReference.Territory.Britannia,
                            i),
                        worldEntryDirection, world.State.CharacterRecords);
                    TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);
                }
            }

            EnemyReference enemy1 =
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(320));

            // for (int i = 0; i < 112; i++)
            // temporarily not checking the very last 111 level of dungeon because it 
            // uses LB mirror sprites and breaks everything
            for (int i = 0; i < 111; i++)
            {
                SingleCombatMapReference singleCombatMapReference =
                    GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                        i);

                if (!singleCombatMapReference.GetEntryDirections().Any())
                {
                    IEnumerable<SingleCombatMapReference.EntryDirection> dirs =
                        singleCombatMapReference.GetEntryDirections();
                }

                foreach (SingleCombatMapReference.EntryDirection dungeonEntryDirection in singleCombatMapReference
                             .GetEntryDirections())
                {
                    world.State.TheVirtualMap.LoadCombatMap(
                        GameReferences.CombatMapRefs.GetSingleCombatMapReference(
                            SingleCombatMapReference.Territory.Dungeon,
                            i),
                        dungeonEntryDirection, world.State.CharacterRecords, enemy1, 4);
                    TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);
                }
            }

            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_GetEscapablePoints(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia,
                    15),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords,
                // orcs
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(448)),
                5,
                // troll
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(484)),
                1);

            List<Point2D> points =
                world.State.TheVirtualMap.CurrentCombatMap.GetEscapablePoints(new Point2D(12, 12),
                    Map.WalkableType.CombatLand);
            _ = "";
        }

        [Test] public void Test_ExtentCheck()
        {
            Point2D point = new Point2D(15, 15);
            var constrainedPoints = point.GetConstrainedSurroundingPoints(1, 15, 15);
            Assert.True(constrainedPoints.Count == 2);
            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_TalkToNoOne(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            NonPlayerCharacter npc =
                world.State.TheVirtualMap.GetNpcToTalkTo(MapUnitMovement.MovementCommandDirection.North);
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadInitialSaveGame(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, true);

            NonPlayerCharacter npc =
                world.State.TheVirtualMap.GetNpcToTalkTo(MapUnitMovement.MovementCommandDirection.North);
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadAndReloadInitialSave(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, true);
            world.ReLoadFromJson();
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadAndReloadCarpetOverworld(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            world.ReLoadFromJson();
        }

        [Test]
        [TestCase(SaveFiles.b_carpet)]
        [TestCase(SaveFiles.BucDen1)]
        [TestCase(SaveFiles.Britain2)]
        [TestCase(SaveFiles.b_frigat)]
        public void test_ReloadStressTest(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            world.ReLoadFromJson();

            string loadedJson = world.State.Serialize();
            world.ReLoadFromJson();
            string newLoadedJson = world.State.Serialize();

            Assert.AreEqual(loadedJson, newLoadedJson);

            _ = world.State.TheVirtualMap.CurrentPosition;
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void test_TestInventoryAfterReload(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);
            world.ReLoadFromJson();
            string loadedJson = world.State.Serialize();
            Assert.NotNull(world.State);
            world.ReLoadFromJson();
            string newLoadedJson = world.State.Serialize();
            Assert.NotNull(world.State);

            Assert.AreEqual(loadedJson, newLoadedJson);

            Reagent reagent = world.State.PlayerInventory.SpellReagents.Items[Reagent.SpecificReagentType.Garlic];
            Assert.NotNull(reagent);
            string reagentName = reagent.LongName;

            CombatItemReference weaponRef =
                world.State.PlayerInventory.TheWeapons.AllCombatItems.ToList()[0].TheCombatItemReference;
            Assert.NotNull(weaponRef);
            string weaponRefStr = weaponRef.EquipmentName;

            Weapon weapon =
                world.State.PlayerInventory.TheWeapons.GetWeaponFromEquipment(DataOvlReference.Equipment.GlassSword);
            Assert.NotNull(weapon);
            string weaponStr = weapon.LongName;

            Armour armourHelm =
                world.State.PlayerInventory.ProtectiveArmour.GetArmourFromEquipment(DataOvlReference.Equipment
                    .IronHelm);
            Assert.NotNull(armourHelm);
            string helmStr = armourHelm.LongName;

            // spells
            // references

            _ = world.State.TheVirtualMap.CurrentPosition;
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void test_ReloadAndQuantityCheck(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            int nTorches = world.State.PlayerInventory.TheProvisions.Torches;
            int nFood = world.State.PlayerInventory.TheProvisions.Food;
            int nGold = world.State.PlayerInventory.TheProvisions.Gold;
            Assert.NotZero(nTorches);
            Assert.NotZero(nFood);
            Assert.NotZero(nGold);
            world.ReLoadFromJson();
            int nTorchesNew = world.State.PlayerInventory.TheProvisions.Torches;
            int nFoodNew = world.State.PlayerInventory.TheProvisions.Food;
            int nGoldNew = world.State.PlayerInventory.TheProvisions.Gold;
            Assert.AreEqual(nTorches, nTorchesNew);
            Assert.AreEqual(nFood, nFoodNew);
            Assert.AreEqual(nGold, nGoldNew);

            string loadedJson = world.State.Serialize();
            Assert.NotNull(world.State);
            world.ReLoadFromJson();
            string newLoadedJson = world.State.Serialize();
            Assert.NotNull(world.State);

            nTorchesNew = world.State.PlayerInventory.TheProvisions.Torches;
            nFoodNew = world.State.PlayerInventory.TheProvisions.Food;
            nGoldNew = world.State.PlayerInventory.TheProvisions.Gold;

            Assert.AreEqual(nTorches, nTorchesNew);
            Assert.AreEqual(nFood, nFoodNew);
            Assert.AreEqual(nGold, nGoldNew);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void test_CheckSpellReagents(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);

            List<Reagent.SpecificReagentType> reagents = new List<Reagent.SpecificReagentType>
            {
                Reagent.SpecificReagentType.SulfurAsh
            };

            Assert.True(GameReferences.MagicRefs.GetMagicReference(MagicReference.SpellWords.In_Lor)
                .IsCorrectReagents(reagents));
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void test_ReloadAndCheckNPCs(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            world.ReLoadFromJson();

            string loadedJson = world.State.Serialize();
            Assert.NotNull(world.State);
            world.ReLoadFromJson();
            string newLoadedJson = world.State.Serialize();
        }

        [Test] [TestCase(SaveFiles.quicksave)] public void test_ReloadNewSave(SaveFiles saveFiles)
        {
            World world = CreateWorldFromNewSave(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
        }

        [Test] [TestCase(SaveFiles.quicksave)] public void test_ReloadNewSaveToCombatAndBack(SaveFiles saveFiles)
        {
            World world = CreateWorldFromNewSave(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            //world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            EnemyReference enemyReference =
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(448));

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia,
                    0),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords, enemyReference);

            world.State.TheVirtualMap.ReturnToPreviousMapAfterCombat();

            TileReference tileRef = world.State.TheVirtualMap.GetTileReference(0, 0);
        }

        [Test] [TestCase(SaveFiles.quicksave)] public void test_SerializeDeserializeGameSummary(SaveFiles saveFiles)
        {
            World world = CreateWorldFromNewSave(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            string saveDirectory = GetNewSaveDirectory(saveFiles);
            string summaryFileAndPath = Path.Combine(saveDirectory, FileConstants.NEW_SAVE_SUMMARY_FILE);

            GameSummary gameSummary = world.State.CreateGameSummary(saveDirectory);
            string serializedGameSummary = gameSummary.SerializeGameSummary();
            GameSummary reserializedGameSummary = GameSummary.DeserializeGameSummary(serializedGameSummary);
        }

        [Test] [TestCase(SaveFiles.quicksave)] public void test_CheckSpriteReferences(SaveFiles saveFiles)
        {
            World world = CreateWorldFromNewSave(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);
            CombatItemReference itemRef =
                GameReferences.CombatItemRefs.GetCombatItemReferenceFromEquipment(DataOvlReference.Equipment.LongSword);
            Assert.True(itemRef.Sprite == 261);
        }

        [Test] [TestCase(SaveFiles.quicksave)] public void test_CastInLor(SaveFiles saveFiles)
        {
            World world = CreateWorldFromNewSave(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            SpellCastingDetails details = new SpellCastingDetails();
            Spell spell = world.State.PlayerInventory.MagicSpells.Items[MagicReference.SpellWords.In_Lor];
            SpellResult result = spell.CastSpell(world.State, details);

            Assert.True(result.Status == SpellResult.SpellResultStatus.Success);
        }

        [Test] [TestCase(SaveFiles.quicksave)] public void test_GetMagicReferenceByString(SaveFiles saveFiles)
        {
            World world = CreateWorldFromNewSave(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            MagicReference magReg = GameReferences.MagicRefs.GetMagicReference("In_Lor");
            // world.State.PlayerInventory.Mag 

            Assert.NotNull(magReg);
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void test_CarpetEnemiesExist(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            world.ReLoadFromJson();

            Assert.True(
                world.State.TheVirtualMap.TheMapUnits.OverworldMapMapUnitCollection.Enemies.Count(m => m.IsActive) > 0);
        }

        [Test] [TestCase(SaveFiles.quicksave)] public void test_WorldVisibilityLargeMapAtEdge(SaveFiles saveFiles)
        {
            World world = CreateWorldFromNewSave(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            world.ReLoadFromJson();

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
            Point2D newAvatarPos = new Point2D(146, 254);
            newAvatarPos = new Point2D(146, 255);
            world.State.TheVirtualMap.MoveAvatar(newAvatarPos);
            world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(newAvatarPos);

            Assert.True(world.State.TheVirtualMap.CurrentMap.VisibleOnMap[146][0]);
        }

        [Test] [TestCase(SaveFiles.quicksave)] public void test_SmallMapVisibilityAtEdge(SaveFiles saveFiles)
        {
            World world = CreateWorldFromNewSave(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            world.ReLoadFromJson();

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Trinsic, 0));

            world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition.XY);

            //Assert.True(world.State.TheVirtualMap.CurrentMap.TouchedOuterBorder);
        }

        [Test] [TestCase(SaveFiles.fresh)] public void test_LoadLegacyFreshAvatarExists(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            Assert.True(world.State.TheVirtualMap.IsMapUnitOccupiedTile(new Point2D(15, 15)));
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void test_EnterDungeon(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            Point2D minocCovetousDungeon = new Point2D(156, 27);
            world.State.TheVirtualMap.MoveAvatar(minocCovetousDungeon);
            TurnResults turnResults = new TurnResults();
            world.TryToEnterBuilding(minocCovetousDungeon, out bool bWasSuccessful, turnResults);
            Assert.IsNotNull(world);
        }

        [Test] [TestCase(SaveFiles.fresh)] public void test_DefaultMoonstoneCheckMinoc(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            world.ReLoadFromJson();

            Assert.True(world.State.TheMoongates.IsMoonstoneBuried(new Point3D(224, 133, 0)));
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void test_AvatarInitiatedCombatChecks(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            world.ReLoadFromJson();

            Assert.True(world.State.TheMoongates.IsMoonstoneBuried(new Point3D(224, 133, 0)));

            // VirtualMap.AggressiveMapUnitInfo aggressiveMapUnitInfo = world.State.TheVirtualMap.GetAggressiveMapUnitInfo(
            //     world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().MapUnitPosition.XY,
            //     new Point2D(146, 238), SingleCombatMapReference.Territory.Britannia,
            //     world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit());
            SingleCombatMapReference singleCombatMapReference =
                world.State.TheVirtualMap.GetCombatMapReferenceForAvatarAttacking(
                    world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().MapUnitPosition.XY,
                    new Point2D(146, 238), SingleCombatMapReference.Territory.Britannia);

            // attack insects
            Assert.True(singleCombatMapReference.CombatMapNum == 7);
            //Assert.True(missileType == CombatItemReference.MissileType.None);
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void test_AvatarPassSomeTurnsInCarpet(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            world.ReLoadFromJson();

            Assert.True(world.State.TheMoongates.IsMoonstoneBuried(new Point3D(224, 133, 0)));

            world.State.TheVirtualMap.OneInXOddsOfNewMonster = 2;
            Utils.Ran = new Random(1);
            TurnResults turnResults = new TurnResults();

            world.TryToMove(Point2D.Direction.Left, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Left, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Left, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Left, false, false, turnResults,
                true);

            world.TryToPassTime(turnResults);
            world.TryToPassTime(turnResults);
            world.TryToPassTime(turnResults);
            world.TryToPassTime(turnResults);
            world.TryToPassTime(turnResults);
            world.TryToPassTime(turnResults);
        }

        [Test] [TestCase(SaveFiles.b_frigat)] public void test_AvatarMoveIntoOcean(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles, true, false);
            Assert.NotNull(world);
            Assert.NotNull(world.State);

            GameReferences.Initialize(DataDirectory);

            world.ReLoadFromJson();

            Assert.True(world.State.TheMoongates.IsMoonstoneBuried(new Point3D(224, 133, 0)));

            world.State.TheVirtualMap.OneInXOddsOfNewMonster = 2;
            Utils.Ran = new Random(1);
            TurnResults turnResults = new TurnResults();

            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
            world.TryToMove(Point2D.Direction.Down, false, false, turnResults,
                true);
        }

        private static T GetEnumByName<T>(InventoryReference inventoryReference) where T : Enum
        {
            T enumResult = (T)Enum.Parse(typeof(T), inventoryReference.ItemName);
            return enumResult;
        }

        [Test] [TestCase(SaveFiles.b_frigat)] public void test_EnumCalcsWithInventory(SaveFiles saveFiles)
        {
            GameReferences.Initialize(DataDirectory);

            LordBritishArtifact.ArtifactType artifact = GetEnumByName<LordBritishArtifact.ArtifactType>(
                GameReferences.InvRef.GetInventoryReference(
                    InventoryReferences.InventoryReferenceType.Item, "Crown"));
            Assert.AreEqual(artifact, LordBritishArtifact.ArtifactType.Crown);
        }

        [Test] [TestCase(SaveFiles.b_frigat)] public void test_CheckAllNonAttackingUnits(SaveFiles saveFiles)
        {
            GameReferences.Initialize(DataDirectory);

            foreach (KeyValuePair<string, List<InventoryReference>> kvp in GameReferences.InvRef._invRefsDictionary)
            {
                if (kvp.Value[0].InvRefType == InventoryReferences.InventoryReferenceType.Reagent) continue;
                foreach (InventoryReference invRef in kvp.Value)
                {
                    NonAttackingUnit nau =
                        NonAttackingUnitFactory.Create(invRef.ItemSpriteExposed, new MapUnitPosition());
                    InventoryItemFactory.Create(invRef);
                }
            }
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void test_LBChestCheck(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            Trace.Write("Starting ");
            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lord_Britishs_Castle, 0));

            // MapUnit mapUnit = world.State.TheVirtualMap.GetTopVisibleMapUnit(new Point2D(0, 0), true);
            // Assert.IsNotNull(mapUnit);
            //
            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lord_Britishs_Castle, -1));

            MapUnit mapUnit = world.State.TheVirtualMap.GetTopVisibleMapUnit(new Point2D(16, 21), true);
            Assert.IsNotNull(mapUnit);

            TestContext.Out.Write("Ending ");

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void test_BlackthorneThingCheck(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            Trace.Write("Starting ");
            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Palace_of_Blackthorn, 3));

            Point2D thingPosition = new Point2D(15, 13);
            bool bIsMapUnit = world.State.TheVirtualMap.IsMapUnitOccupiedTile(thingPosition);
            Assert.IsTrue(bIsMapUnit);
            MapUnit mapUnit = world.State.TheVirtualMap.GetTopVisibleMapUnit(thingPosition, true);
            Assert.IsNotNull(mapUnit);

            TestContext.Out.Write("Ending ");

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void test_CheckAndGetAllMapUnitsSmallMap(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            _ = "";
            foreach (SmallMapReferences.SingleMapReference smr in GameReferences.SmallMapRef.MapReferenceList)
            {
                SmallMapReferences.SingleMapReference singleMap =
                    GameReferences.SmallMapRef.GetSingleMapByLocation(smr.MapLocation, smr.Floor);
                // we don't test dungeon maps here
                if (singleMap.MapType == Map.Maps.Dungeon) continue;
                world.State.TheVirtualMap.LoadSmallMap(singleMap);

                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        Point2D thingPosition = new Point2D(i, j);

                        bool bIsMapUnit = world.State.TheVirtualMap.IsMapUnitOccupiedTile(thingPosition);
                        if (bIsMapUnit)
                        {
                            MapUnit mapUnit = world.State.TheVirtualMap.GetTopVisibleMapUnit(thingPosition, false);
                            Assert.IsNotNull(mapUnit);
                            if (mapUnit is ItemStack itemStack)
                            {
                                GameReferences.SpriteTileReferences.GetTileReference(itemStack.KeyTileReference.Index);
                            }
                        }
                    }
                }
            }

            TestContext.Out.Write("Ending ");

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void test_JimmyDoorLBBasement(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lord_Britishs_Castle, -1));

            Point2D avatarPos = new Point2D(15, 25);
            TurnResults turnResults = new TurnResults();
            List<VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos = world.TryToJimmyDoor(avatarPos,
                world.State.CharacterRecords.AvatarRecord, out bool bWasSuccessful,
                turnResults);

            TestContext.Out.Write("Ending ");

            Assert.True(true);
        }

        [Test] [TestCase(SaveFiles.Britain2)] public void test_SearchLBChest(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lord_Britishs_Castle, -1));

            Point2D avatarPos = new Point2D(14, 23);
            Point2D chestPos = new Point2D(13, 23);
            TurnResults turnResults = new TurnResults();

            List<VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos =
                world.TryToSearch(chestPos, out bool bWasSuccessful, turnResults);
            Assert.IsFalse(bWasSuccessful);

            aggressiveMapUnitInfos = world.TryToSearch(chestPos, out bWasSuccessful, turnResults);
            Assert.IsFalse(bWasSuccessful);

            aggressiveMapUnitInfos =
                world.TryToGetAThing(chestPos, out bool bGotAThing, out InventoryItem thingIGot, turnResults);
            Assert.IsFalse(bWasSuccessful);

            aggressiveMapUnitInfos = world.TryToOpenAThing(chestPos, out bWasSuccessful, turnResults);
            Assert.IsTrue(bWasSuccessful);
            aggressiveMapUnitInfos = world.TryToOpenAThing(chestPos, out bWasSuccessful, turnResults);
            Assert.IsFalse(bWasSuccessful);

            return;
            aggressiveMapUnitInfos =
                world.TryToGetAThing(chestPos, out bGotAThing, out thingIGot, turnResults);
            Assert.IsNotNull(thingIGot);
            aggressiveMapUnitInfos =
                world.TryToGetAThing(chestPos, out bGotAThing, out thingIGot, turnResults);
            Assert.IsNotNull(thingIGot);

            TestContext.Out.Write("Ending ");

            Assert.True(true);
        }

        // search all the things
        // open all the things
        // look at all the things

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_Dungeon71WithOpenDoor(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    71),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords);

            //TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;

            TurnResults turnResults = new TurnResults();
            // locked door
            world.TryToOpenAThing(new Point2D(5, 4), out bool bWasSuccessful, turnResults);
            Assert.IsFalse(bWasSuccessful);

            world.TryToJimmyDoor(new Point2D(5, 4), world.State.CharacterRecords.AvatarRecord, out bWasSuccessful,
                turnResults);
            Assert.IsTrue(bWasSuccessful);

            world.TryToOpenAThing(new Point2D(5, 4), out bWasSuccessful, turnResults);
            Assert.IsTrue(bWasSuccessful);

            CombatMap combatMap = world.State.TheVirtualMap.CurrentCombatMap;
            Assert.NotNull(combatMap);
            combatMap.DivideEnemy(combatMap.AllEnemies.ToList()[2]);

            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_Dungeon93(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    93),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords);

            //TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;

            TurnResults turnResults = new TurnResults();

            CombatMap combatMap = world.State.TheVirtualMap.CurrentCombatMap;
            Assert.NotNull(combatMap);
            combatMap.DivideEnemy(combatMap.AllEnemies.ToList()[2]);

            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_Dungeon93GetStuffAndMove(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            // force avatar to go first
            world.State.CharacterRecords.AvatarRecord.Stats.Dexterity = 100;

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    93),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords);

            //TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;
            Assert.IsNotNull(player);

            TurnResults turnResults = new TurnResults();

            CombatMap combatMap = world.State.TheVirtualMap.CurrentCombatMap;
            Assert.NotNull(combatMap);

            Point2D chestPosition = new Point2D(8, 9);
            world.TryToOpenAThing(chestPosition, out bool bWasSuccessful, turnResults);
            Assert.IsTrue(bWasSuccessful);

            ItemStack itemStack = world.State.TheVirtualMap.GetTopVisibleMapUnit(chestPosition, true) as ItemStack;

            while (itemStack.HasStackableItems)
            {
                StackableItem item = itemStack.PopStackableItem();
            }

            // put the avatar just up from the chest
            player.MapUnitPosition.XY = new Point2D(8, 8);
            world.TryToMoveCombatMap(player, Point2D.Direction.Down, turnResults);
            Assert.IsTrue(turnResults.PeekLastTurnResult.TheTurnResultType !=
                          TurnResult.TurnResultType.ActionMoveBlocked);

            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_Dungeon24LotsOfStuff(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            // force avatar to go first
            world.State.CharacterRecords.AvatarRecord.Stats.Dexterity = 100;

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    24),
                SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords);

            //TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;
            Assert.IsNotNull(player);

            TurnResults turnResults = new TurnResults();

            CombatMap combatMap = world.State.TheVirtualMap.CurrentCombatMap;
            Assert.NotNull(combatMap);

            Point2D gemPosition = new Point2D(7, 2);
            MapUnit gemMapUnit = world.State.TheVirtualMap.GetTopVisibleMapUnit(gemPosition, true);
            Assert.IsInstanceOf<DeadBody>(gemMapUnit);

            Point2D itemStackPosition = new Point2D(8, 2);
            MapUnit itemStackMapUnit = world.State.TheVirtualMap.GetTopVisibleMapUnit(itemStackPosition, true);
            Assert.IsInstanceOf<ItemStack>(itemStackMapUnit);
            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_Dungeon0StartLocation(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            // force avatar to go first
            world.State.CharacterRecords.AvatarRecord.Stats.Dexterity = 100;

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon, 0),
                SingleCombatMapReference.EntryDirection.East, world.State.CharacterRecords);

            //TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;
            Assert.IsNotNull(player);

            //Assert.AreEqual(player.MapUnitPosition.X, 3);

            TurnResults turnResults = new TurnResults();
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_Dungeon2HasFields(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            // force avatar to go first
            world.State.CharacterRecords.AvatarRecord.Stats.Dexterity = 100;

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon, 2),
                SingleCombatMapReference.EntryDirection.East, world.State.CharacterRecords);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;
            Assert.IsNotNull(player);

            MapUnit shouldBeFieldMapUnit = world.State.TheVirtualMap.GetTopVisibleMapUnit(new Point2D(5, 5), true);
            Assert.IsInstanceOf<CombatMapUnit>(shouldBeFieldMapUnit);

            TurnResults turnResults = new TurnResults();
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_Dungeon38HasWhirlpools(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            // force avatar to go first
            world.State.CharacterRecords.AvatarRecord.Stats.Dexterity = 100;

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    38),
                SingleCombatMapReference.EntryDirection.East, world.State.CharacterRecords);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;
            Assert.IsNotNull(player);

            //MapUnit shouldBeFieldMapUnit = world.State.TheVirtualMap.GetTopVisibleMapUnit(new Point2D(5, 5), true);
            //Assert.IsInstanceOf<CombatMapUnit>(shouldBeFieldMapUnit);

            TurnResults turnResults = new TurnResults();
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadAllDungeonCombatMaps(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            // force avatar to go first
            world.State.CharacterRecords.AvatarRecord.Stats.Dexterity = 100;

            for (int i = 0; i <= 111; i++)
            {
                world.State.TheVirtualMap.LoadCombatMap(
                    GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                        i),
                    SingleCombatMapReference.EntryDirection.East, world.State.CharacterRecords);
            }

            TurnResults turnResults = new TurnResults();
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadDungeon42CheckBatMovement(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            // force avatar to go first
            world.State.CharacterRecords.AvatarRecord.Stats.Dexterity = 1;

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    42),
                SingleCombatMapReference.EntryDirection.East, world.State.CharacterRecords);

            TurnResults turnResults = new TurnResults();

            world.State.TheVirtualMap.CurrentCombatMap.ProcessEnemyTurn(turnResults,
                out CombatMapUnit activeCombatMapUnit,
                out CombatMapUnit targetedCombatMapUnit,
                //out string preAttackOutputStr, out string postAttackOutputStr,
                out Point2D missedPoint);
            Assert.IsInstanceOf<Enemy>(activeCombatMapUnit);

            world.State.TheVirtualMap.CurrentCombatMap.ProcessEnemyTurn(turnResults,
                out activeCombatMapUnit, out targetedCombatMapUnit,
                //out preAttackOutputStr, out postAttackOutputStr,
                out missedPoint);
            Assert.IsInstanceOf<Enemy>(activeCombatMapUnit);

            world.State.TheVirtualMap.CurrentCombatMap.ProcessEnemyTurn(turnResults,
                out activeCombatMapUnit, out targetedCombatMapUnit,
                //out preAttackOutputStr, out postAttackOutputStr,
                out missedPoint);
            Assert.IsInstanceOf<Enemy>(activeCombatMapUnit);
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadDungeon58EnemyOnNonAttackingUnit(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            // force avatar to go first
            world.State.CharacterRecords.AvatarRecord.Stats.Dexterity = 1;

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    58), SingleCombatMapReference.EntryDirection.East, world.State.CharacterRecords);

            TurnResults turnResults = new TurnResults();
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LoadDungeon110BasicAttack(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);
            // force avatar to go first
            //world.State.CharacterRecords.AvatarRecord.Stats.Dexterity = 100;

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    110), SingleCombatMapReference.EntryDirection.South, world.State.CharacterRecords);

            TurnResults turnResults = new TurnResults();

            CombatMap combatMap = world.State.TheVirtualMap.CurrentCombatMap;

            Assert.IsInstanceOf<Enemy>(combatMap.GetAndRefreshCurrentCombatMapUnit());

            return;
        }

        [Test] public void Test_SurroundingPoints()
        {
            // force avatar to go first
            Point2D xy = new Point2D(8, 8);
            //xy.GetRandomSurroundingPointThatIsnt
            List<Point2D> surroundingCombatPlayerPoints =
                xy.GetConstrainedSurroundingPoints(1, 16, 16);
            Assert.IsTrue(surroundingCombatPlayerPoints.Count == 8);
            foreach (Point2D p in surroundingCombatPlayerPoints)
            {
                Assert.AreNotEqual(xy, p);
            }

            Point2D p1 = new Point2D(5, 5);
            List<Point2D> points = p1.GetConstrainedSurroundingPoints(1, 10, 10);
            List<Point2D> points2 = p1.GetConstrainedSurroundingPoints(4, 6, 6);
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_ClosestTileReferenceAround(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            int nClosest = world.State.TheVirtualMap.ClosestTileReferenceAround(
                GameReferences.SpriteTileReferences.GetTileReference(5), new Point2D(0, 0), 16);
            Assert.AreEqual(nClosest, 255);

            Point2D aroundBritain = new Point2D(82, 106);
            nClosest = world.State.TheVirtualMap.ClosestTileReferenceAround(
                GameReferences.SpriteTileReferences.GetTileReference(5), aroundBritain, 16);
            Assert.Less(nClosest, 255);

            nClosest = world.State.TheVirtualMap.ClosestTileReferenceAround(
                GameReferences.SpriteTileReferences.GetTileReference(6), aroundBritain, 2);
            Assert.AreEqual(nClosest, 255);

            nClosest = world.State.TheVirtualMap.ClosestTileReferenceAround(
                GameReferences.SpriteTileReferences.GetTileReference(6), aroundBritain, 8);
            Assert.Less(nClosest, 255);

            Point2D minoc = new Point2D(157, 20);
            nClosest = world.State.TheVirtualMap.ClosestTileReferenceAround(
                minoc, 8, GameReferences.SpriteTileReferences.IsFrigate);
            Assert.Less(nClosest, 255);

            nClosest = world.State.TheVirtualMap.ClosestTileReferenceAround(
                minoc, 2, GameReferences.SpriteTileReferences.IsFrigate);
            Assert.AreEqual(nClosest, 255);

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lord_Britishs_Castle, 0));

            // fountain
            Point2D courtYard = new Point2D(15, 19);
            nClosest = world.State.TheVirtualMap.ClosestTileReferenceAround(
                GameReferences.SpriteTileReferences.GetTileReference(216), courtYard, 8);
            Assert.Less(nClosest, 255);

            nClosest = world.State.TheVirtualMap.ClosestTileReferenceAround(
                GameReferences.SpriteTileReferences.GetTileReference(216), courtYard, 2);
            Assert.AreEqual(nClosest, 255);

            // brick in the corner
            nClosest = world.State.TheVirtualMap.ClosestTileReferenceAround(
                GameReferences.SpriteTileReferences.GetTileReference(68), Point2D.Zero, 8);
            Assert.Less(nClosest, 255);
        }

        [Test] [TestCase(SaveFiles.BucDen3)] public void Test_LoadLoadMoveSmallMap(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world = CreateWorldFromLegacy(saveFiles);

            //world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
            //world.State.TheVirtualMap.CurrentPosition.XY = new Point2D(166, 21);
            TurnResults turnResults = new TurnResults();
            // // we don't test dungeon maps here
            // if (singleMap.MapType == Map.Maps.Dungeon) continue;
            // world.State.TheVirtualMap.LoadSmallMap(singleMap);

            //
            world.TryToMove(Point2D.Direction.Up, false, false, turnResults);
            Assert.AreEqual(world.State.TheVirtualMap.CurrentPosition.XY,
                world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().MapUnitPosition.XY,
                $"CurrPos = {world.State.TheVirtualMap.CurrentPosition.X}, {world.State.TheVirtualMap.CurrentPosition.Y} but AvatarUnit = {world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().MapUnitPosition.XY.X}, {world.State.TheVirtualMap.TheMapUnits.GetAvatarMapUnit().MapUnitPosition.XY.Y}");
        }

        [Test] [TestCase(SaveFiles.blackt)] public void Test_BlacktLargeMapCheck(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            _ = "";
        }

        [Test] [TestCase(SaveFiles.blackt)] public void Test_AnimationFramesSane(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            TileReference water1 = GameReferences.SpriteTileReferences.GetTileReference(1);
            TileReference water2 = GameReferences.SpriteTileReferences.GetTileReference(2);

            Debug.Assert(water1.KeyTileTileReferenceIndex == 1);
            Debug.Assert(water2.KeyTileTileReferenceIndex == 1);

            for (int i = 0; i < 50; i++)
            {
                Debug.Assert(water1.GetRandomAnimationFrameIndex(out bool _) is 1 or 2);
                Debug.Assert(water2.GetRandomAnimationFrameIndex(out bool _) is 1 or 2);
            }
        }

        [Test] [TestCase(SaveFiles.Britain3)] public void Test_FoodSignInBritain(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            Point2D foodSignPos = new Point2D(13, 8);
            TileReference tileReference = world.State.TheVirtualMap.GetTileReference(foodSignPos);
            Assert.True(tileReference.Index == 240);
            int nGuessIndex = world.State.TheVirtualMap.GuessTile(foodSignPos);
            Assert.True(nGuessIndex == 49);
            int nNextGuess = world.State.TheVirtualMap.GuessTile(new Point2D(17, 19));
            Assert.True(nNextGuess == 48);

            nNextGuess = world.State.TheVirtualMap.GetAlternateFlatSprite(new Point2D(17, 19));
            Assert.True(nNextGuess == 48);

            world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(new Point2D(16, 9));

            TileStack ts = world.State.TheVirtualMap.GetTileStack(new Point2D(17, 19), true);
            _ = "";
        }


        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LBCastleTileOverride(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lord_Britishs_Castle, 3));

            Point2D flagPolePos = new Point2D(13, 15);
            TileReference tileReference = world.State.TheVirtualMap.GetTileReference(flagPolePos);
            Assert.True(tileReference.Index == 5);

            //int nGuessIndex = world.State.TheVirtualMap.GuessTile(foodSignPos);
            //Assert.True(nGuessIndex == 49);
            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_AraratTileOverrides(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Ararat, 1));

            Point2D cornerPosition = new Point2D(6, 19);
            // TileReference tileReference = world.State.TheVirtualMap.GetTileReference(flagPolePos);
            // Assert.True(tileReference.Index == 5);

            world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(cornerPosition);
            TileStack tileStack = world.State.TheVirtualMap.GetTileStack(cornerPosition, true);
            Assert.True(tileStack.TileReferencesDictionary.ContainsKey(5));

            int nNextGuess = world.State.TheVirtualMap.GetAlternateFlatSprite(cornerPosition);
            Assert.True(nNextGuess == 5);
            //int nGuessIndex = world.State.TheVirtualMap.GuessTile(foodSignPos);
            //Assert.True(nGuessIndex == 49);
            _ = "";
        }


        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_LycaeumOverrides(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lycaeum, 0));

            Point2D cornerPosition = new Point2D(23, 15);
            // TileReference tileReference = world.State.TheVirtualMap.GetTileReference(flagPolePos);
            // Assert.True(tileReference.Index == 5);

            world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(cornerPosition);
            TileStack tileStack = world.State.TheVirtualMap.GetTileStack(cornerPosition, true);
            Assert.True(tileStack.TileReferencesDictionary.ContainsKey(68));

            int nNextGuess = world.State.TheVirtualMap.GetAlternateFlatSprite(cornerPosition);
            Assert.True(nNextGuess == 68);
            //int nGuessIndex = world.State.TheVirtualMap.GuessTile(foodSignPos);
            //Assert.True(nGuessIndex == 49);
            _ = "";
        }

        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_AttackManacles(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            world.State.TheVirtualMap.LoadSmallMap(
                GameReferences.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Palace_of_Blackthorn, 1));

            Point2D avatarPosition = new Point2D(10, 1);
            world.State.TheVirtualMap.MoveAvatar(avatarPosition);
            //world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(avatarPosition);
            TurnResults turnResults = new TurnResults();
            List<VirtualMap.AggressiveMapUnitInfo> aggressiveMapUnitInfos =
                world.TryToAttackNonCombatMap(new Point2D(10, 10),
                    out MapUnit mapUnit, out SingleCombatMapReference singleCombatMapReference,
                    out World.TryToAttackResult tryToAttackResult, turnResults);
            Assert.True(tryToAttackResult == World.TryToAttackResult.NpcMurder);
            _ = "";
        }


        [Test] [TestCase(SaveFiles.b_carpet)] public void Test_HorizTombstoneCheck(SaveFiles saveFiles)
        {
            World world = CreateWorldFromLegacy(saveFiles);

            EnemyReference enemyReference =
                GameReferences.EnemyRefs.GetEnemyReference(GameReferences.SpriteTileReferences.GetTileReference(448));

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    58),
                SingleCombatMapReference.EntryDirection.East, world.State.CharacterRecords, enemyReference);

            bool bIsHoriz = world.State.TheVirtualMap.IsHorizTombstone(new Point2D(5, 3));
            Assert.True(bIsHoriz);

            world.State.TheVirtualMap.LoadCombatMap(
                GameReferences.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                    53),
                SingleCombatMapReference.EntryDirection.East, world.State.CharacterRecords, enemyReference);

            bIsHoriz = world.State.TheVirtualMap.IsHorizTombstone(new Point2D(4, 5));
            Assert.False(bIsHoriz);
        }
    }
}