using System.Diagnostics.CodeAnalysis;

namespace Ultima5Redux.References
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class FileConstants
    {
        public const string BRIT_CBT = "BRIT.CBT";

        public const string BRIT_DAT = "BRIT.DAT";
        public const string BRIT_OOL = "BRIT.OOL";

        public const string CASTLE_DAT = "CASTLE.DAT";

        public const string CASTLE_NPC = "CASTLE.NPC";

        public const string CASTLE_TLK = "CASTLE.TLK";
        public const string DATA_OVL = "DATA.OVL";
        public const string DUNGEON_CBT = "DUNGEON.CBT";
        public const string DUNGEON_DAT = "DUNGEON.DAT";

        public const string DWELLING_DAT = "DWELLING.DAT";
        public const string DWELLING_NPC = "DWELLING.NPC";
        public const string DWELLING_TLK = "DWELLING.TLK";
        public const string INIT_GAM = "INIT.GAM";
        public const string KEEP_DAT = "KEEP.DAT";
        public const string KEEP_NPC = "KEEP.NPC";
        public const string KEEP_TLK = "KEEP.TLK";
        public const string LOOK2_DAT = "LOOK2.DAT";

        public const string SAVED_GAM = "SAVED.GAM";
        public const string SAVED_OOL = "SAVED.OOL";

        public const string SHOPPE_DAT = "SHOPPE.DAT";
        public const string SIGNS_DAT = "SIGNS.DAT";

        public const string TOWNE_DAT = "TOWNE.DAT";
        public const string TOWNE_NPC = "TOWNE.NPC";
        public const string TOWNE_TLK = "TOWNE.TLK";
        public const string UNDER_DAT = "UNDER.DAT";
        public const string UNDER_OOL = "UNDER.OOL";
        public static readonly string[] TalkFiles = { CASTLE_TLK, TOWNE_TLK, DWELLING_TLK, KEEP_TLK };
        public static readonly string[] NPCFiles = { CASTLE_NPC, TOWNE_NPC, DWELLING_NPC, KEEP_NPC };
        public static readonly string[] SmallMapFiles = { CASTLE_DAT, TOWNE_DAT, DWELLING_DAT, KEEP_DAT };

        public const string NEW_SAVE_FILE = "save.json";
        public const string NEW_SAVE_SUMMARY_FILE = "summary.json";
    }
}