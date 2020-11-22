using System;
using Ultima5Redux.Data;
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
    public class GameState
    {
        ///     2D array of flag indicating if an NPC is dead [MasterMap][npc#]
        private readonly bool[][] _npcIsDeadArray;
        ///     2D array of flag indicating if an NPC is met [MasterMap][npc#]
        private readonly bool[][] _npcIsMetArray;
        ///     A random number generator - capable of seeding in future
        private readonly Random _ran = new Random();
        /// Legacy save game state
        private readonly ImportedGameState _importedGameState;

        // all initial loaded state information
        private readonly SmallMapReferences.SingleMapReference.Location _location;
        private readonly int _nInitialFloor;
        private readonly int _nInitialX;
        private readonly int _nInitialY;
        private readonly LargeMap.Maps _initialMap;
        
        /// <summary>
        /// Amount of food Avatar has 
        /// </summary>
        public ushort Food { get; set; }
        /// <summary>
        /// Amount of gold Avatar has
        /// </summary>
        public ushort Gold { get; set; }
        /// <summary>
        ///  Number of keys the Avatar has
        /// </summary>
        public int Keys { get; set; }
        /// <summary>
        ///  Number of gems the Avatar has
        /// </summary>
        public int Gems { get; set; }
        /// <summary>
        /// Number of torches the Avatar has
        /// </summary>
        public int Torches { get; set; }
        /// <summary>
        /// Number of skull keys the Avatar has
        /// </summary>
        public int SkullKeys { get; set; }
        /// <summary>
        /// Does the Avatar have the Grappling Hook
        /// </summary>
        public bool HasGrapple { get; set; }
        /// <summary>
        /// How many turns until the Avatar's torch is extinguished
        /// </summary>
        public int TurnsToExtinguish { get; set; }
        /// <summary>
        /// What is the index of the currently active player?
        /// </summary>
        public int ActivePlayerNumber { get; set; }
        /// <summary>
        /// Does the Avatar have a torch lit?
        /// </summary>
        public bool IsTorchLit => TurnsToExtinguish > 0;
        /// <summary>
        ///     All player character records
        /// </summary>
        public PlayerCharacterRecords CharacterRecords { get; }

        /// <summary>
        ///     The virtual map which includes the static map plus all things overlaid on it including NPCs
        /// </summary>
        public VirtualMap TheVirtualMap { get; private set; }

        /// <summary>
        ///     The current time of day
        /// </summary>
        public TimeOfDay TheTimeOfDay { get; }

        /// <summary>
        /// Location and state of all moongates and moonstones
        /// </summary>
        public Moongates TheMoongates { get; }

        /// <summary>
        ///     Players current inventory
        /// </summary>
        public Inventory PlayerInventory { get; }

        /// <summary>
        ///     Users Karma
        /// </summary>
        public ushort Karma { get; set; }

        /// <summary>
        ///     The name of the Avatar
        /// </summary>
        public string AvatarsName => CharacterRecords.Records[PlayerCharacterRecords.AVATAR_RECORD].Name;        
        
        // DataChunk accessors, not ideal - but only available within the library
        internal DataChunk NonPlayerCharacterMovementLists => _importedGameState.NonPlayerCharacterMovementLists;
        internal DataChunk NonPlayerCharacterMovementOffsets => _importedGameState.NonPlayerCharacterMovementOffsets;
        internal DataChunk OverworldOverlayDataChunks => _importedGameState.OverworldOverlayDataChunks;
        internal DataChunk UnderworldOverlayDataChunks => _importedGameState.UnderworldOverlayDataChunks;
        internal DataChunk CharacterAnimationStatesDataChunk => _importedGameState.CharacterAnimationStatesDataChunk;
        internal DataChunk CharacterStatesDataChunk => _importedGameState.CharacterStatesDataChunk;
        internal DataChunk NonPlayerCharacterKeySprites => _importedGameState.NonPlayerCharacterKeySprites;
        
        /// <summary>
        ///     Construct the GameState from a legacy save file
        /// </summary>
        /// <param name="u5Directory">Directory of the game State files</param>
        /// <param name="dataOvlRef"></param>
        public GameState(string u5Directory, DataOvlReference dataOvlRef)
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
            Food = _importedGameState.Food;
            Gold = _importedGameState.Gold;
            Keys = _importedGameState.Keys;
            Gems = _importedGameState.Gems;
            Torches = _importedGameState.Torches;
            SkullKeys = _importedGameState.SkullKeys;
            HasGrapple = _importedGameState.HasGrapple;
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
                new MoonPhaseReferences(dataOvlRef), TheMoongates, this);
        }

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
        /// <param name="npc">NPC object</param>
        /// <returns>true if NPC is alive</returns>
        public bool NpcIsAlive(NonPlayerCharacterReference npc)
        {
            // the array isDead because LB stores 0=alive, 1=dead
            // I think it's easier to evaluate if they are alive
            return _npcIsDeadArray[npc.MapLocationId][npc.DialogIndex] == false;
        }

        /// <summary>
        ///     Sets the flag to indicate the NPC is met
        /// </summary>
        /// <param name="npc"></param>
        public void SetMetNpc(NonPlayerCharacterReference npc)
        {
            _npcIsMetArray[npc.MapLocationId][npc.DialogIndex] = true;
        }


        /// <summary>
        ///     Has the NPC met the avatar yet?
        /// </summary>
        /// <param name="npc"></param>
        /// <returns></returns>
        public bool NpcHasMetAvatar(NonPlayerCharacterReference npc)
        {
            return _npcIsMetArray[npc.MapLocationId][npc.DialogIndex];
        }

        /// <summary>
        ///     Set a flag to determine if Avatar has met an NPC
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="bHasMet"></param>
        public void SetNpcHasMetAvatar(NonPlayerCharacterReference npc, bool bHasMet)
        {
            _npcIsMetArray[npc.MapLocationId][npc.DialogIndex] = bHasMet;
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
        internal void InitializeVirtualMap(SmallMapReferences smallMapReferences, SmallMaps smallMaps,
            LargeMap overworldMap, LargeMap underworldMap, TileReferences tileReferences, 
            NonPlayerCharacterReferences npcRefs, InventoryReferences inventoryReferences,
            DataOvlReference dataOvlReference)
        {
            SmallMapReferences.SingleMapReference mapRef =
                _location == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld
                    ? null : smallMapReferences.GetSingleMapByLocation(_location, _nInitialFloor);

            TheVirtualMap = new VirtualMap(smallMapReferences, smallMaps, overworldMap,
                underworldMap, tileReferences, this, npcRefs, TheTimeOfDay, TheMoongates, 
                inventoryReferences, CharacterRecords, _initialMap, mapRef, dataOvlReference);
            // we have to set the initial xy, not the floor because that is part of the SingleMapReference
            // I should probably just add yet another thing to the constructor
            TheVirtualMap.CurrentPosition.XY = new Point2D(_nInitialX, _nInitialY);
        }

    }
}