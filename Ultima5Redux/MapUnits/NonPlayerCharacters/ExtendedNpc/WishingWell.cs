using Ultima5Redux.References;
using Ultima5Redux.References.Dialogue;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ExtendedNpc
{
    public class WishingWell : NonPlayerCharacter
    {
        private WishingWell(MapUnitMovement mapUnitMovement, SmallMapReferences.SingleMapReference.Location location,
            MapUnitPosition mapUnitPosition, NonPlayerCharacterState npcState) :
            base(null, mapUnitMovement, false, location, mapUnitPosition, npcState)
        {
        }

        public static WishingWell Create(SmallMapReferences.SingleMapReference.Location location, Point2D xy,
            int nFloor)
        {
            var schedule = new NonPlayerCharacterReference.NpcSchedule();

            TalkScript wishingWellTalkScript =
                GameReferences.Instance.TalkScriptsRef.GetCustomTalkScript("WishingWell");

            var npcRef = new NonPlayerCharacterReference(
                location, schedule, 0, 0, 0, wishingWellTalkScript);

            var npcState = new NonPlayerCharacterState(npcRef, true);

            var theWell = new WishingWell(
                new MapUnitMovement(npcState.NPCRef.DialogIndex),
                location,
                new MapUnitPosition(xy.X, xy.Y, nFloor),
                npcState
            );
            return theWell;
        }
    }
}