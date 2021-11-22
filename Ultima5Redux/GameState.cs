using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Ultima5Redux
{
    [DataContract] public class GameState
    {

        [DataMember(Name = "InitialMap")] private readonly Map.Maps _initialMap;

        /// <summary>
        ///     All player character records
        /// </summary>
        [DataMember] public PlayerCharacterRecords CharacterRecords { get; }

        /// <summary>
        ///     Players current inventory
        /// </summary>
        [DataMember] public Inventory PlayerInventory { get; }

        /// <summary>
        ///     Location and state of all moongates and moonstones
        /// </summary>
        [DataMember] public Moongates TheMoongates { get; }

        /// <summary>
        ///     The current time of day
        /// </summary>
        [DataMember] public TimeOfDay TheTimeOfDay { get; }

        /// <summary>
        ///     What is the index of the currently active player?
        /// </summary>
        [DataMember] public int ActivePlayerNumber { get; set; }

        /// <summary>
        ///     Users Karma
        /// </summary>
        [DataMember] public ushort Karma { get; set; }

        /// <summary>
        ///     NPC states such as if they are dead or have met the avatar
        /// </summary>
        [DataMember]
        public NonPlayerCharacterStates TheNonPlayerCharacterStates;

        /// <summary>
        ///     The virtual map which includes the static map plus all things overlaid on it including NPCs
        /// </summary>
        [DataMember] public VirtualMap TheVirtualMap { get; private set; }

        /// <summary>
        ///     How many turns until the Avatar's torch is extinguished
        /// </summary>
        [DataMember] public int TurnsToExtinguish { get; set; }

        [DataMember] public Point2D.Direction WindDirection { get; set; } = Point2D.Direction.None;

        /// Legacy save game state
        [IgnoreDataMember]
        private readonly ImportedGameState _importedGameState;

        /// <summary>
        ///     The name of the Avatar
        /// </summary>
        [IgnoreDataMember] public string AvatarsName =>
            CharacterRecords.Records[PlayerCharacterRecords.AVATAR_RECORD].Name;

        /// <summary>
        ///     Does the Avatar have a torch lit?
        /// </summary>
        [IgnoreDataMember] public bool IsTorchLit => TurnsToExtinguish > 0;

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
            _importedGameState = loadedFromInitial ? new ImportedGameState() : new ImportedGameState(saveDirectory);

            // one time copy of all imported state information
            CharacterRecords = _importedGameState.CharacterRecords;
            _initialMap = _importedGameState.InitialMap;

            Karma = _importedGameState.Karma;
            TurnsToExtinguish = _importedGameState.TorchTurnsLeft;
            ActivePlayerNumber = _importedGameState.ActivePlayerNumber;

            TheMoongates = _importedGameState.TheMoongates;

            TheTimeOfDay = _importedGameState.TheTimeOfDay;

            TheNonPlayerCharacterStates = _importedGameState.TheNonPlayerCharacterStates;

            // import the players inventory
            PlayerInventory = new Inventory(_importedGameState.GameStateByteArray, TheMoongates, this);
            InitializeVirtualMap(smallMaps, overworldMap, underworldMap, bUseExtendedSprites,
                _importedGameState.Location, _importedGameState.X, _importedGameState.Y, _importedGameState.Floor);
        }

        public static GameState Deserialize(string stateJson)
        {
            return JsonConvert.DeserializeObject<GameState>(stateJson);
        }

        /// <summary>
        ///     Take fall damage from klimbing mountains
        /// </summary>
        public void GrapplingFall()
        {
            // called when falling from a Klimb on a mountain
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
                bUseExtendedSprites, _importedGameState);
            // we have to set the initial xy, not the floor because that is part of the SingleMapReference
            // I should probably just add yet another thing to the constructor
            TheVirtualMap.CurrentPosition.XY = new Point2D(nInitialX, nInitialY);
        }

        public string Serialize()
        {
            string stateJson = JsonConvert.SerializeObject(this);
            return stateJson;
        }
    }
}