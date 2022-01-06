using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Ultima5Redux
{
    [DataContract] public class GameState
    {
        [DataMember(Name = "InitialMap")] private readonly Map.Maps _initialMap;

        public GameSummary CreateGameSummary(string saveGamePath)
        {
            GameSummary gameSummary = new()
            {
                CharacterRecords = CharacterRecords,
                TheTimeOfDay = TheTimeOfDay,
                TheExtraSaveData = new GameSummary.ExtraSaveData
                {
                    CurrentMapPosition = TheVirtualMap.CurrentPosition,
                    FriendlyLocationName = FriendlyLocationName,
                    SavedDirectory = saveGamePath,
                    LastWrite = Directory.GetLastWriteTime(saveGamePath)
                }
            };
            return gameSummary;
        }

        /// <summary>
        ///     All player character records
        /// </summary>
        [DataMember]
        public PlayerCharacterRecords CharacterRecords { get; protected set; }

        /// <summary>
        ///     The current time of day
        /// </summary>
        [DataMember]
        public TimeOfDay TheTimeOfDay { get; protected set; }

        /// <summary>
        ///     What is the index of the currently active player?
        /// </summary>
        [DataMember]
        public int ActivePlayerNumber { get; set; }

        /// <summary>
        ///     Users Karma
        /// </summary>
        [DataMember]
        public ushort Karma { get; set; }

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
        ///     The virtual map which includes the static map plus all things overlaid on it including NPCs
        /// </summary>
        [DataMember]
        public VirtualMap TheVirtualMap { get; private set; }

        /// <summary>
        ///     How many turns until the Avatar's torch is extinguished
        /// </summary>
        [DataMember]
        public int TurnsToExtinguish { get; set; }

        [DataMember] public Point2D.Direction WindDirection { get; set; } = Point2D.Direction.None;

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
                if (!TheVirtualMap.IsLargeMap)
                {
                    if (TheVirtualMap.CurrentSingleMapReference == null)
                        throw new Ultima5ReduxException("No single map is set in virtual map");

                    return GameReferences.SmallMapRef.GetLocationName(TheVirtualMap.CurrentSingleMapReference
                        .MapLocation);
                }

                if (TheVirtualMap.CurrentSingleMapReference == null)
                    throw new Ultima5ReduxException("No single map is set in virtual map");

                return TheVirtualMap.CurrentSingleMapReference.Floor == -1 ? "Underworld" : "Overworld";
            }
        }

        /// Legacy save game state
        [IgnoreDataMember] internal readonly ImportedGameState ImportedGameState;

        /// <summary>
        ///     Does the Avatar have a torch lit?
        /// </summary>
        [IgnoreDataMember]
        public bool IsTorchLit => TurnsToExtinguish > 0;

        [JsonConstructor] private GameState()
        {
            GameStateReference.SetState(this);
        }

        /// <summary>
        ///     Construct the GameState from a legacy save file
        /// </summary>
        /// <param name="saveDirectory">Directory of the game State files</param>
        /// <param name="smallMaps"></param>
        /// <param name="overworldMap"></param>
        /// <param name="underworldMap"></param>
        /// <param name="bUseExtendedSprites"></param>
        public GameState(string saveDirectory, SmallMaps smallMaps, LargeMap overworldMap, LargeMap underworldMap,
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

            TheMoongates = ImportedGameState.TheMoongates;

            TheTimeOfDay = ImportedGameState.TheTimeOfDay;

            TheNonPlayerCharacterStates = ImportedGameState.TheNonPlayerCharacterStates;

            // import the players inventory
            PlayerInventory = new Inventory(ImportedGameState);
            InitializeVirtualMap(smallMaps, overworldMap, underworldMap, bUseExtendedSprites,
                ImportedGameState.Location, ImportedGameState.X, ImportedGameState.Y, ImportedGameState.Floor);
        }

        public static GameState Deserialize(string stateJson)
        {
            return JsonConvert.DeserializeObject<GameState>(stateJson);
        }

        public static GameState DeserializeFromFile(string filePathAndName)
        {
            FileStream fs = new(filePathAndName, FileMode.Open, FileAccess.Read, FileShare.Read);
            StreamReader sr = new(fs);
            JsonReader js = new JsonTextReader(sr);
            JsonSerializer jser = new();

            GameState state = jser.Deserialize<GameState>(js);

            Debug.Assert(state != null, nameof(state) + " != null");

            fs.Close();
            return state;
        }

        /// <summary>
        ///     Initializes (one time) the virtual map component
        ///     Must be initialized pretty much after everything else has been loaded into memory
        /// </summary>
        /// <param name="smallMaps"></param>
        /// <param name="overworldMap"></param>
        /// <param name="underworldMap"></param>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="location"></param>
        /// <param name="nInitialX"></param>
        /// <param name="nInitialY"></param>
        /// <param name="nInitialFloor"></param>
        private void InitializeVirtualMap(SmallMaps smallMaps, LargeMap overworldMap, LargeMap underworldMap,
            bool bUseExtendedSprites, SmallMapReferences.SingleMapReference.Location location, int nInitialX,
            int nInitialY, int nInitialFloor)
        {
            SmallMapReferences.SingleMapReference mapRef =
                location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld
                    ? null
                    : GameReferences.SmallMapRef.GetSingleMapByLocation(location, nInitialFloor);

            TheVirtualMap = new VirtualMap(smallMaps, overworldMap, underworldMap, _initialMap, mapRef,
                bUseExtendedSprites, ImportedGameState);
            // we have to set the initial xy, not the floor because that is part of the SingleMapReference
            // I should probably just add yet another thing to the constructor
            TheVirtualMap.CurrentPosition.XY = new Point2D(nInitialX, nInitialY);
        }

        /// <summary>
        ///     Take fall damage from klimbing mountains
        /// </summary>
        public void GrapplingFall()
        {
            // called when falling from a Klimb on a mountain
        }

        public string Serialize()
        {
            string stateJson = JsonConvert.SerializeObject(this, Formatting.Indented);
            return stateJson;
        }

        private void MoveFileAsBackup(string currentSaveGamePathAndFile)
        {
            string currentSaveGamePathAndFileBackup = currentSaveGamePathAndFile + ".bak";

            if (!File.Exists(currentSaveGamePathAndFile)) return;

            if (File.Exists(currentSaveGamePathAndFileBackup))
            {
                File.Delete(currentSaveGamePathAndFileBackup);
            }

            File.Move(currentSaveGamePathAndFile, currentSaveGamePathAndFileBackup);
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
            } catch (Exception e)
            {
                errorStr = e.Message;
                return false;
            }

            return true;
        }
    }
}