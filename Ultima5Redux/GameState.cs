using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.TurnResults;
using Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.State;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Ultima5Redux
{
    [DataContract]
    public class GameState
    {
        [DataMember(Name = "InitialMap")] private readonly Map.Maps _initialMap;

        /// <summary>
        ///     What is the index of the currently active player?
        /// </summary>
        [DataMember]
        public int ActivePlayerNumber { get; set; }

        /// <summary>
        ///     All player character records
        /// </summary>
        [DataMember]
        public PlayerCharacterRecords CharacterRecords { get; protected set; }

        /// <summary>
        ///     Users Karma
        /// </summary>
        [DataMember]
        public ushort Karma { get; private set; }


        /// <summary>
        ///     Players current inventory
        /// </summary>
        [DataMember]
        public Inventory PlayerInventory { get; private set; }

        /// <summary>
        ///     Location and state of all moongates and moonstones
        /// </summary>
        [DataMember]
        public Moongates TheMoongates { get; private set; }

        /// <summary>
        ///     NPC states such as if they are dead or have met the avatar
        /// </summary>
        [DataMember]
        public NonPlayerCharacterStates TheNonPlayerCharacterStates { get; private set; }

        /// <summary>
        ///     The current time of day
        /// </summary>
        [DataMember]
        public TimeOfDay TheTimeOfDay { get; protected set; }

        /// <summary>
        ///     The virtual map which includes the static map plus all things overlaid on it including NPCs
        /// </summary>
        [DataMember]
        public VirtualMap TheVirtualMap { get; private set; }

        [DataMember] public int TurnsSinceStart { get; set; }

        /// <summary>
        ///     How many turns until the Avatar's torch is extinguished
        /// </summary>
        [DataMember]
        public int TurnsToExtinguish { get; set; }

        [DataMember] public Point2D.Direction WindDirection { get; set; } = Point2D.Direction.None;

        [DataMember] public ShrineStates TheShrineStates { get; private set; }
        
        /// Legacy save game state
        [IgnoreDataMember] internal readonly ImportedGameState ImportedGameState;

        /// <summary>
        ///     The name of the Avatar
        /// </summary>
        [IgnoreDataMember]
        public string AvatarsName =>
            CharacterRecords.Records[PlayerCharacterRecords.AVATAR_RECORD].Name;

        
        [IgnoreDataMember]
        public string FriendlyLocationName
        {
            get
            {
                if (TheVirtualMap.CurrentMap is not LargeMap)
                {
                    if (TheVirtualMap.CurrentMap.CurrentSingleMapReference == null)
                        throw new Ultima5ReduxException("No single map is set in virtual map");

                    return GameReferences.Instance.SmallMapRef.GetLocationName(TheVirtualMap.CurrentMap
                        .CurrentSingleMapReference
                        .MapLocation);
                }

                if (TheVirtualMap.CurrentMap.CurrentSingleMapReference == null)
                    throw new Ultima5ReduxException("No single map is set in virtual map");

                return TheVirtualMap.CurrentMap.CurrentSingleMapReference.Floor == -1 ? "Underworld" : "Overworld";
            }
        }

        /// <summary>
        ///     Does the Avatar have a torch lit?
        /// </summary>
        [IgnoreDataMember]
        public bool IsTorchLit => TurnsToExtinguish > 0;

        public GameOverrides TheGameOverrides { get; set; } = new();

        [JsonConstructor]
        private GameState()
        {
            GameStateReference.SetState(this);
        }

        /// <summary>
        ///     Construct the GameState from a legacy save file
        /// </summary>
        /// <param name="saveDirectory">Directory of the game State files</param>
        /// <param name="bUseExtendedSprites"></param>
        public GameState(string saveDirectory,
            //LargeMap overworldMap, LargeMap underworldMap,
            bool bUseExtendedSprites)
        {
            GameStateReference.SetState(this);

            bool loadedFromInitial = saveDirectory == "";
            // imports the legacy save game file data 
            ImportedGameState = loadedFromInitial ? new ImportedGameState() : new ImportedGameState(saveDirectory);

            // one time copy of all imported state information
            CharacterRecords = ImportedGameState.CharacterRecords;
            _initialMap = ImportedGameState.InitialMap;

            Karma = ImportedGameState.Karma;
            TurnsToExtinguish = ImportedGameState.TorchTurnsLeft;
            ActivePlayerNumber = ImportedGameState.ActivePlayerNumber;
            TurnsSinceStart = ImportedGameState.TurnsSinceStart;

            TheMoongates = ImportedGameState.TheMoongates;

            TheTimeOfDay = ImportedGameState.TheTimeOfDay;

            TheShrineStates = ImportedGameState.TheShrineStates;

            TheNonPlayerCharacterStates = ImportedGameState.TheNonPlayerCharacterStates;

            //TheSearchItems = ImportedGameState.TheSearchItems;

            // import the players inventory
            PlayerInventory = new Inventory(ImportedGameState);

            InitializeVirtualMap(
                //overworldMap, underworldMap,
                bUseExtendedSprites,
                ImportedGameState.Location, ImportedGameState.X, ImportedGameState.Y, ImportedGameState.Floor,
                ImportedGameState.TheSearchItems);
        }

        [OnDeserialized]
        private void PostDeserialized(StreamingContext context)
        {
            // we do this because we need to load the NPC information from the main state - not a copy in  
            // the JSON. If we don't do this then it creates two sets of NPCs
            if (TheVirtualMap.CurrentMap is SmallMap smallMap)
                smallMap.ReloadNpcData(TheVirtualMap.CurrentMap.CurrentSingleMapReference.MapLocation);
        }

        private static void MoveFileAsBackup(string currentSaveGamePathAndFile)
        {
            string currentSaveGamePathAndFileBackup = currentSaveGamePathAndFile + ".bak";

            if (!File.Exists(currentSaveGamePathAndFile)) return;

            if (File.Exists(currentSaveGamePathAndFileBackup))
            {
                File.Delete(currentSaveGamePathAndFileBackup);
            }

            File.Move(currentSaveGamePathAndFile, currentSaveGamePathAndFileBackup);
        }

        /// <summary>
        ///     Initializes (one time) the virtual map component
        ///     Must be initialized pretty much after everything else has been loaded into memory
        ///     This is ONLY for loading from a legacy game state - all future JSON loads will
        ///     use deserialization and this method will not be called
        /// </summary>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="location"></param>
        /// <param name="nInitialX"></param>
        /// <param name="nInitialY"></param>
        /// <param name="nInitialFloor"></param>
        /// <param name="searchItems"></param>
        private void InitializeVirtualMap(
            bool bUseExtendedSprites, SmallMapReferences.SingleMapReference.Location location, int nInitialX,
            int nInitialY, int nInitialFloor, SearchItems searchItems)
        {
            SmallMapReferences.SingleMapReference mapRef =
                location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld
                    ? null
                    : GameReferences.Instance.SmallMapRef.GetSingleMapByLocation(location, nInitialFloor);

            TheVirtualMap = new VirtualMap(
                _initialMap, mapRef,
                bUseExtendedSprites, ImportedGameState, searchItems)
            {
                CurrentMap =
                {
                    CurrentPosition =
                    {
                        // we have to set the initial xy, not the floor because that is part of the SingleMapReference
                        // I should probably just add yet another thing to the constructor
                        XY = new Point2D(nInitialX, nInitialY),
                        Floor = nInitialFloor
                    }
                }
            };
        }


        public static GameState Deserialize(string stateJson) => JsonConvert.DeserializeObject<GameState>(stateJson);

        public static GameState DeserializeFromFile(string filePathAndName)
        {
            FileStream fs = new(filePathAndName, FileMode.Open, FileAccess.Read, FileShare.Read);
            StreamReader sr = new(fs);
            JsonReader js = new JsonTextReader(sr);
            JsonSerializer jser = new();

            var state = jser.Deserialize<GameState>(js);

            Debug.Assert(state != null, nameof(state) + " != null");

            fs.Close();
            return state;
        }

        public void ChangeKarma(int nAdjustBy, TurnResults turnResults)
        {
            turnResults.PushTurnResult(new KarmaChanged(nAdjustBy, Karma));
            Karma = (ushort)Math.Max(0, Karma + nAdjustBy);
            if (Karma > 99) Karma = 99;
        }

        public GameSummary CreateGameSummary(string saveGamePath)
        {
            GameSummary gameSummary = new()
            {
                CharacterRecords = CharacterRecords,
                TheTimeOfDay = TheTimeOfDay,
                TheExtraSaveData = new GameSummary.ExtraSaveData
                {
                    CurrentMapPosition = TheVirtualMap.CurrentMap.CurrentPosition,
                    FriendlyLocationName = FriendlyLocationName,
                    SavedDirectory = saveGamePath,
                    LastWrite = Directory.GetLastWriteTime(saveGamePath)
                }
            };
            return gameSummary;
        }

        /// <summary>
        ///     Take fall damage from klimbing mountains
        /// </summary>
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        public void GrapplingFall()
        {
            // called when falling from a Klimb on a mountain
        }

        public bool SaveGame(out string errorStr, string currentSaveGamePath)
        {
            errorStr = "";

            Directory.CreateDirectory(currentSaveGamePath);

            try
            {
                string currentSaveGamePathAndFile = Path.Combine(currentSaveGamePath, FileConstants.NEW_SAVE_FILE);
                // make sure there is a backup of the save game before saving
                MoveFileAsBackup(currentSaveGamePathAndFile);
                string saveFileJsonStr = Serialize();
                File.WriteAllText(currentSaveGamePathAndFile, saveFileJsonStr);

                string currentSaveGameSummaryPathAndFile =
                    Path.Combine(currentSaveGamePath, FileConstants.NEW_SAVE_SUMMARY_FILE);
                MoveFileAsBackup(currentSaveGameSummaryPathAndFile);

                GameSummary gameSummary = CreateGameSummary(currentSaveGamePath);
                string gameSummaryJsonStr = gameSummary.SerializeGameSummary();
                File.WriteAllText(currentSaveGameSummaryPathAndFile, gameSummaryJsonStr);
            }
            catch (Exception e)
            {
                errorStr = e.Message;
                return false;
            }

            return true;
        }

        public string Serialize()
        {
            string stateJson = JsonConvert.SerializeObject(this, Formatting.Indented);
            return stateJson;
        }
    }
}