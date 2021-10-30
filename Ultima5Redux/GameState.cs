using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.Inventory;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Ultima5Redux
{
    [DataContract]
    public class GameState
    {
        private readonly MagicReferences _magicReferences;

        /// Legacy save game state
        private readonly ImportedGameState _importedGameState;

        private readonly Map.Maps _initialMap;

        // all initial loaded state information
        private readonly SmallMapReferences.SingleMapReference.Location _location;
        private readonly int _nInitialFloor;
        private readonly int _nInitialX;
        private readonly int _nInitialY;

        /// 2D array of flag indicating if an NPC is dead [MasterMap][npcRef#]
        private readonly bool[][] _npcIsDeadArray;

        /// 2D array of flag indicating if an NPC is met [MasterMap][npcRef#]
        private readonly bool[][] _npcIsMetArray;

        /// A random number generator - capable of seeding in future
        private readonly Random _ran = new Random();

        /// <summary>
        ///     Construct the GameState from a legacy save file
        /// </summary>
        /// <param name="u5Directory">Directory of the game State files</param>
        /// <param name="dataOvlRef"></param>
        /// <param name="inventoryReferences"></param>
        /// <param name="magicReferences"></param>
        public GameState(string u5Directory, DataOvlReference dataOvlRef, InventoryReferences inventoryReferences, MagicReferences magicReferences)
        {
            _magicReferences = magicReferences;
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
            // Food = _importedGameState.Food;
            // Gold = _importedGameState.Gold;
            // Keys = _importedGameState.Keys;
            // Gems = _importedGameState.Gems;
            // Torches = _importedGameState.Torches;
            // SkullKeys = _importedGameState.SkullKeys;
            // HasGrapple = _importedGameState.HasGrapple;
            TurnsToExtinguish = _importedGameState.TorchTurnsLeft;
            ActivePlayerNumber = _importedGameState.ActivePlayerNumber;

            // Initialize the table to determine if an NPC is dead
            _npcIsDeadArray = _importedGameState.NPCIsDeadArray;

            // these will map directly to the towns and the NPC dialog #
            _npcIsMetArray = _importedGameState.NPCIsMetArray;

            TheMoongates = _importedGameState.TheMoongates;

            TheTimeOfDay = _importedGameState.TheTimeOfDay;

            // import the players inventory
            PlayerInventory = new Inventory(_importedGameState.GameStateByteArray, dataOvlRef,
                new MoonPhaseReferences(dataOvlRef), TheMoongates, this, inventoryReferences, _magicReferences,
                _importedGameState);

            Serialize();
        }

        public void Serialize()
        {
            string derp = JsonConvert.SerializeObject(this);
        }

        internal DataChunk CharacterAnimationStatesDataChunk => _importedGameState.CharacterAnimationStatesDataChunk;
        internal DataChunk CharacterStatesDataChunk => _importedGameState.CharacterStatesDataChunk;
        internal DataChunk NonPlayerCharacterKeySprites => _importedGameState.NonPlayerCharacterKeySprites;

        // DataChunk accessors, not ideal - but only available within the library
        internal DataChunk NonPlayerCharacterMovementLists => _importedGameState.NonPlayerCharacterMovementLists;
        internal DataChunk NonPlayerCharacterMovementOffsets => _importedGameState.NonPlayerCharacterMovementOffsets;
        internal DataChunk OverworldOverlayDataChunks => _importedGameState.OverworldOverlayDataChunks;
        internal DataChunk UnderworldOverlayDataChunks => _importedGameState.UnderworldOverlayDataChunks;

        /// <summary>
        ///     Does the Avatar have a torch lit?
        /// </summary>
        public bool IsTorchLit => TurnsToExtinguish > 0;

        [DataMember] public Point2D.Direction WindDirection { get; set; } = Point2D.Direction.None;

        /// <summary>
        ///     What is the index of the currently active player?
        /// </summary>
        [DataMember] public int ActivePlayerNumber { get; set; }



        /// <summary>
        ///     How many turns until the Avatar's torch is extinguished
        /// </summary>
        [DataMember] public int TurnsToExtinguish { get; set; }

        /// <summary>
        ///     Players current inventory
        /// </summary>
        [DataMember] public Inventory PlayerInventory { get; }

        /// <summary>
        ///     Location and state of all moongates and moonstones
        /// </summary>
        public Moongates TheMoongates { get; }

        /// <summary>
        ///     All player character records
        /// </summary>
        public PlayerCharacterRecords CharacterRecords { get; }

        /// <summary>
        ///     The name of the Avatar
        /// </summary>
        public string AvatarsName => CharacterRecords.Records[PlayerCharacterRecords.AVATAR_RECORD].Name;

        /// <summary>
        ///     The current time of day
        /// </summary>
        public TimeOfDay TheTimeOfDay { get; }

        /// <summary>
        ///     Users Karma
        /// </summary>
        public ushort Karma { get; set; }

        /// <summary>
        ///     The virtual map which includes the static map plus all things overlaid on it including NPCs
        /// </summary>
        public VirtualMap TheVirtualMap { get; private set; }

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
        ///     Is NPC alive?
        /// </summary>
        /// <param name="npcRef">NPC object</param>
        /// <returns>true if NPC is alive</returns>
        public bool NpcIsAlive(NonPlayerCharacterReference npcRef)
        {
            // the array isDead because LB stores 0=alive, 1=dead
            // I think it's easier to evaluate if they are alive
            return _npcIsDeadArray[npcRef.MapLocationId][npcRef.DialogIndex] == false;
        }

        public void SetNpcIsDead(NonPlayerCharacterReference npcRef, bool bIsDead)
        {
            _npcIsDeadArray[npcRef.MapLocationId][npcRef.DialogIndex] = bIsDead;
        }


        /// <summary>
        ///     Has the NPC met the avatar yet?
        /// </summary>
        /// <param name="npcRef"></param>
        /// <returns></returns>
        public bool NpcHasMetAvatar(NonPlayerCharacterReference npcRef)
        {
            return _npcIsMetArray[npcRef.MapLocationId][npcRef.DialogIndex];
        }

        /// <summary>
        ///     Set a flag to determine if Avatar has met an NPC
        /// </summary>
        /// <param name="npcRef"></param>
        /// <param name="bHasMet"></param>
        public void SetNpcHasMetAvatar(NonPlayerCharacterReference npcRef, bool bHasMet)
        {
            _npcIsMetArray[npcRef.MapLocationId][npcRef.DialogIndex] = bHasMet;
        }

        /// <summary>
        ///     Initializes (one time) the virtual map component
        ///     Must be initialized pretty much after everything else has been loaded into memory
        /// </summary>
        /// <param name="smallMapReferences"></param>
        /// <param name="smallMaps"></param>
        /// <param name="overworldMap"></param>
        /// <param name="underworldMap"></param>
        /// <param name="tileReferences"></param>
        /// <param name="npcRefs"></param>
        /// <param name="inventoryReferences"></param>
        /// <param name="dataOvlReference"></param>
        /// <param name="bUseExtendedSprites"></param>
        /// <param name="enemyReferences"></param>
        /// <param name="combatMapReferences"></param>
        /// <param name="tileOverrides"></param>
        internal void InitializeVirtualMap(SmallMapReferences smallMapReferences, SmallMaps smallMaps,
            LargeMap overworldMap, LargeMap underworldMap, TileReferences tileReferences,
            NonPlayerCharacterReferences npcRefs, InventoryReferences inventoryReferences,
            DataOvlReference dataOvlReference, bool bUseExtendedSprites,
            EnemyReferences enemyReferences, CombatMapReferences combatMapReferences, TileOverrides tileOverrides)
        {
            SmallMapReferences.SingleMapReference mapRef =
                _location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld
                    ? null
                    : smallMapReferences.GetSingleMapByLocation(_location, _nInitialFloor);

            TheVirtualMap = new VirtualMap(smallMapReferences, smallMaps, overworldMap,
                underworldMap, tileReferences, this, npcRefs, TheTimeOfDay, TheMoongates,
                inventoryReferences, CharacterRecords, _initialMap, mapRef, dataOvlReference, bUseExtendedSprites,
                enemyReferences, PlayerInventory, combatMapReferences, tileOverrides);
            // we have to set the initial xy, not the floor because that is part of the SingleMapReference
            // I should probably just add yet another thing to the constructor
            TheVirtualMap.CurrentPosition.XY = new Point2D(_nInitialX, _nInitialY);
        }
    }
}