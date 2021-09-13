using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Ultima5Redux.Maps
{
    [JsonObject(MemberSerialization.OptIn)] 
    public class DungeonCombatMapReference
    {
        public enum Dungeon {Deceit = 27, Despise = 28, Destard = 29, Wrong = 30, Covetous = 31,
            Shame = 32, Hythloth = 33, Doom = 34} 
        [JsonProperty] public int Index { get; internal set; }
        [JsonProperty] public string Name { get; internal set; }
        [JsonProperty] public Dungeon DungeonLocation  { get; internal set; }
        [JsonProperty] public bool DirEastLeft { get; internal set; }
        [JsonProperty] public bool DirWestRight { get; internal set; }
        [JsonProperty] public bool DirNorthUp { get; internal set; }
        [JsonProperty] public bool DirSouthDown { get; internal set; }
        [JsonProperty] public bool OtherStart { get; internal set; }
        [JsonProperty] public bool LaddersUp { get; internal set; }
        [JsonProperty] public bool LaddersDown { get; internal set; }
        [JsonProperty] public bool HasTriggers { get; internal set; }
        [JsonProperty] public bool HasRegularDoor { get; internal set; }
        [JsonProperty] public bool HasMagicDoor { get; internal set; }
        [JsonProperty] public bool SpecialEnemyComputation { get; internal set; }
        [JsonProperty] public bool IsBroke { get; internal set; }
        [JsonProperty] public string Notes { get; internal set; }

        public string GetAsCSVLine() => $"{Index}, {Name}, {DungeonLocation}, {DirEastLeft}, {DirWestRight}, {DirWestRight}, {DirSouthDown}, {LaddersUp}, {LaddersDown}, {HasTriggers}, {Notes}";

        public static string GetCSVHeader() => "Index, Name, DungeonLocation, DirEastLeft, DirWestRight, DirWestRight, DirSouthDown, LaddersUp, LaddersDown, HasTriggers, Notes";
    }
    
    public class DungeonCombatMapReferences
    {
        private List<DungeonCombatMapReference> _dungeonCombatMapReferences;

        public DungeonCombatMapReferences()
        {
            _dungeonCombatMapReferences = new List<DungeonCombatMapReference>();
            
        }

        public string GetAsCSV()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(DungeonCombatMapReference.GetCSVHeader());
            foreach (DungeonCombatMapReference dungeonCombatMapReference in _dungeonCombatMapReferences)
            {
                sb.Append("\n" + dungeonCombatMapReference.GetAsCSVLine());
            }

            return sb.ToString();
        }

        public void BuildDynamically(CombatMapReferences combatMapReferences, 
            TileReferences tileReferences)
        {
            foreach (SingleCombatMapReference combatMapReference in combatMapReferences
                .GetListOfSingleCombatMapReferences(SingleCombatMapReference.Territory.Dungeon))
            {
                DungeonCombatMapReference dungeonCombatMapReference = new DungeonCombatMapReference
                {
                    Index = combatMapReference.CombatMapNum,
                    Name = "Dungeon-" + combatMapReference.CombatMapNum,
                    // check for valid directions
                    DirSouthDown = combatMapReference.IsEntryDirectionValid(SingleCombatMapReference.EntryDirection.South),
                    DirNorthUp = combatMapReference.IsEntryDirectionValid(SingleCombatMapReference.EntryDirection.North),
                    DirEastLeft = combatMapReference.IsEntryDirectionValid(SingleCombatMapReference.EntryDirection.East),
                    DirWestRight = combatMapReference.IsEntryDirectionValid(SingleCombatMapReference.EntryDirection.West),
                    LaddersUp = combatMapReference.DoesTileReferenceOccurOnMap(tileReferences.GetTileReferenceByName("LadderUp")),
                    LaddersDown = combatMapReference.DoesTileReferenceOccurOnMap(tileReferences.GetTileReferenceByName("LadderDown")),
                    HasMagicDoor = combatMapReference.DoesTileReferenceOccurOnMap(tileReferences.GetTileReferenceByName("MagicLockDoor")) 
                        || combatMapReference.DoesTileReferenceOccurOnMap(tileReferences.GetTileReferenceByName("MagicLockDoorWithView")),
                    HasRegularDoor = combatMapReference.DoesTileReferenceOccurOnMap(tileReferences.GetTileReferenceByName("RegularDoor"))
                                     || combatMapReference.DoesTileReferenceOccurOnMap(tileReferences.GetTileReferenceByName("LockedDoor"))
                                     || combatMapReference.DoesTileReferenceOccurOnMap(tileReferences.GetTileReferenceByName("RegularDoorView"))
                                     || combatMapReference.DoesTileReferenceOccurOnMap(tileReferences.GetTileReferenceByName("LockedDoorView"))
                };

                _dungeonCombatMapReferences.Add(dungeonCombatMapReference);
            }
            Console.Write(GetAsCSV());
        }
    }
}