using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Ultima5Redux;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers;
using Ultima5Redux.MapUnits.SeaFaringVessels;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.PlayerCharacters.Inventory;

// ReSharper disable UnusedVariable
// ReSharper disable RedundantAssignment
// ReSharper disable NotAccessedVariable
// ReSharper disable RedundantArgumentDefaultValue

namespace Ultima5ReduxTesting
{
    [TestFixture] public class Tests
    {
        private string SaveDirectory
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return @"/Users/bradhannah/games/u5/Gold";
                return @"C:\games\ultima5tests\Britain2";
            }
        }

        private string ActualSaveDirectory
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return @"/Users/bradhannah/games/u5tests";
                return @"C:\games\ultima5tests";
            }
        }

        [Test] public void AllSmallMapsLoadTest()
        {
            World world = new World(ActualSaveDirectory + @"\Britain");
            _ = "";
            foreach (SmallMapReferences.SingleMapReference smr in world.SmallMapRef.MapReferenceList)
            {
                world.State.TheVirtualMap.LoadSmallMap(
                    world.SmallMapRef.GetSingleMapByLocation(smr.MapLocation, smr.Floor));
            }

            Assert.True(true);
        }

        [Test] public void test_LoadTrinsicSmallMapsLoadTest()
        {
            World world = new World(ActualSaveDirectory + @"\Britain");
            _ = "";

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Trinsic, 0));

            Assert.True(true);
        }

        [Test] public void test_LoadMinocBuyFrigateAndCheck()
        {
            World world = new World(ActualSaveDirectory + @"\Britain");
            _ = "";

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Minoc, 0));
            world.State.TheVirtualMap.TheMapUnits.CreateFrigateAtDock(
                SmallMapReferences.SingleMapReference.Location.Minoc);
            Point2D dockLocation =
                VirtualMap.GetLocationOfDock(SmallMapReferences.SingleMapReference.Location.Minoc,
                    world.DataOvlRef);
            List<MapUnit> mapUnits = world.State.TheVirtualMap.TheMapUnits.GetMapUnitByLocation(
                Map.Maps.Overworld,
                dockLocation, 0);

            Frigate frigate2 = world.State.TheVirtualMap.TheMapUnits.GetSpecificMapUnitByLocation<Frigate>(
                Map.Maps.Overworld,
                dockLocation, 0);

            Debug.Assert(
                world.State.TheVirtualMap.IsShipOccupyingDock(SmallMapReferences.SingleMapReference.Location.Minoc));

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            world.State.TheVirtualMap.MoveAvatar(new Point2D(frigate2.TheMapUnitState.X, frigate2.TheMapUnitState.Y));
            string retStr = world.Board(out bool bWasSuccessful);
            Debug.Assert(bWasSuccessful);

            Assert.True(frigate2 != null);
            Assert.True(mapUnits[0] is Frigate);
            Assert.True(true);
        }

        [Test] public void test_LoadMinocBuySkiffAndCheck()
        {
            // World world = new World(SaveDirectory);
            World world = new World(ActualSaveDirectory + @"\Bucden3");
            _ = "";

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Minoc, 0));
            world.State.TheVirtualMap.TheMapUnits.CreateSkiffAtDock(
                SmallMapReferences.SingleMapReference.Location.Minoc);
            Point2D dockLocation =
                VirtualMap.GetLocationOfDock(SmallMapReferences.SingleMapReference.Location.Minoc,
                    world.DataOvlRef);
            List<MapUnit> mapUnits = world.State.TheVirtualMap.TheMapUnits.GetMapUnitByLocation(
                Map.Maps.Overworld,
                dockLocation, 0);

            Skiff skiff = world.State.TheVirtualMap.TheMapUnits.GetSpecificMapUnitByLocation<Skiff>(Map.Maps.Overworld,
                dockLocation, 0);

            Debug.Assert(
                world.State.TheVirtualMap.IsShipOccupyingDock(SmallMapReferences.SingleMapReference.Location.Minoc));

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            world.State.TheVirtualMap.MoveAvatar(new Point2D(skiff.TheMapUnitState.X, skiff.TheMapUnitState.Y));
            string retStr = world.Board(out bool bWasSuccessful);
            Debug.Assert(bWasSuccessful);

            Assert.True(skiff != null);
            Assert.True(mapUnits[0] is Skiff);
            Assert.True(true);
        }

        [Test] public void test_LoadSkaraBraeSmallMapsLoadTest()
        {
            World world = new World(ActualSaveDirectory + @"\Britain");
            _ = "";

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Skara_Brae, 0));

            Assert.True(true);
        }

        [Test] public void LoadBritishBasement()
        {
            World world = new World(SaveDirectory);

            Trace.Write("Starting ");
            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(
                    SmallMapReferences.SingleMapReference.Location.Lord_Britishs_Castle, 0));
            int i = (24 * (60 / 2));
            while (i > 0)
            {
                world.AdvanceTime(2);
                i--;
            }

            TestContext.Out.Write("Ending ");

            Assert.True(true);
        }


        [Test] public void AllSmallMapsLoadWithOneDayTest()
        {
            World world = new World(SaveDirectory);

            foreach (SmallMapReferences.SingleMapReference smr in world.SmallMapRef.MapReferenceList)
            {
                Debug.WriteLine("***** Loading " + smr.MapLocation + " on floor " + smr.Floor);
                world.State.TheVirtualMap.LoadSmallMap(
                    world.SmallMapRef.GetSingleMapByLocation(smr.MapLocation, smr.Floor));

                int i = (24 * (60 / 2));
                while (i > 0)
                {
                    world.AdvanceTime(2);
                    i--;
                }

                Debug.WriteLine("***** Ending " + smr.MapLocation + " on floor " + smr.Floor);
            }

            Assert.True(true);
        }

        [Test] public void Test_TileOverrides()
        {
            TileOverrides to = new TileOverrides();

            World world = new World(SaveDirectory);

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Lycaeum, 1));

            world.State.TheVirtualMap.GuessTile(new Point2D(14, 7));
        }


        [Test] public void Test_LoadOverworld()
        {
            World world = new World(SaveDirectory);

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
        }

        [Test] public void Test_LoadOverworldOverrideTile()
        {
            World world = new World(SaveDirectory);

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

        [Test] public void Test_PushPull_WontBudge()
        {
            World world = new World(SaveDirectory);

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Britain, 0));

            string pushAThing = world.PushAThing(new Point2D(5, 7), Point2D.Direction.Down, out bool bWasPushed);
            Assert.False(bWasPushed);
            Debug.WriteLine(pushAThing);

            pushAThing = world.PushAThing(new Point2D(22, 2), Point2D.Direction.Left, out bWasPushed);
            Assert.True(bWasPushed);
            Debug.WriteLine(pushAThing);

            pushAThing = world.PushAThing(new Point2D(2, 8), Point2D.Direction.Right, out bWasPushed);
            Assert.True(bWasPushed);
            Debug.WriteLine(pushAThing);
        }

        [Test] public void Test_FreeMoveAcrossWorld()
        {
            World world = new World(SaveDirectory);

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            Point2D startLocation = world.State.TheVirtualMap.CurrentPosition.XY.Copy();

            for (int i = 0; i < 256; i++)
            {
                world.TryToMove(Point2D.Direction.Up, false, true, out World.TryToMoveResult moveResult);
            }

            Point2D finalLocation = world.State.TheVirtualMap.CurrentPosition.XY.Copy();

            Assert.True(finalLocation == startLocation);
        }

        [Test] public void Test_CheckAlLTilesForMoongates()
        {
            World world = new World(SaveDirectory);

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

        [Test] public void Test_LookupMoonstoneInInventory()
        {
            World world = new World(SaveDirectory);

            foreach (MoonPhaseReferences.MoonPhases phase in Enum.GetValues(typeof(MoonPhaseReferences.MoonPhases)))
            {
                if (phase == MoonPhaseReferences.MoonPhases.NoMoon) continue;
                bool bBuried = world.State.TheMoongates.IsMoonstoneBuried((int)phase);
                int nMoonstonesInInv = world.State.PlayerInventory.TheMoonstones.Items[phase].Quantity;
                string desc = world.InvRef.GetInventoryReference(InventoryReferences.InventoryReferenceType.Item,
                    phase.ToString()).ItemDescription;
            }
        }

        [Test] public void Test_GetAndUseMoonstone()
        {
            World world = new World(SaveDirectory);
            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
            world.State.TheTimeOfDay.Hour = 12;

            Point2D moongatePosition = new Point2D(166, 19);
            world.State.TheVirtualMap.CurrentPosition.XY = moongatePosition;
            // first search should find moonstone
            world.TryToSearch(moongatePosition, out bool bWasSuccessful);
            Debug.Assert(bWasSuccessful);

            world.TryToGetAThing(moongatePosition, out bWasSuccessful, out InventoryItem item);
            Debug.Assert(bWasSuccessful);
            Debug.Assert(item != null);
            Debug.Assert(item.GetType() == typeof(Moonstone));

            string useStr = world.UseMoonstone((Moonstone)item, out bWasSuccessful);
            Debug.Assert(bWasSuccessful);
        }


        [Test] public void Test_SearchForMoonstoneAndGet()
        {
            World world = new World(SaveDirectory);
            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);

            world.State.TheTimeOfDay.Hour = 12;

            Point2D moongatePosition = new Point2D(166, 19);
            // first search should find moonstone
            world.TryToSearch(moongatePosition, out bool bWasSuccessful);
            Debug.Assert(bWasSuccessful);
            // second search should be empty
            world.TryToSearch(moongatePosition, out bWasSuccessful);
            Debug.Assert(!bWasSuccessful);

            TileReference tileRef = world.State.TheVirtualMap.GetTileReference(moongatePosition);
            Debug.Assert(tileRef.Index == 281);

            int nSprite = world.State.TheVirtualMap.GuessTile(moongatePosition);

            // can't get it twice!
            world.TryToGetAThing(moongatePosition, out bWasSuccessful, out InventoryItem item);
            Debug.Assert(bWasSuccessful);
            Debug.Assert(item != null);
            world.TryToGetAThing(moongatePosition, out bWasSuccessful, out item);
            Debug.Assert(!bWasSuccessful);
            Debug.Assert(item == null);

            world.TryToSearch(moongatePosition, out bWasSuccessful);
            Debug.Assert(!bWasSuccessful);
        }

        [Test] public void Test_MoongateHunting()
        {
            World world = new World(SaveDirectory);

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

        [Test] public void Test_TestCorrectMoons()
        {
            World world = new World(SaveDirectory);

            MoonPhaseReferences moonPhaseReferences = new MoonPhaseReferences(world.DataOvlRef);

            world.State.TheTimeOfDay.Day = 25;
            world.State.TheTimeOfDay.Hour = 4;

            MoonPhaseReferences.MoonPhases trammelPhase =
                moonPhaseReferences.GetMoonPhasesByTimeOfDay(world.State.TheTimeOfDay,
                    MoonPhaseReferences.MoonsAndSun.Trammel);
            MoonPhaseReferences.MoonPhases feluccaPhase =
                moonPhaseReferences.GetMoonPhasesByTimeOfDay(world.State.TheTimeOfDay,
                    MoonPhaseReferences.MoonsAndSun.Felucca);
            Debug.Assert(trammelPhase == MoonPhaseReferences.MoonPhases.GibbousWaning);
            Debug.Assert(feluccaPhase == MoonPhaseReferences.MoonPhases.LastQuarter);
        }

        [Test] public void Test_MoonPhaseReference()
        {
            World world = new World(SaveDirectory);

            MoonPhaseReferences moonPhaseReferences = new MoonPhaseReferences(world.DataOvlRef);

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

        [Test] public void Test_TalkToSmith()
        {
            World world = new World(SaveDirectory);
            List<NonPlayerCharacterReference> iolosHutRefs =
                world.NpcRef.GetNonPlayerCharactersByLocation(SmallMapReferences.SingleMapReference.Location.Iolos_Hut);

            NonPlayerCharacterReference smithTheHorseRef = iolosHutRefs[4];

            world.CreateConversationAndBegin(smithTheHorseRef,
                OnUpdateOfEnqueuedScriptItem);
        }

        private static void OnUpdateOfEnqueuedScriptItem(Conversation conversation)
        {
        }

        [Test] public void Test_KlimbMountain()
        {
            World world = new World(SaveDirectory);

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
            world.State.TheVirtualMap.CurrentPosition.XY = new Point2D(166, 21);
            world.TryToKlimb(out World.KlimbResult klimbResult);
            Debug.Assert(klimbResult == World.KlimbResult.RequiresDirection);
        }

        [Test] public void Test_MoveALittle()
        {
            World world = new World(SaveDirectory);

            world.State.TheVirtualMap.LoadLargeMap(Map.Maps.Overworld);
            world.State.TheVirtualMap.CurrentPosition.XY = new Point2D(166, 21);

            world.TryToMove(Point2D.Direction.Up, false, false, out World.TryToMoveResult tryToMoveResult);
        }

        [Test] public void Test_TalkToDelwyn()
        {
            World world = new World(SaveDirectory);
            List<NonPlayerCharacterReference> minocNpcRef =
                world.NpcRef.GetNonPlayerCharactersByLocation(SmallMapReferences.SingleMapReference.Location.Minoc);

            NonPlayerCharacterReference delwynRef = minocNpcRef[9];

            Conversation convo = world.CreateConversationAndBegin(delwynRef,
                OnUpdateOfEnqueuedScriptItemHandleDelwyn);
            convo.BeginConversation();
            convo.AddUserResponse("yes");
            convo.AddUserResponse("yes");
            convo.AddUserResponse("yes");
            convo.AddUserResponse("bye");

            Conversation convo2 = world.CreateConversationAndBegin(delwynRef,
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

        [Test] public void Test_BasicBlackSmithDialogue()
        {
            World world = new World(SaveDirectory);
            BlackSmith blacksmith = world.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Minoc,
                NonPlayerCharacterReference.NPCDialogTypeEnum.Blacksmith, null) as BlackSmith;

            Debug.Assert(blacksmith != null, nameof(blacksmith) + " != null");
            string purchaseStr2 = blacksmith.GetEquipmentBuyingOutput(DataOvlReference.Equipment.LeatherHelm, 100);
            string purchaseStr = blacksmith.GetEquipmentBuyingOutput(DataOvlReference.Equipment.Amuletofturning, 100);

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

        [Test] public void Test_BasicHealerDialogue()
        {
            World world = new World(SaveDirectory);
            Healer healer = (Healer)world.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Cove,
                NonPlayerCharacterReference.NPCDialogTypeEnum.Healer, null);

            _ = healer.NoNeedForMyArt();

            int price = healer.GetPrice(Healer.RemedyTypes.Heal);
        }

        [Test] public void Test_BasicTavernDialogue()
        {
            World world = new World(SaveDirectory);
            BarKeeper barKeeper = (BarKeeper)world.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Paws,
                NonPlayerCharacterReference.NPCDialogTypeEnum.Barkeeper, null);

            string myOppressionTest = barKeeper.GetGossipResponse("oppr", true);
        }

        [Test] public void Test_BasicMagicSellerDialogue()
        {
            World world = new World(SaveDirectory);
            MagicSeller magicSeller = (MagicSeller)world.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Cove,
                NonPlayerCharacterReference.NPCDialogTypeEnum.MagicSeller, null);
            List<Reagent> reagents = magicSeller.GetReagentsForSale();

            int price1 = reagents[0].GetAdjustedBuyPrice(world.State.CharacterRecords,
                SmallMapReferences.SingleMapReference.Location.Cove);
            int price2 = reagents[1].GetAdjustedBuyPrice(world.State.CharacterRecords,
                SmallMapReferences.SingleMapReference.Location.Cove);

            string hello = magicSeller.GetHelloResponse(world.State.TheTimeOfDay);
            string buything = magicSeller.GetReagentBuyingOutput(reagents[0]);
            buything = magicSeller.GetReagentBuyingOutput(reagents[1]);
            buything = magicSeller.GetReagentBuyingOutput(reagents[2]);
            buything = magicSeller.GetReagentBuyingOutput(reagents[3]);
            buything = magicSeller.GetReagentBuyingOutput(reagents[4]);
        }

        [Test] public void Test_AdjustedMerchantPrices()
        {
            World world = new World(SaveDirectory);

            int nCrossbowBuy = world.State.PlayerInventory.TheWeapons.Items[Weapon.WeaponTypeEnum.Crossbow]
                .GetAdjustedBuyPrice(world.State.CharacterRecords,
                    ((RegularMap)world.State.TheVirtualMap.CurrentMap).CurrentSingleMapReference.MapLocation);
            int nCrossbowSell = world.State.PlayerInventory.TheWeapons.Items[Weapon.WeaponTypeEnum.Crossbow]
                .GetAdjustedSellPrice(world.State.CharacterRecords,
                    ((RegularMap)world.State.TheVirtualMap.CurrentMap).CurrentSingleMapReference.MapLocation);
            Debug.Assert(nCrossbowBuy > 0);
            Debug.Assert(nCrossbowSell > 0);

            int nKeysPrice = world.State.PlayerInventory.TheProvisions.Items[Provision.ProvisionTypeEnum.Keys]
                .GetAdjustedBuyPrice(world.State.CharacterRecords,
                    SmallMapReferences.SingleMapReference.Location.Buccaneers_Den);
            GuildMaster guildMaster = (GuildMaster)world.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Buccaneers_Den,
                NonPlayerCharacterReference.NPCDialogTypeEnum.GuildMaster, null);
            string buyKeys = guildMaster.GetProvisionBuyOutput(Provision.ProvisionTypeEnum.Keys, 240);
        }


        [Test] public void Test_SimpleStringTest()
        {
            World world = new World(SaveDirectory);
            string str = world.DataOvlRef.StringReferences.GetString(DataOvlReference.Battle2Strings.N_VICTORY_BANG_N);
        }

        [Test] public void Test_ShipwrightDialogue()
        {
            World world = new World(SaveDirectory);
            Shipwright shipwright = (Shipwright)world.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Buccaneers_Den,
                NonPlayerCharacterReference.NPCDialogTypeEnum.Shipwright, null);

            string hi = shipwright.GetHelloResponse(world.State.TheTimeOfDay);
        }

        public void Test_ShipInWorldState()
        {
            World world = new World(SaveDirectory);
        }

        public void Test_Gossip()
        {
            World world = new World(SaveDirectory);
        }

        [Test] public void Test_EnterBuilding()
        {
            World world = new World(ActualSaveDirectory + @"\Britain");
            _ = "";

            world.EnterBuilding(new Point2D(159, 20), out bool bWasSuccessful);

            Assert.True(true);
        }

        [Test] public void Test_EnterYewAndLookAtMonster()
        {
            World world = new World(ActualSaveDirectory + @"\Britain");
            _ = "";

            // yew
            world.EnterBuilding(new Point2D(58, 43), out bool bWasSuccessful);

            foreach (MapUnit mapUnit in world.State.TheVirtualMap.TheMapUnits.CurrentMapUnits)
            {
                _ = mapUnit.NonBoardedTileReference;
            }

            Assert.True(true);
        }

        [Test] public void Test_GetInnStuffAtBucDen()
        {
            // World world = new World(SaveDirectory);
            World world = new World(ActualSaveDirectory + @"\Bucden1");
            _ = "";

            world.EnterBuilding(new Point2D(159, 20), out bool bWasSuccessful);

            Innkeeper innKeeper = (Innkeeper)world.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Buccaneers_Den,
                NonPlayerCharacterReference.NPCDialogTypeEnum.InnKeeper, null);

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
            string howmuch = innKeeper.GetThatWillBeGold(activeRecords[1]);
            string shoppename = innKeeper.TheShoppeKeeperReference.ShoppeName;

            _ = world.State.CharacterRecords.GetCharacterFromParty(5);
            Assert.True(true);
        }

        [Test] public void Test_GetInnStuffAtBritain()
        {
            // World world = new World(SaveDirectory);
            World world = new World(ActualSaveDirectory + @"\Britain3");
            _ = "";

            world.EnterBuilding(new Point2D(159, 20), out bool bWasSuccessful);

            Innkeeper innKeeper = (Innkeeper)world.ShoppeKeeperDialogueReference.GetShoppeKeeper(
                SmallMapReferences.SingleMapReference.Location.Britain,
                NonPlayerCharacterReference.NPCDialogTypeEnum.InnKeeper, null);

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

        [Test] public void Test_MakeAHorse()
        {
            // World world = new World(SaveDirectory);
            World world = new World(ActualSaveDirectory + @"\Britain");
            _ = "";

            world.EnterBuilding(new Point2D(159, 20), out bool bWasSuccessful);

            Horse horse = world.State.TheVirtualMap.CreateHorseAroundAvatar();
            Assert.True(horse != null);
        }

        [Test] public void Test_MoveWithExtendedSprites()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet", true);

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.AvatarMapUnit;

            string retStr = world.TryToMove(Point2D.Direction.Down, false, false, out World.TryToMoveResult result,
                true);
            // make sure it is using the extended sprite
            Debug.Assert(world.State.TheVirtualMap.TheMapUnits.AvatarMapUnit.CurrentBoardedMapUnit.BoardedTileReference
                .Index == 515);
        }

        [Test] public void Test_CheckedBoardedTileCarpet()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet");

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.AvatarMapUnit;
            Debug.Assert(avatar.IsAvatarOnBoardedThing);
            Debug.Assert(avatar.CurrentBoardedMapUnit != null);

            int nCarpets = world.State.PlayerInventory.MagicCarpets;
            world.Xit(out bool bWasSuccessful);
            Debug.Assert(nCarpets == world.State.PlayerInventory.MagicCarpets);
            Debug.Assert(bWasSuccessful);
            world.Board(out bool bWasSuccessfulBoard);
            Debug.Assert(bWasSuccessfulBoard);
            world.Xit(out bWasSuccessful);

            nCarpets = world.State.PlayerInventory.MagicCarpets;
            Point2D curPos = world.State.TheVirtualMap.CurrentPosition.XY;
            world.TryToMove(Point2D.Direction.Left, false, false, out World.TryToMoveResult result);
            world.TryToGetAThing(curPos, out bool bGotACarpet, out InventoryItem carpet);
            Debug.Assert(bGotACarpet);
            //Debug.Assert(carpet!=null);
            Debug.Assert(nCarpets + 1 == world.State.PlayerInventory.MagicCarpets);
            world.TryToGetAThing(curPos, out bGotACarpet, out carpet);
            Debug.Assert(!bGotACarpet);

            world.UseSpecialItem(
                world.State.PlayerInventory.SpecializedItems.Items[SpecialItem.ItemTypeSpriteEnum.Carpet],
                out bool bAbleToUseItem);
            Debug.Assert(bAbleToUseItem);
            world.Xit(out bWasSuccessful);
            Debug.Assert(bWasSuccessful);
            world.TryToMove(Point2D.Direction.Left, false, false, out result);
            curPos = world.State.TheVirtualMap.CurrentPosition.XY;
            string retStr = world.TryToGetAThing(curPos, out bGotACarpet, out carpet);
            Debug.Assert(bGotACarpet);

            _ = "";
        }


        [Test] public void Test_CheckUseCarpet()
        {
            World world = new World(ActualSaveDirectory + @"\Bucden3");

            int nCarpets = world.State.PlayerInventory.MagicCarpets;
            world.UseSpecialItem(
                world.State.PlayerInventory.SpecializedItems.Items[SpecialItem.ItemTypeSpriteEnum.Carpet],
                out bool bAbleToUseItem);
            Debug.Assert(bAbleToUseItem);
            Debug.Assert(world.State.PlayerInventory.MagicCarpets == nCarpets - 1);
        }

        [Test] public void Test_CheckedBoardedTileHorse()
        {
            World world = new World(ActualSaveDirectory + @"\b_horse");

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.AvatarMapUnit;
            Debug.Assert(avatar.IsAvatarOnBoardedThing);
            Debug.Assert(avatar.CurrentBoardedMapUnit != null);

            world.Xit(out bool bWasSuccessful);

            _ = "";
        }

        [Test] public void Test_CheckedBoardedTileSkiff()
        {
            // World world = new World(SaveDirectory);
            World world = new World(ActualSaveDirectory + @"\b_skiff");

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.AvatarMapUnit;
            Debug.Assert(avatar.IsAvatarOnBoardedThing);
            Debug.Assert(avatar.CurrentBoardedMapUnit != null);

            world.Xit(out bool bWasSuccessful);

            _ = "";
        }

        [Test] public void Test_CheckedBoardedTileSkiffMoveOntoSkiff()
        {
            // World world = new World(SaveDirectory);
            World world = new World(ActualSaveDirectory + @"\b_skiff");

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.AvatarMapUnit;
            Debug.Assert(avatar.IsAvatarOnBoardedThing);
            Debug.Assert(avatar.CurrentBoardedMapUnit != null);

            world.TryToMove(Point2D.Direction.Down, false, true, out World.TryToMoveResult moveResult);
            Debug.Assert(moveResult == World.TryToMoveResult.Blocked);
            _ = "";
        }

        [Test] public void Test_CheckedBoardedTileFrigate()
        {
            // World world = new World(SaveDirectory);
            World world = new World(ActualSaveDirectory + @"\b_frigat");

            Avatar avatar = world.State.TheVirtualMap.TheMapUnits.AvatarMapUnit;
            Debug.Assert(avatar.IsAvatarOnBoardedThing);
            Debug.Assert(avatar.CurrentBoardedMapUnit != null);

            world.Xit(out bool bWasSuccessful);

            _ = "";
        }

        [Test] public void Test_ForceVisibleRecalculationInBucDen()
        {
            World world = new World(ActualSaveDirectory + @"\bucden1");
            Point2D startSpot = new Point2D(159, 20);
            world.EnterBuilding(startSpot, out bool bWasSuccessful);

            world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition.XY);
            _ = "";
        }

        [Test] public void Test_ForceVisibleRecalculationInLargeMap()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet");

            world.State.TheVirtualMap.MoveAvatar(new Point2D(128, 0));
            world.State.TheVirtualMap.CurrentMap.RecalculateVisibleTiles(world.State.TheVirtualMap.CurrentPosition.XY);
        }


        [Test] public void Test_LookupPrimaryAndSecondaryEnemyReferences()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet");

            EnemyReference enemyReference =
                world.EnemyRefs.GetEnemyReference(world.SpriteTileReferences.GetTileReference(448));

            world.State.TheVirtualMap.LoadCombatMap(
                world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, 0),
                SingleCombatMapReference.EntryDirection.Direction2, world.State.CharacterRecords, enemyReference);

            EnemyReference secondEnemyReference = world.EnemyRefs.GetFriendReference(enemyReference);
            //GetEnemyReference(enemyReference.FriendIndex);
        }

        [Test] public void Test_LoadCampFireCombatMap()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet");

            world.State.TheVirtualMap.LoadCombatMap(
                world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, 0),
                SingleCombatMapReference.EntryDirection.Direction2, world.State.CharacterRecords,
                // orcs
                world.EnemyRefs.GetEnemyReference(world.SpriteTileReferences.GetTileReference(448)), 5,
                // troll
                world.EnemyRefs.GetEnemyReference(world.SpriteTileReferences.GetTileReference(484)), 1);
            
            TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);

            // CombatMap.TurnResult turnResult = world.State.TheVirtualMap.CurrentCombatMap.ProcessEnemyTurn(
            //     out CombatMapUnit combatMapUnit, out _, out string outputStr, out string postAttackOutputStr, out _);
            // Debug.Assert(turnResult == CombatMap.TurnResult.RequireCharacterInput);
            // Debug.Assert(combatMapUnit is CombatPlayer);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;
            
            Debug.Assert(player.Record.Class == PlayerCharacterRecord.CharacterClass.Avatar);
            world.TryToMoveCombatMap(Point2D.Direction.Up, out World.TryToMoveResult tryToMoveResult, true);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Left, out tryToMoveResult, true);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Up, out tryToMoveResult, true);
            Debug.Assert(tryToMoveResult == World.TryToMoveResult.Blocked);

            _ = "";
        }

        [Test] public void Test_HammerCombatMapInitiativeTest()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet");

            // right sided hammer
            world.State.TheVirtualMap.LoadCombatMap(
                world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon, 4),
                SingleCombatMapReference.EntryDirection.Direction2, world.State.CharacterRecords);

            // CombatMap.TurnResult turnResult = world.State.TheVirtualMap.CurrentCombatMap.ProcessEnemyTurn(
            //     out CombatMapUnit combatMapUnit, out _, out string outputStr, out string postAttackOutputStr, out _);
            // Debug.Assert(turnResult == CombatMap.TurnResult.RequireCharacterInput);
            // Debug.Assert(combatMapUnit is CombatPlayer);

            world.TryToMoveCombatMap(Point2D.Direction.Up, out World.TryToMoveResult tryToMoveResult, true);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Left, out tryToMoveResult, true);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Up, out tryToMoveResult, true);
            _ = "";
        }

        [Test] public void Test_EscapeCombatMap()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet");

            world.State.TheVirtualMap.LoadCombatMap(
                world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, 4),
                SingleCombatMapReference.EntryDirection.Direction2, world.State.CharacterRecords);

            // CombatMap.TurnResult turnResult = world.State.TheVirtualMap.CurrentCombatMap.ProcessEnemyTurn(
            //     out CombatMapUnit combatMapUnit, out _, out string outputStr, out string postAttackOutputStr, out _);
            // Debug.Assert(turnResult == CombatMap.TurnResult.RequireCharacterInput);
            // Debug.Assert(combatMapUnit is CombatPlayer);

            world.TryToMoveCombatMap(Point2D.Direction.Up, out World.TryToMoveResult tryToMoveResult, true);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Left, out tryToMoveResult, true);
            world.State.TheVirtualMap.CurrentCombatMap.AdvanceToNextCombatMapUnit();
            world.TryToMoveCombatMap(Point2D.Direction.Up, out tryToMoveResult, true);

            do
            {
                world.State.TheVirtualMap.CurrentCombatMap.NextCharacterEscape(out CombatPlayer combatPlayer);
                CombatPlayer newCombatPlayer = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer;

                if (combatPlayer == null) break;
            } while (true);

            _ = "";
        }

        [Test] public void Test_LoadFirstCombatMap()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet");

            world.State.TheVirtualMap.LoadCombatMap(
                world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, 11),
                SingleCombatMapReference.EntryDirection.Direction2, world.State.CharacterRecords,
                // orcs
                world.EnemyRefs.GetEnemyReference(world.SpriteTileReferences.GetTileReference(448)), 5,
                // troll
                world.EnemyRefs.GetEnemyReference(world.SpriteTileReferences.GetTileReference(484)), 1);
            TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);

            CombatPlayer player = world.State.TheVirtualMap.CurrentCombatMap.CurrentCombatPlayer; 
            {
                Debug.Assert(player.Record.Class == PlayerCharacterRecord.CharacterClass.Avatar);
                world.TryToMoveCombatMap(player, Point2D.Direction.Up, out World.TryToMoveResult tryToMoveResult,
                    true);
            }

            _ = "";
        }

        [Test] public void Test_LoadCombatMapThenBack()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet");

            world.EnterBuilding(new Point2D(159, 20), out bool bWasSuccessful);

            world.State.TheVirtualMap.LoadCombatMap(
                world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, 2),
                SingleCombatMapReference.EntryDirection.Direction2, world.State.CharacterRecords,
                // orcs
                world.EnemyRefs.GetEnemyReference(world.SpriteTileReferences.GetTileReference(448)), 5,
                // troll
                world.EnemyRefs.GetEnemyReference(world.SpriteTileReferences.GetTileReference(484)), 1);

            var thing = world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, 2)
                .GetEntryDirections();

            world.State.TheVirtualMap.ReturnToPreviousMapAfterCombat();

            _ = "";
        }

        [Test] public void Test_LoadAllCombatMapsWithMonsters()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet");
            List<CombatMap> worldMaps = new List<CombatMap>();
            for (int i = 0; i < 16; i++)
            {
                SingleCombatMapReference singleCombatMapReference =
                    world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, i);
                foreach (SingleCombatMapReference.EntryDirection worldEntryDirection in singleCombatMapReference
                    .GetEntryDirections())
                {
                    world.State.TheVirtualMap.LoadCombatMap(
                        world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia,
                            i),
                        worldEntryDirection, world.State.CharacterRecords);
                    TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);
                    worldMaps.Add(world.State.TheVirtualMap.CurrentCombatMap);
                }
            }

            EnemyReference enemy1 = world.EnemyRefs.GetEnemyReference(world.SpriteTileReferences.GetTileReference(320));

            List<CombatMap> dungMaps = new List<CombatMap>();
            for (int i = 0; i < 112; i++)
            {
                SingleCombatMapReference singleCombatMapReference =
                    world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon, i);

//                Debug.Assert(singleCombatMapReference.GetEntryDirections().Count > 0);
                if (singleCombatMapReference.GetEntryDirections().Count == 0)
                {
                    List<SingleCombatMapReference.EntryDirection> dirs = singleCombatMapReference.GetEntryDirections();
                }

                foreach (SingleCombatMapReference.EntryDirection dungeonEntryDirection in singleCombatMapReference
                    .GetEntryDirections())
                {
                    world.State.TheVirtualMap.LoadCombatMap(
                        world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Dungeon,
                            i),
                        dungeonEntryDirection, world.State.CharacterRecords, enemy1, 4);
                    TileReference tileReference = world.State.TheVirtualMap.GetTileReference(0, 0);
                    dungMaps.Add(world.State.TheVirtualMap.CurrentCombatMap);
                }
            }

            _ = "";
        }

        [Test] public void Test_GetSurroundingPoints()
        {
            Point2D p1 = new Point2D(5, 5);
            List<Point2D> points = p1.GetConstrainedSurroundingPoints(1, 10, 10);
            List<Point2D> points2 = p1.GetConstrainedSurroundingPoints(4, 6, 6);
            Debug.WriteLine("DERP");
        }

        [Test] public void Test_GetEscapablePoints()
        {
            World world = new World(ActualSaveDirectory + @"\b_carpet");

            world.State.TheVirtualMap.LoadCombatMap(
                world.CombatMapRefs.GetSingleCombatMapReference(SingleCombatMapReference.Territory.Britannia, 15),
                SingleCombatMapReference.EntryDirection.Direction2, world.State.CharacterRecords,
                // orcs
                world.EnemyRefs.GetEnemyReference(world.SpriteTileReferences.GetTileReference(448)), 5,
                // troll
                world.EnemyRefs.GetEnemyReference(world.SpriteTileReferences.GetTileReference(484)), 1);

            List<Point2D> points =
                world.State.TheVirtualMap.CurrentCombatMap.GetEscapablePoints(new Point2D(12, 12),
                    Map.WalkableType.CombatLand);
            _ = "";
        }

        [Test] public void Test_ExtentCheck()
        {
            Point2D point = new Point2D(15, 15);
            var constrainedPoints = point.GetConstrainedSurroundingPoints(1, 15, 15);
            Debug.Assert(constrainedPoints.Count == 2);
            _ = "";
        }
    }
}