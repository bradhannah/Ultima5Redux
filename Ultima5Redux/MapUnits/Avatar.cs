using System;
using System.Collections.Generic;
using Ultima5Redux.Data;
using Ultima5Redux.DayNightMoon;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits
{
    public class Avatar : MapUnit
    {
        public Avatar(TileReferences tileReferences, MapUnitPosition avatarPosition, 
            SmallMapReferences.SingleMapReference.Location location, MapUnitMovement movement,
            MapUnitState mapUnitState = null) : base()
        {
            if (mapUnitState == null)
            {
                TheMapUnitState = MapUnitState.CreateAvatar(tileReferences, SmallMapReferences.GetStartingXYZByLocation(location));
            }
            else
            {
                TheMapUnitState = MapUnitState.CreateAvatar(tileReferences, SmallMapReferences.GetStartingXYZByLocation(location),
                    mapUnitState);
            }
            TileReferences = tileReferences;

            CurrentDirection = TheMapUnitState.Tile1Ref.GetDirection();
            CurrentAvatarState = CalculateAvatarState(TheMapUnitState.Tile1Ref);
            
            //TheMapUnitState = mapUnitState;
            // set the initial key tile
            // KeyTileReference = TheMapUnitState.Tile1Ref;
            // _ = KeyTileReference;
            Movement = movement;
        }

        private AvatarState CalculateAvatarState(TileReference tileReference)
        {
            if (tileReference.Name == "BasicAvatar") return AvatarState.Regular;
            if (tileReference.Name.StartsWith("Ship")) return AvatarState.Frigate;
            if (tileReference.Name.StartsWith("Skiff")) return AvatarState.Skiff;
            if (tileReference.Name.StartsWith("RidingMagicCarpet")) return AvatarState.Carpet;
            if (tileReference.Name.StartsWith("RidingHorse")) return AvatarState.Horse;
            throw new Ultima5ReduxException("Asked to calculate AvatarState of "+tileReference.Name + " but you can't do that, it's not a thing!");
        }

        public override bool IsActive => true;

        public enum AvatarState { Regular, Carpet, Horse, Frigate, Skiff, Hidden }

        public AvatarState CurrentAvatarState { get; private set; } = AvatarState.Regular;
        public VirtualMap.Direction PreviousDirection { get; private set; } = VirtualMap.Direction.None;
        public VirtualMap.Direction CurrentDirection { get; private set; } = VirtualMap.Direction.None;
        public bool AreSailsUp { get; set; } = false;
        
        /// <summary>
        /// Map of all sprites the current state and avatar direction
        /// </summary>
        private readonly Dictionary<AvatarState, Dictionary<VirtualMap.Direction, string>> 
        _tileIndexMap = new Dictionary<AvatarState, Dictionary<VirtualMap.Direction, string>>()
            { 
                { 
                    AvatarState.Carpet, new Dictionary<VirtualMap.Direction, string> ()
                    {
                        {VirtualMap.Direction.None, "RidingMagicCarpetLeft"},
                        {VirtualMap.Direction.Left, "RidingMagicCarpetLeft"},
                        {VirtualMap.Direction.Down, "RidingMagicCarpetLeft"},
                        {VirtualMap.Direction.Right, "RidingMagicCarpetRight"},
                        {VirtualMap.Direction.Up, "RidingMagicCarpetRight"},
                    }
                },
                { 
                    AvatarState.Regular, new Dictionary<VirtualMap.Direction, string> ()
                    {
                        {VirtualMap.Direction.None, "BasicAvatar"},
                        {VirtualMap.Direction.Left, "BasicAvatar"},
                        {VirtualMap.Direction.Down, "BasicAvatar"},
                        {VirtualMap.Direction.Right, "BasicAvatar"},
                        {VirtualMap.Direction.Up, "BasicAvatar"},
                    }
                },
                { 
                    AvatarState.Frigate, new Dictionary<VirtualMap.Direction, string> ()
                    {
                        {VirtualMap.Direction.None, "ShipNoSailsUp"},
                        {VirtualMap.Direction.Left, "ShipNoSailsLeft"},
                        {VirtualMap.Direction.Down, "ShipNoSailsDown"},
                        {VirtualMap.Direction.Right, "ShipNoSailsRight"},
                        {VirtualMap.Direction.Up, "ShipNoSailsUp"},
                    }
                },
                { 
                    AvatarState.Skiff, new Dictionary<VirtualMap.Direction, string> ()
                    {
                        {VirtualMap.Direction.None, "SkiffLeft"},
                        {VirtualMap.Direction.Left, "SkiffLeft"},
                        {VirtualMap.Direction.Down, "SkiffDown"},
                        {VirtualMap.Direction.Right, "SkiffRight"},
                        {VirtualMap.Direction.Up, "SkiffUp"},
                    }
                },           
                { 
                    AvatarState.Horse, new Dictionary<VirtualMap.Direction, string> ()
                    {
                        {VirtualMap.Direction.None, "HorseLeft"},
                        {VirtualMap.Direction.Left, "HorseLeft"},
                        {VirtualMap.Direction.Down, "HorseLeft"},
                        {VirtualMap.Direction.Right, "HorseRight"},
                        {VirtualMap.Direction.Up, "HorseRight"},
                    }
                },           
            };

        private string GetSpriteName()
        {
            // if the sails are up then we make a slight modification to show the sails up
            if (CurrentAvatarState == AvatarState.Frigate && AreSailsUp)
            {
                return _tileIndexMap[CurrentAvatarState][CurrentDirection].Replace("No", "");
            }

            return _tileIndexMap[CurrentAvatarState][CurrentDirection];
        }
        
        public void Move(VirtualMap.Direction direction)
        {
            PreviousDirection = CurrentDirection;
            CurrentDirection = direction;
            // set the new sprite to reflect the new direction
            TheMapUnitState.SetTileReference(TileReferences.GetTileReferenceByName(GetSpriteName()));
        }
        
        /// <summary>
        /// Creates an Avatar MapUnit at the default small map position
        /// Note: this should never need to be called from a LargeMap since the values persist on disk
        /// </summary>
        /// <param name="tileReferences"></param>
        /// <param name="location"></param>
        /// <param name="movement"></param>
        /// <returns></returns>
        public static MapUnit CreateAvatar(TileReferences tileReferences, 
            SmallMapReferences.SingleMapReference.Location location, MapUnitMovement movement,
            MapUnitState mapUnitState = null)
        {
            Avatar theAvatar = new Avatar(tileReferences, SmallMapReferences.GetStartingXYZByLocation(location), 
                location, movement, mapUnitState);
            
            return theAvatar;
        }

        /// <summary>
        /// Show the Avatar that isn't boarded on top of anything
        /// </summary>
        public void SetUnboardedAvatar()
        {
            KeyTileReference = TileReferences.GetTileReferenceByName("BasicAvatar");
        }
        
        public void SetBoardedCarpet()
        {
            KeyTileReference = TileReferences.GetTileReferenceByName("RidingMagicCarpetRight");
        }

        public void SetBoardedHorse()
        {
            KeyTileReference = TileReferences.GetTileReferenceByName("RidingHorseRight");
        }

        public void SetBoardedFrigate(VirtualMap.Direction direction, bool bSailsUp = false)
        {
            KeyTileReference = TileReferences.GetTileReferenceByName("ShipNoSailsRight");
        }

        public void SetBoardedSkiff()
        {
            KeyTileReference = TileReferences.GetTileReferenceByName("SkiffRight");
        }
    }
}