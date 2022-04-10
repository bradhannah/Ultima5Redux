using Ultima5Redux.References;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public class BloodSpatter : NonAttackingUnit
    {
        //GUTS_BANG_N, A_BLOOD_PULP_BANG_N,
        public override string FriendlyName => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.ThingsIFindStrings.GUTS_BANG_N).TrimEnd();

        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;
        public override string Name => FriendlyName;
    }
}