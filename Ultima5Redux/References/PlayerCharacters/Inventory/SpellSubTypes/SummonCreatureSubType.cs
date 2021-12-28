namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class SummonCreatureSubType : SpellSubType
    {
        public SummonCreatureSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        public override SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellResult spellResult = new(MagicRef.SpellEnum);

            DoSetupAndStateCheck(state, spellResult);

            switch (MagicRef.SpellEnum)
            {
                case MagicReference.SpellWords.Kal_Xen:
                case MagicReference.SpellWords.In_Flam_Grav:
                case MagicReference.SpellWords.In_Nox_Grav:
                case MagicReference.SpellWords.In_Zu_Grav:
                case MagicReference.SpellWords.In_Sanct_Grav:
                case MagicReference.SpellWords.In_Bet_Xen:
                case MagicReference.SpellWords.In_Quas_Xen:
                case MagicReference.SpellWords.Kal_Xen_Corp:
                    break;
                default:
                    throw new Ultima5ReduxException($"Tried to create SummonCreatureSubType with {MagicRef.SpellEnum}");
            }

            return spellResult;
        }
    }
}