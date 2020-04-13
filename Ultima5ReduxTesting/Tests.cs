using System;
using System.Diagnostics;
using NUnit.Framework;
using Ultima5Redux;
using System.Collections.Generic;
using System.Media;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Dialogue;
using Ultima5Redux.MapCharacters;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5ReduxTesting
{
    [TestFixture]
    public class Tests
    {
        private string SaveDirectory
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return @"/Users/bradhannah/games/u5/Gold";
                return @"C:\games\ultima_5_late\Britain";
                //return @"C:\games\ultima_5\Gold";
            }
            
        }
        
        [Test]
        public void AllSmallMapsLoadTest()
        {

            World world = new World(SaveDirectory);

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
            World world = new World(SaveDirectory);

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
            World world = new World(SaveDirectory);

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
            
            World world = new World(SaveDirectory);

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Lycaeum, 1), world.State.CharacterRecords,
                false);

            world.State.TheVirtualMap.GuessTile(new Point2D(14, 7));
        }


        [Test]
        public void Test_LoadOverworld()
        {
            World world = new World(SaveDirectory);

            Trace.Write("Starting ");

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
        }

        [Test]
        public void Test_LoadOverworldOverrideTile()
        {
            World world = new World(SaveDirectory);

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
                str = invRefs.HighlightKeywords(str);
            }
        }

        [Test]
        public void Test_PushPull_WontBudge()
        {
            World world = new World(SaveDirectory);

            world.State.TheVirtualMap.LoadSmallMap(
                world.SmallMapRef.GetSingleMapByLocation(SmallMapReferences.SingleMapReference.Location.Britain, 0), world.State.CharacterRecords,
                false);

            string pushAThing = world.PushAThing(new Point2D(5, 7), VirtualMap.Direction.Down, out bool bWasPushed);
            Assert.False(bWasPushed);
            Debug.WriteLine(pushAThing);
            
            pushAThing = world.PushAThing(new Point2D(22, 2), VirtualMap.Direction.Left, out bWasPushed);
            Assert.True(bWasPushed);
            Debug.WriteLine(pushAThing);
            
            pushAThing = world.PushAThing(new Point2D(2, 8), VirtualMap.Direction.Right, out bWasPushed);
            Assert.True(bWasPushed);
            Debug.WriteLine(pushAThing);
        }

        [Test]
        public void Test_FreeMoveAcrossWorld()
        {
            World world = new World(SaveDirectory);

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
            
            Point2D startLocation = world.State.TheVirtualMap.CurrentPosition.Copy();
            
            for (int i = 0; i < 256; i++)
            {
                world.TryToMove(VirtualMap.Direction.Up, false, true, out World.TryToMoveResult moveResult);
            }
            Point2D finalLocation = world.State.TheVirtualMap.CurrentPosition.Copy();
            
            Assert.True(finalLocation == startLocation);
        }
        
        [Test]
        public void Test_CheckAlLTilesForMoongates()
        {
            World world = new World(SaveDirectory);

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);

            Point2D startLocation = world.State.TheVirtualMap.CurrentPosition.Copy();
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    TileReference tileReference = world.State.TheVirtualMap.GetTileReference(x, y);
                }
            }

            Point2D finalLocation = world.State.TheVirtualMap.CurrentPosition.Copy();
            
            Assert.True(finalLocation == startLocation);
        }

        [Test]
        public void Test_LookupMoonstoneInInventory()
        {
            World world = new World(SaveDirectory);

            //for (int i = 0; i < 8; i++)
            foreach (MoonPhaseReferences.MoonPhases phase in Enum.GetValues(typeof(MoonPhaseReferences.MoonPhases)))
            {
                if (phase == MoonPhaseReferences.MoonPhases.NoMoon) continue;
                bool bBuried = world.State.TheMoongates.IsMoonstoneBuried((int)phase);
                int nMoonstonesInInv = world.State.PlayerInventory.TheMoonstones.Items[phase].Quantity;
                string desc = world.InvRef.GetInventoryReference(InventoryReferences.InventoryReferenceType.Item,
                    phase.ToString()).ItemDescription;
            }
        }

        [Test]
        public void Test_GetAndUseMoonstone()
        {
            World world = new World(SaveDirectory);

            world.State.TheTimeOfDay.Hour = 12;

            Point2D moongatePosition = new Point2D(166, 19);
            world.State.TheVirtualMap.CurrentPosition = moongatePosition;
            // first search should find moonstone
            world.TryToSearch(moongatePosition, out bool bWasSuccessful);
            Debug.Assert(bWasSuccessful);

            world.TryToGetAThing(moongatePosition, out bWasSuccessful, out InventoryItem item);
            Debug.Assert(bWasSuccessful);
            Debug.Assert(item != null);
            Debug.Assert(item.GetType() == typeof(Moonstone));

            string useStr = world.TryToUseAnInventoryItem(item, out bWasSuccessful);
            Debug.Assert(bWasSuccessful);
        }
        
            
        
        [Test]
        public void Test_SearchForMoonstoneAndGet()
        {
            World world = new World(SaveDirectory);

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
        
        [Test]
        public void Test_MoongateHunting()
        {
            World world = new World(SaveDirectory);

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);

            world.State.TheVirtualMap.CurrentPosition = new Point2D(167, 22);
            bool bOnMoongate = world.IsAvatarOnActiveMoongate();
            world.State.TheTimeOfDay.Hour = 23;
            world.State.TheTimeOfDay.Day=1;
            for (int i = 1; i <= 28; i++)
            {
                world.State.TheTimeOfDay.Day=(byte)i;
                world.State.TheTimeOfDay.Hour = 23;
                Point3D p3d = world.GetMoongateTeleportLocation();
                world.State.TheTimeOfDay.Hour = 4;
                p3d = world.GetMoongateTeleportLocation();
            }
        }

        [Test]
        public void Test_TestCorrectMoons()
        {
            World world = new World(SaveDirectory);
            
            MoonPhaseReferences moonPhaseReferences = new MoonPhaseReferences(world.DataOvlRef);
            
            TimeOfDay tod = new TimeOfDay(world.State.GetDataChunk(GameState.DataChunkName.CURRENT_YEAR),
                world.State.GetDataChunk(GameState.DataChunkName.CURRENT_MONTH),
                world.State.GetDataChunk(GameState.DataChunkName.CURRENT_DAY),
                world.State.GetDataChunk(GameState.DataChunkName.CURRENT_HOUR),
                world.State.GetDataChunk(GameState.DataChunkName.CURRENT_MINUTE)); 

            // tod.Day = 2;
            // tod.Hour = 4;
            tod.Day = 25;
            tod.Hour = 4;

            MoonPhaseReferences.MoonPhases trammelPhase =
                moonPhaseReferences.GetMoonPhasesByTimeOfDay(tod, MoonPhaseReferences.MoonsAndSun.Trammel);
            MoonPhaseReferences.MoonPhases feluccaPhase =
                moonPhaseReferences.GetMoonPhasesByTimeOfDay(tod, MoonPhaseReferences.MoonsAndSun.Felucca);
            Debug.Assert(trammelPhase == MoonPhaseReferences.MoonPhases.GibbousWaning);
            Debug.Assert(feluccaPhase == MoonPhaseReferences.MoonPhases.LastQuarter);
        }
        
        [Test]
        public void Test_MoonPhaseReference()
        {
            World world = new World(SaveDirectory);
            
            MoonPhaseReferences moonPhaseReferences = new MoonPhaseReferences(world.DataOvlRef);

            for (byte nDay = 1; nDay <= 28; nDay++)
            {
                for (byte nHour = 0; nHour < 24; nHour++)
                {
                    TimeOfDay tod = new TimeOfDay(world.State.GetDataChunk(GameState.DataChunkName.CURRENT_YEAR),
                        world.State.GetDataChunk(GameState.DataChunkName.CURRENT_MONTH),
                        world.State.GetDataChunk(GameState.DataChunkName.CURRENT_DAY),
                        world.State.GetDataChunk(GameState.DataChunkName.CURRENT_HOUR),
                        world.State.GetDataChunk(GameState.DataChunkName.CURRENT_MINUTE));

                    tod.Day = nDay;
                    tod.Hour = nHour;

                    MoonPhaseReferences.MoonPhases moonPhase = moonPhaseReferences.GetMoonGateMoonPhase(tod);

                    float fMoonAngle = moonPhaseReferences.GetMoonAngle(tod);
                    Assert.True(fMoonAngle >= 0 && fMoonAngle < 360);
                }
            }
        }

        [Test]
        public void Test_TalkToSmith()
        {
            World world = new World(SaveDirectory);
            List<NonPlayerCharacterReference> iolosHutRefs =
                world.NpcRef.GetNonPlayerCharactersByLocation(SmallMapReferences.SingleMapReference.Location.Iolos_Hut);

            NonPlayerCharacterReference smithTheHorseRef = iolosHutRefs[4];

            world.CreateConversationAndBegin(smithTheHorseRef,  new Conversation.EnqueuedScriptItem(OnUpdateOfEnqueuedScriptItem));
        }

        private void OnUpdateOfEnqueuedScriptItem(Conversation conversation)
        {
            
        }

        [Test]
        public void Test_KlimbMountain()
        {
            World world = new World(SaveDirectory);

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
            world.State.TheVirtualMap.CurrentPosition = new Point2D(166,21);
            world.TryToKlimb(out World.KlimbResult klimbResult);
            Debug.Assert(klimbResult == World.KlimbResult.RequiresDirection);
        }
        
        [Test]
        public void Test_MoveALittle()
        {
            World world = new World(SaveDirectory);

            world.State.TheVirtualMap.LoadLargeMap(LargeMap.Maps.Overworld);
            world.State.TheVirtualMap.CurrentPosition = new Point2D(166,21);

            world.TryToMove(VirtualMap.Direction.Up, false, false, out World.TryToMoveResult tryToMoveResult);
        }
        
        [Test]
        public void Test_TalkToDelwyn()
        {
            World world = new World(SaveDirectory);
            List<NonPlayerCharacterReference> minocNpcRef =
                world.NpcRef.GetNonPlayerCharactersByLocation(SmallMapReferences.SingleMapReference.Location.Minoc);

            NonPlayerCharacterReference delwynRef = minocNpcRef[9];

            Conversation convo = world.CreateConversationAndBegin(delwynRef,  new Conversation.EnqueuedScriptItem(OnUpdateOfEnqueuedScriptItemHandleDelwyn));
            convo.BeginConversation();
            convo.AddUserResponse("yes");
            convo.AddUserResponse("yes");
            convo.AddUserResponse("yes");
            convo.AddUserResponse("bye");
            
            //convo.EnqueuedScriptItemCallback -= OnUpdateOfEnqueuedScriptItemHandleDelwyn;
            
            Conversation convo2 = world.CreateConversationAndBegin(delwynRef,  new Conversation.EnqueuedScriptItem(OnUpdateOfEnqueuedScriptItemHandleDelwyn));
            convo2.BeginConversation();
            convo2.AddUserResponse("yes");
            convo2.AddUserResponse("yes");
            convo2.AddUserResponse("yes");
            convo2.AddUserResponse("bye");
        }
        
        private void OnUpdateOfEnqueuedScriptItemHandleDelwyn(Conversation conversation)
        {
                 TalkScript.ScriptItem item = conversation.DequeueFromOutputBuffer();
                 //string userResponse;
                 switch (item.Command)
                 {
                     case TalkScript.TalkCommand.PlainString:
                         Debug.WriteLine(item.Str);
                         break;
                     case TalkScript.TalkCommand.AvatarsName:
                         break;
                     case TalkScript.TalkCommand.EndCoversation:
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
                         //Console.Write(conversation.GetConversationStr(DataOvlReference.ChunkPhrasesConversation.YOUR_INTEREST));
                         break;
                     case TalkScript.TalkCommand.UserInputNotRecognized:
                         break;
                     default:
                         throw new ArgumentOutOfRangeException();
                 }
        }


    }
}