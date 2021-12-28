namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class AscendDescendSpellSubType : SpellSubType
    {
        public AscendDescendSpellSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        public override SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellResult spellResult = new(MagicRef.SpellEnum);

            DoSetupAndStateCheck(state, spellResult);

            switch (MagicRef.SpellEnum)
            {
                case MagicReference.SpellWords.Uus_Por:
                case MagicReference.SpellWords.Des_Por:
                    break;
                default:
                    throw new Ultima5ReduxException(
                        $"Tried to create AscendDescendSpellSubType with {MagicRef.SpellEnum}");
            }

            return spellResult;
        }
    }
}