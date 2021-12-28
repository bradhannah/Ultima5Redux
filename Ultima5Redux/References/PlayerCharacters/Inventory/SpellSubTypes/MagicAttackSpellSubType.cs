namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class MagicAttackSpellSubType : SpellSubType
    {
        public MagicAttackSpellSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        public override SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellResult spellResult = new(MagicRef.SpellEnum);

            DoSetupAndStateCheck(state, spellResult);

            switch (MagicRef.SpellEnum)
            {
                case MagicReference.SpellWords.Grav_Por:
                case MagicReference.SpellWords.Vas_Flam:
                case MagicReference.SpellWords.In_Vas_Por_Ylem:
                case MagicReference.SpellWords.Xen_Corp:
                    break;
                default:
                    throw new Ultima5ReduxException(
                        $"Tried to create MagicAttackSpellSubType with {MagicRef.SpellEnum}");
            }

            return spellResult;
        }
    }
}