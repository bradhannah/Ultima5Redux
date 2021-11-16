using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Ultima5Redux
{
    [DataContract] public class GameState
    {
        /// Legacy save game state
        private readonly ImportedGameState _importedGameState;

        private readonly Map.Maps _initialMap;

        // all initial loaded state information
        private readonly SmallMapReferences.SingleMapReference.Location _location;
        private readonly int _nInitialFloor;
        private readonly int _nInitialX;
        private readonly int _nInitialY;

        /// A random number generator - capable of seeding in future
        private readonly Random _ran = new Random();

        /// <summary>
        ///     Construct the GameState from a legacy save file
        /// </summary>
        /// <param name="u5Directory">Directory of the game State files</param>
        public GameState(string u5Directory)
        {
            // imports the legacy save game file data 
            _importedGameState = new ImportedGameState(u5Directory);

            // one time copy of all imported state information
            CharacterRecords = _importedGameState.CharacterRecords;
            _location = _importedGameState.Location;
            _nInitialFloor = _importedGameState.Floor;
            _initialMap = _importedGameState.InitialMap;
            _nInitialX = _importedGameState.X;
            _nInitialY = _importedGameState.Y;

            Karma = _importedGameState.Karma;
            TurnsToExtinguish = _importedGameState.TorchTurnsLeft;
            ActivePlayerNumber = _importedGameState.ActivePlayerNumber;

            TheMoongates = _importedGameState.TheMoongates;

            TheTimeOfDay = _importedGameState.TheTimeOfDay;

            TheNonPlayerCharacterStates = _importedGameState.TheNonPlayerCharacterStates;

            // import the players inventory
            PlayerInventory = new Inventory(_importedGameState.GameStateByteArray, TheMoongates, this);
        }


        public string Serialize()
        {
            string derp = JsonConvert.SerializeObject(this);
            return derp;
        }

        /// <summary>
        ///     Does the Avatar have a torch lit?
        /// </summary>
        public bool IsTorchLit => TurnsToExtinguish > 0;

        /// <summary>
        ///     Users Karma
        /// </summary>
        [DataMember]
        public ushort Karma { get; set; }

        [DataMember] public Point2D.Direction WindDirection { get; set; } = Point2D.Direction.None;

        /// <summary>
        ///     What is the index of the currently active player?
        /// </summary>
        [DataMember]
        public int ActivePlayerNumber { get; set; }


        /// <summary>
        ///     How many turns until the Avatar's torch is extinguished
        /// </summary>
        [DataMember]
        public int TurnsToExtinguish { get; set; }

        /// <summary>
        ///     Players current inventory
        /// </summary>
        [DataMember]
        public Inventory PlayerInventory { get; }

        /// <summary>
        ///     Location and state of all moongates and moonstones
        /// </summary>
        [DataMember]
        public Moongates TheMoongates { get; }

        /// <summary>
        ///     All player character records
        /// </summary>
        [DataMember]
        public PlayerCharacterRecords CharacterRecords { get; }

        /// <summary>
        ///     The name of the Avatar
        /// </summary>
        [IgnoreDataMember]
        public string AvatarsName => CharacterRecords.Records[PlayerCharacterRecords.AVATAR_RECORD].Name;

        /// <summary>
        ///     The current time of day
        /// </summary>
        [DataMember]
        public TimeOfDay TheTimeOfDay { get; }

        /// <summary>
        ///     The virtual map which includes the static map plus all things overlaid on it including NPCs
        /// </summary>
        [DataMember]
        public VirtualMap TheVirtualMap { get; private set; }

        /// <summary>
        /// NPC states such as if they are dead or have met the avatar
        /// </summary>
        [DataMember] public NonPlayerCharacterStates TheNonPlayerCharacterStates;

        /// <summary>
        ///     Take fall damage from klimbing mountains
        /// </summary>
        public void GrapplingFall()
        {
            // called when falling from a Klimb on a mountain
        }

        /// <summary>
        ///     Using the random number generator, provides 1 in howMany odds of returning true
        /// </summary>
        /// <param name="howMany">1 in howMany odds of returning true</param>
        /// <returns>true if odds are beat</returns>
        public bool OneInXOdds(int howMany)
        {
            // if ran%howMany is zero then we beat the odds
            int nextRan = _ran.Next();
            return nextRan % howMany == 0;
        }


        /// <summary>
        ///     Initializes (one time) the virtual map component
        ///     Must be initialized pretty much after everything else has been loaded into memory
        /// </summary>
        /// <param name="smallMapReferences"></param>
        /// <param name="smallMaps"></param>
        /// <param name="overworldMap"></param>
        /// <param name="underworldMap"></param>
        /// <param name="bUseExtendedSprites"></param>
        internal void InitializeVirtualMap(SmallMapReferences smallMapReferences, SmallMaps smallMaps,
            LargeMap overworldMap, LargeMap underworldMap, bool bUseExtendedSprites)
        {
            SmallMapReferences.SingleMapReference mapRef =
                _location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld
                    ? null
                    : smallMapReferences.GetSingleMapByLocation(_location, _nInitialFloor);

            TheVirtualMap = new VirtualMap(smallMapReferences, smallMaps, overworldMap,
                underworldMap, this, TheTimeOfDay, TheMoongates, CharacterRecords, _initialMap, mapRef, 
                bUseExtendedSprites, PlayerInventory, _importedGameState, TheNonPlayerCharacterStates);
            // we have to set the initial xy, not the floor because that is part of the SingleMapReference
            // I should probably just add yet another thing to the constructor
            TheVirtualMap.CurrentPosition.XY = new Point2D(_nInitialX, _nInitialY);
        }
    }
}