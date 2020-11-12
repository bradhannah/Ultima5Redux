using System.Diagnostics.CodeAnalysis;

namespace Ultima5Redux
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")] 
    public static class FileConstants
    {
        public const string DATA_OVL = "data.ovl";

        public const string SHOPPE_DAT = "shoppe.dat";

        public const string SAVED_GAM = "saved.gam";
        public const string INIT_GAM = "init.gam";
        public const string BRIT_OOL = "brit.ool";
        public const string UNDER_OOL = "under.ool";
        public const string SAVED_OOL = "saved.ool";

        public const string BRIT_DAT = "brit.dat";
        public const string UNDER_DAT = "under.dat";
        public const string LOOK2_DAT = "look2.dat";
        public const string SIGNS_DAT = "signs.dat";

        public const string CASTLE_DAT = "castle.dat";
        public const string TOWNE_DAT = "towne.dat";
        public const string DWELLING_DAT = "dwelling.dat";
        public const string KEEP_DAT = "keep.dat";

        public const string CASTLE_NPC = "castle.Npc";
        public const string TOWNE_NPC = "towne.Npc";
        public const string DWELLING_NPC = "dwelling.Npc";
        public const string KEEP_NPC = "keep.Npc";

        public const string CASTLE_TLK = "castle.tlk";
        public const string TOWNE_TLK = "towne.tlk";
        public const string DWELLING_TLK = "dwelling.tlk";
        public const string KEEP_TLK = "keep.tlk";

        public static readonly string[] TalkFiles = {CASTLE_TLK, TOWNE_TLK, DWELLING_TLK, KEEP_TLK};
        public static readonly string[] NPCFiles = {CASTLE_NPC, TOWNE_NPC, DWELLING_NPC, KEEP_NPC};
        public static readonly string[] SmallMapFiles = {CASTLE_DAT, TOWNE_DAT, DWELLING_DAT, KEEP_DAT};
    }
}