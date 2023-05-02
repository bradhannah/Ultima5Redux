using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class TeleportNewLocation : TurnResult
    {
        public SmallMapReferences.SingleMapReference.Location TheLocation { get; }
        public Point2D TeleportPosition { get; }
        public int TeleportFloor { get; }

        public enum TeleportType { InnSleep, Waterfall, Whirlpool, CombatMap, EnterSmallMap }

        public TeleportType TheTeleportType { get; }

        public TeleportNewLocation(
            SmallMapReferences.SingleMapReference.Location theLocation, Point2D teleportPosition, int teleportFloor,
            TeleportType theTeleportType) : base(
            TurnResultType.TeleportToNewLocation, TurnResulActionType.ActionRequired)
        {
            TheLocation = theLocation;
            TeleportPosition = teleportPosition;
            TeleportFloor = teleportFloor;
            TheTeleportType = theTeleportType;

            if (TheLocation == SmallMapReferences.SingleMapReference.Location.Britannia_Underworld)
            {
                if (TeleportFloor != 0 && TeleportFloor != -1)
                {
                    throw new Ultima5ReduxException("Teleport floor for Over/underworld must be 0 or -1");
                }
            }
            else if (!GameReferences.Instance.SmallMapRef.DoesFloorExist(TheLocation, teleportFloor))
            {
                throw new Ultima5ReduxException($"Small map floor doesn't exist: {TheLocation}:{TeleportFloor}");
            }
        }
    }
}