namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class OtherSpellSubType : SpellSubType
    {
        public OtherSpellSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        public override SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellResult spellResult = new(MagicRef.SpellEnum);

            DoSetupAndStateCheck(state, spellResult);

            switch (MagicRef.SpellEnum)
            {
                case MagicReference.SpellWords.In_An:
                case MagicReference.SpellWords.Rel_Xen_Bet:
                case MagicReference.SpellWords.An_Tym:
                case MagicReference.SpellWords.Nox:
                    break;
                default:
                    throw new Ultima5ReduxException($"Tried to create OtherSpellSubType with {MagicRef.SpellEnum}");
            }

            return spellResult;
        }
    }
}