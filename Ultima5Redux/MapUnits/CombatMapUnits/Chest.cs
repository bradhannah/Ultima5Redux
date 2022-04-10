using Ultima5Redux.References;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class Chest : NonAttackingUnit
    {
        public override string FriendlyName => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.Vision2Strings.A_WOODEN_CHEST_DOT_N).TrimEnd();

        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;
        public override string Name => FriendlyName;
    }
}