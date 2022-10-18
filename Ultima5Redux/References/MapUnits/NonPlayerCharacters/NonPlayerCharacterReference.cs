using System;
using System.Runtime.InteropServices;
using Ultima5Redux.References.Dialogue;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.MapUnits.NonPlayerCharacters
{
    /// <summary>
    ///     A single non player character (NPC)
    /// </summary>
    public class NonPlayerCharacterReference
    {
        /// <summary>
        ///     NPC Type, any other value is a specific character
        /// </summary>
        public enum SpecificNpcDialogType
        {
            Blacksmith = 0x81, Barkeeper = 0x82, HorseSeller = 0x83, Shipwright = 0x84, Healer = 0x87, InnKeeper = 0x88,
            MagicSeller = 0x85, GuildMaster = 0x86, None = 0xFF, Guard = 0xFE
            // unknowns may be crown and sandlewood box
        }

        /// <summary>
        ///     The byte representing the type of character
        /// </summary>
        private byte CharacterType { get; }

        /// <summary>
        ///     The Dialog identifier
        /// </summary>
        private byte DialogNumber { get; }

        /// <summary>
        ///     0-31 index of it's position in the NPC arrays (used for saved.gam references)
        /// </summary>
        public int DialogIndex { get; }

        /// <summary>
        ///     friendlier version of name in case they are a profession and not named
        /// </summary>
        public string FriendlyName => Name == "" ? NpcType.ToString() : Name;

        public bool IsGuard => NpcType == SpecificNpcDialogType.Guard;

        public bool IsShoppeKeeper => NpcType is not SpecificNpcDialogType.None and not SpecificNpcDialogType.Guard;

        /// <summary>
        ///     Which map is the NPC on?
        /// </summary>
        public SmallMapReferences.SingleMapReference.Location MapLocation { get; }

        public byte MapLocationId => (byte)(MapLocation - 1);

        // based on Xu4 AI = (0x0-fixed, 0x1-wander, 0x80-follow, 0xFF-attack)

        /// <summary>
        ///     NPCs name
        /// </summary>
        public string Name =>
            Script == null
                ? string.Empty
                : Script.GetScriptLine(TalkScript.TalkConstants.Name).GetScriptItem(0).StringData.Trim();

        /// <summary>
        ///     They are either a merchant or they have a speaking role
        /// </summary>
        public bool NormalNPC => NpcType != SpecificNpcDialogType.None || DialogNumber > 0;

        public int NPCKeySprite => CharacterType + 0x100;

        /// <summary>
        ///     What type of NPC are they?
        /// </summary>
        public SpecificNpcDialogType NpcType
        {
            get
            {
                // special override in case it's a guard
                if (NPCKeySprite is (int)TileReference.SpriteIndex.Guard_KeyIndex
                    and <= (int)TileReference.SpriteIndex.Guard_KeyIndex + TileReference.N_TYPICAL_ANIMATION_FRAMES)
                    return SpecificNpcDialogType.Guard;

                // daemons at Windemere are guards too
                if (NPCKeySprite is (int)TileReference.SpriteIndex.Daemon1_KeyIndex
                        and <= (int)TileReference.SpriteIndex.Daemon1_KeyIndex +
                               TileReference.N_TYPICAL_ANIMATION_FRAMES
                    && MapLocation == SmallMapReferences.SingleMapReference.Location.Windemere)
                    return SpecificNpcDialogType.Guard;
                
                //if (NPCKeySprite is >= 368 and <= 371) return SpecificNpcDialogType.Guard;
                // it's the Avatar
                if (NPCKeySprite == 256) return SpecificNpcDialogType.None;

                // it's a merchant
                foreach (int npcType in Enum.GetValues(typeof(SpecificNpcDialogType)))
                {
                    if (npcType == DialogNumber)
                        return (SpecificNpcDialogType)npcType;
                }

                foreach (int npcType in Enum.GetValues(typeof(SpecificNpcDialogType)))
                {
                    if (npcType == CharacterType)
                        return (SpecificNpcDialogType)npcType;
                }

                return SpecificNpcDialogType.None;
            }
        }

        /// <summary>
        ///     The daily schedule of the NPC
        /// </summary>
        public NonPlayerCharacterSchedule Schedule { get; }

        /// <summary>
        ///     The talk script the NPC will follow
        /// </summary>
        public TalkScript Script { get; }

        /// <summary>
        ///     Construct an NPC
        /// </summary>
        /// <param name="schedule">daily schedule</param>
        /// <param name="npcType">type of NPC they are</param>
        /// <param name="dialogNumber">dialog number referencing data OVL</param>
        /// <param name="dialogIndex">0-31 index of it's position in the NPC arrays (used for saved.gam references)</param>
        /// <param name="talkScript">their conversation script</param>
        /// <param name="location"></param>
        public NonPlayerCharacterReference(SmallMapReferences.SingleMapReference.Location location,
            NpcSchedule schedule, byte npcType, byte dialogNumber, int dialogIndex, TalkScript talkScript)
        {
            MapLocation = location;

            CharacterType = npcType;
            DialogNumber = dialogNumber;
            Script = talkScript;
            DialogIndex = dialogIndex;

            Schedule = new NonPlayerCharacterSchedule(schedule);
            // this is converts ambiguous AI types like 4 (custom AI) to a more specific type (extorting guard)
            Schedule.AdaptAiTypesByNpcRef(location, this);
        }

        /// <summary>
        ///     Does the NPC have an empty schedule? If so, then they aren't actually an NPC
        /// </summary>
        /// <param name="schedule">daily schedule</param>
        /// <returns></returns>
        private static bool IsEmptySchedule(NpcSchedule schedule)
        {
            unsafe
            {
                if (schedule.times[0] == 0 && schedule.times[1] == 0 && schedule.times[2] == 0 &&
                    schedule.times[3] == 0 && schedule.times[4] == 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Return true if the dialog is not part of a standard dialog tree like a guard or shopkeeper
        /// </summary>
        /// <param name="dialogType">The dialog type that yu want to compare</param>
        /// <returns></returns>
        public static bool IsSpecialDialogType(SpecificNpcDialogType dialogType)
        {
            if (dialogType == 0) return true;
            foreach (SpecificNpcDialogType tempDialogType in (SpecificNpcDialogType[])Enum.GetValues(
                         typeof(SpecificNpcDialogType)))
            {
                if (dialogType == tempDialogType)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Original structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct NpcSchedule
        {
            public fixed byte AI_types[3];
            public fixed byte x_coordinates[3];
            public fixed byte y_coordinates[3];
            public fixed byte z_coordinates[3];
            public fixed byte times[4];
        }
    }
}