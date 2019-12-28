using System;
using System.Runtime.InteropServices;

namespace Ultima5Redux
{
    public partial class NonPlayerCharacters
    {
        /// <summary>
        /// A single non player character (NPC)
        /// </summary>
        public partial class NonPlayerCharacter
        {
            #region Private Variables
            private GameState gameStateRef;
            #endregion

            #region Constants, Sructures and Enums
            /// <summary>
            /// Original structure
            /// </summary>
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public unsafe struct NPC_Schedule
            {
                public fixed byte AI_types[3];
                public fixed byte x_coordinates[3];
                public fixed byte y_coordinates[3];
                public fixed byte z_coordinates[3];
                public fixed byte times[4];
            };

            /// <summary>
            /// NPC Type, any other value is a specific character
            /// </summary>
            public enum NPCDialogTypeEnum
            {
                Custom = -1, Guard = 0, WeaponsDealer = 0x81, Barkeeper = 0x82, HorseSeller = 0x83, ShipSeller = 0x84, Healer = 0x87,
                InnKeeper = 0x88, MagicSeller = 0x85, GuildMaster = 0x86, Unknown = 0xFF
                    // unknowns may be crown and sandlewood box
            };
            public enum NPCKeySpriteEnum
            {
                Custom = -1, Guard = 368, Merchant = 340, Healer = 320,
                UnknownX86 = 368, Bard = 324, Fighter = 328, Towny = 336, BardPlaying = 348, Jester = 344, Child = 360, Beggar = 364,
                Apparition = 372, BlackThorn = 376, LordBritish = 380, Unknown = 0xFF
            };

            // based on Xu4 AI = (0x0-fixed, 0x1-wander, 0x80-follow, 0xFF-attack)

            #endregion

            #region Public Static Methods
            /// <summary>
            /// Return true if the dialog is not part of a standard dialog tree like a guard or shopkeeper
            /// </summary>
            /// <param name="dialogType">The dialog type that yu want to compare</param>
            /// <returns></returns>
            static public bool IsSpecialDialogType(NPCDialogTypeEnum dialogType)
            {
                foreach (NPCDialogTypeEnum tempDialogType in (NPCDialogTypeEnum[])Enum.GetValues(typeof(NPCDialogTypeEnum)))
                {
                    if (dialogType == tempDialogType)
                        return true;
                }
                return false;
            }
            #endregion

            #region Public Properties
            /// <summary>
            /// NPCs name
            /// </summary>
            public string Name
            {
                get
                {
                    if (Script != null)
                    {
                        return Script.GetScriptLine(TalkScript.TalkConstants.Name).GetScriptItem(0).Str.Trim();
                    }
                    else
                    {
                        return string.Empty;
                    }
                } 
            }

            /// <summary>
            /// The daily schedule of the NPC
            /// </summary>
            public NPCSchedule Schedule { get; }
            /// <summary>
            /// The Dialog identifier
            /// </summary>
            public byte DialogNumber { get; }

            /// <summary>
            /// 0-31 index of it's position in the NPC arrays (used for saved.gam references)
            /// </summary>
            public int DialogIndex { get; }

            /// <summary>
            /// The byte representing the type of character
            /// </summary>
            private byte CharacterType { get; }

            /// <summary>
            /// The talk script the NPC will follow
            /// </summary>
            public TalkScript Script { get; }

            /// <summary>
            /// Which map is the NPC on?
            /// </summary>
            public SmallMapReferences.SingleMapReference MapReference { get; }

            /// <summary>
            /// What type of NPC are they? 
            /// </summary>
            public NPCDialogTypeEnum NPCType {
                get
                {
                    foreach (int npctype in Enum.GetValues(typeof(NPCDialogTypeEnum)))
                    {
                        if (npctype == DialogNumber && npctype != (int)NPCDialogTypeEnum.Custom)
                        {
                            return (NPCDialogTypeEnum)npctype;
                        }
                    }
                    return NPCDialogTypeEnum.Custom;
                }
            }

            public int NPCKeySprite
            {
                get
                {
                    switch (this.NPCType)
                    {
                        case NPCDialogTypeEnum.Custom:
                            return (int)CharacterType + 0x100;
//                            return (int)NPCKeySpriteEnum.Guard;
                        case NPCDialogTypeEnum.Guard:
                            return (int)NPCKeySpriteEnum.Guard;
                        case NPCDialogTypeEnum.WeaponsDealer:
                        case NPCDialogTypeEnum.Barkeeper:
                        case NPCDialogTypeEnum.HorseSeller:
                        case NPCDialogTypeEnum.ShipSeller:
                        case NPCDialogTypeEnum.InnKeeper:
                        case NPCDialogTypeEnum.MagicSeller:
                        case NPCDialogTypeEnum.GuildMaster:
                            return (int)NPCKeySpriteEnum.Merchant;
                        case NPCDialogTypeEnum.Healer:
                            return (int)NPCKeySpriteEnum.Healer;
                        case NPCDialogTypeEnum.Unknown:
                            return (int)NPCKeySpriteEnum.Guard;
                        default:
                            throw new Exception("Unrecognized NPC type");
                    }
                }
            }

            /// <summary>
            /// Returns true if the NPC knows/has met the Avatar
            /// </summary>
            public bool KnowTheAvatar
            {
                get
                {
                    int nScriptLines = Script.NumberOfScriptLines;

                    // two steps - first if the NPC Has met flag is flipped in saved.gam then we know they have met the Avatar
                    // secondly, if the AskName command is not present in their entire script, then we can surmise that they must already know the Avatar (from the old days)

                    if (gameStateRef.NpcHasMetAvatar(this))
                    {
                        return true;
                    }

                    for (int i = 0; i < nScriptLines; i++)
                    {
                        if (Script.GetScriptLine(i).ContainsCommand(TalkScript.TalkCommand.AskName))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                set
                {
                    gameStateRef.SetNpcHasMetAvatar(this, value);
                }
            }
            #endregion

            #region Constructors
            /// <summary>
            /// Construct an NPC
            /// </summary>
            /// <param name="mapRef">Which map are they on?</param>
            /// <param name="sched">daily schedule</param>
            /// <param name="npcType">type of NPC they are</param>
            /// <param name="dialogNumber">dialog number referencing data OVL</param>
            /// <param name="dialogIndex">0-31 index of it's position in the NPC arrays (used for saved.gam references)</param>
            /// <param name="talkScript">their conversation script</param>
            public NonPlayerCharacter (SmallMapReferences.SingleMapReference mapRef, GameState gameStateRef, NPC_Schedule sched, byte npcType, byte dialogNumber, int dialogIndex, TalkScript talkScript)
            {
                Schedule = new NPCSchedule(sched);
                MapReference = mapRef;
                CharacterType = npcType;
                DialogNumber = dialogNumber;
                Script = talkScript;
                DialogIndex = dialogIndex;
                this.gameStateRef = gameStateRef;

                // no schedule? I guess you're not real
                if (!IsEmptySched(sched))
                {
                    System.Console.WriteLine(mapRef.MasterFile.ToString() + "     NPC Number: " + this.DialogNumber + " in " + mapRef.MapLocation.ToString());
                }
            }
            #endregion

            #region Private Methods

            /// <summary>
            /// Does the NPC have an empty schedule? If so, then they aren't actually an NPC
            /// </summary>
            /// <param name="sched">daily schedule</param>
            /// <returns></returns>
            static private bool IsEmptySched(NPC_Schedule sched)
            {
                unsafe
                {
                    if (sched.times[0] == 0 && sched.times[1] == 0 && sched.times[2] == 0 && sched.times[3] == 0 && sched.times[4] == 0)
                        return true;
                }
                return false;
            }
            #endregion
        }



    }
}
