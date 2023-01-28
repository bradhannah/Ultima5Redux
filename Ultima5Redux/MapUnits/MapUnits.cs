// using System;
// using System.Runtime.Serialization;
// using Ultima5Redux.Maps;
// using Ultima5Redux.References;
// using Ultima5Redux.References.Maps;
//
// namespace Ultima5Redux.MapUnits
// {
//     [DataContract] public class MapUnits
//     {
//
//
//         [DataMember(Name = "InitialMapType")] private Map.Maps _initialMapType;
//
//
//         // [DataMember]
//         // public MapUnitCollection CombatMapMapUnitCollection { get; private set; } = new();
//         //
//
//         // [DataMember] public MapUnitCollection OverworldMapMapUnitCollection { get; protected set; } = new();
//         //
//         // // load the SmallMapCharacterStates once from disk, don't worry abut again until you are saving to disk
//         // [DataMember] public MapUnitCollection SmallMapUnitCollection { get; protected set; } = new();
//         //
//         // [DataMember] public MapUnitCollection DungeonMapUnitCollection { get; protected set; } = new();
//         //
//         // [DataMember]
//         // public MapUnitCollection UnderworldMapUnitCollection { get; protected set; } = new();
//
//
//         // [IgnoreDataMember] public MapUnitCollection CurrentMapUnits => GetMapUnitCollection(CurrentMapType);
//
//
//
//         /// <summary>
//         ///     Constructs the collection of all Map CurrentMapUnits in overworld, underworld and current towne
//         ///     from a legacy save import
//         /// </summary>
//         /// <param name="initialMap">
//         ///     The initial map you are beginning on. It's important to know because there is only
//         ///     one TheSmallMapCharacterState loaded in the save file at load time
//         /// </param>
//         /// <param name="bUseExtendedSprites"></param>
//         /// <param name="importedGameState"></param>
//         /// <param name="searchItems"></param>
//         /// <param name="currentSmallMap">The particular map (if small map) that you are loading</param>
//         internal MapUnits(Map.Maps initialMap, bool bUseExtendedSprites, ImportedGameState importedGameState,
//             SearchItems searchItems,
//             SmallMapReferences.SingleMapReference.Location currentSmallMap =
//                 SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
//         {
//             // CurrentMapType = initialMap;
//             // // the initial map is important because it contains the Master Avatar for duplication
//             // _initialMapType = initialMap;
//             // //bUseExtendedSprites = bUseExtendedSprites;
//             // _importedGameState = importedGameState;
//             // CurrentLocation = currentSmallMap;
//             //
//             // // movements pertain to whichever map was loaded from disk
//             // _importedMovements = importedGameState.CharacterMovements;
//             //
//             // // we only load the large maps once and they always exist on disk
//             // GenerateMapUnitsForLargeMap(Map.Maps.Overworld, true, searchItems);
//             // GenerateMapUnitsForLargeMap(Map.Maps.Underworld, true, searchItems);
//             //
//             // // if the small map is the initial map, then load it 
//             // // otherwise we force the correct states to either the over or underworld
//             // switch (initialMap)
//             // {
//             //     case Map.Maps.Small:
//             //         LoadSmallMap(currentSmallMap, true, searchItems);
//             //         break;
//             //     case Map.Maps.Overworld:
//             //         break;
//             //     case Map.Maps.Underworld:
//             //         break;
//             //     case Map.Maps.Combat:
//             //         throw new Ultima5ReduxException("You can't initialize the MapUnits with a combat map");
//             //     default:
//             //         throw new ArgumentOutOfRangeException(nameof(initialMap), initialMap, null);
//             // }
//             //
//             // // We will reassign each AvatarMapUnit to the active one. This will ensure that when the Avatar
//             // // has boarded something, it should carry between maps
//             // SetCommonAvatarMapUnit();
//             //
//             // //SetAllExtendedSprites();
//             //
//             // CurrentMapType = initialMap;
//         }
//
//         //[JsonConstructor] private MapUnits() => _importedMovements = new MapUnitMovements();
//
//         [OnDeserialized] private void PostDeserialized(StreamingContext context)
//         {
//             SetCommonAvatarMapUnit();
//         }
//
//         // combat
//
//
//         // regular
//
//         // regular
//
//         // toss
//         // internal MapUnitCollection GetMapUnitCollection(Map.Maps map)
//         // {
//         //     return map switch
//         //     {
//         //         Map.Maps.Dungeon => DungeonMapUnitCollection,
//         //         Map.Maps.Small => SmallMapUnitCollection,
//         //         Map.Maps.Overworld => OverworldMapMapUnitCollection,
//         //         Map.Maps.Underworld => UnderworldMapUnitCollection,
//         //         Map.Maps.Combat => CombatMapMapUnitCollection,
//         //         _ => throw new InvalidEnumArgumentException(((int)map).ToString())
//         //     };
//         // }
//
//         // regular map
//
//
//         // regular map
//
//
//
//
//      
//
//    
//
//        
//
//
//      
//
//         /// <summary>
//         ///     Force all map units to use or not use extended sprites based on _bUseExtendedSprites field
//         /// </summary>
//         // private void SetAllExtendedSprites()
//         // {
//         //     OverworldMapMapUnitCollection.AllMapUnits.ForEach(m => m.UseFourDirections = _bUseExtendedSprites);
//         //     UnderworldMapUnitCollection.AllMapUnits.ForEach(m => m.UseFourDirections = _bUseExtendedSprites);
//         //     SmallMapUnitCollection?.AllMapUnits.ForEach(m => m.UseFourDirections = _bUseExtendedSprites);
//         // }
//
//         private void SetCommonAvatarMapUnit()
//         {
//             MasterAvatarMapUnit = GetAvatarMapUnit();
//             GetMapUnitCollection(Map.Maps.Overworld).AllMapUnits[0] = MasterAvatarMapUnit;
//             GetMapUnitCollection(Map.Maps.Underworld).AllMapUnits[0] = MasterAvatarMapUnit;
//         }
//
//
//
//  
//       
//
//     
//
//
//
//
//
//         /// <summary>
//         ///     Sets the current map type
//         ///     Called internally to the class only since it has the bLoadFromDisk option
//         /// </summary>
//         /// <param name="mapRef"></param>
//         /// <param name="mapType"></param>
//         /// <param name="searchItems"></param>
//         /// <param name="bLoadFromDisk"></param>
//         /// <exception cref="ArgumentOutOfRangeException"></exception>
//         public void SetCurrentMapType(SmallMapReferences.SingleMapReference mapRef, Map.Maps mapType,
//             SearchItems searchItems, bool bLoadFromDisk = false)
//         {
//             SetCurrentMapTypeNoLoad(mapRef, mapType, false);
//
//             switch (mapType)
//             {
//                 case Map.Maps.Small:
//                     LoadSmallMap(mapRef.MapLocation, bLoadFromDisk, searchItems);
//                     // will reload the search items fresh since we don't save every single small
//                     // map to the save file
//                     break;
//                 case Map.Maps.Combat:
//                     return;
//                 case Map.Maps.Dungeon:
//                     LoadDungeonMap(mapRef.MapLocation);
//                     break;
//                 case Map.Maps.Overworld:
//                 case Map.Maps.Underworld:
//                     CombatMapMapUnitCollection.Clear();
//                     // search items should only be loaded once and then never again
//                     break;
//                 default:
//                     throw new ArgumentOutOfRangeException(nameof(mapType), mapType, null);
//             }
//
//             GetAvatarMapUnit().MapLocation = mapRef.MapLocation;
//             GetAvatarMapUnit().MapUnitPosition.Floor = mapRef.Floor;
//         }
//
//         /// <summary>
//         ///     UNSAFE - this assumes the small map is already loaded into memory - it instead just switches the
//         ///     current map units back over to it.
//         /// </summary>
//         /// <param name="mapRef"></param>
//         /// <param name="mapType"></param>
//         /// <param name="bSetAvatar"></param>
//         /// <exception cref="Ultima5ReduxException"></exception>
//         public void SetCurrentMapTypeNoLoad(SmallMapReferences.SingleMapReference mapRef, Map.Maps mapType,
//             bool bSetAvatar = true)
//         {
//             TheMapType = mapType;
//             if (mapRef == null)
//                 throw new Ultima5ReduxException("Passed a null map ref to SetTheMapType");
//
//             CurrentLocation = mapRef.MapLocation;
//
//             // I may need make an additional save of state before wiping these MapUnits out
//             GameStateReference.State.CharacterRecords.ClearCombatStatuses();
//
//             if (bSetAvatar)
//             {
//                 GetAvatarMapUnit().MapLocation = mapRef.MapLocation;
//                 GetAvatarMapUnit().MapUnitPosition.Floor = mapRef.Floor;
//             }
//         }
//
//
//     }
// }

