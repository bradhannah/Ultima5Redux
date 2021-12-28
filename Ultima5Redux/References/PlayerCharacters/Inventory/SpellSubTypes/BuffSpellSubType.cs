namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class BuffSpellSubType : SpellSubType
    {
        public BuffSpellSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        public override SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellResult spellResult = new(MagicRef.SpellEnum);

            DoSetupAndStateCheck(state, spellResult);

            switch (MagicRef.SpellEnum)
            {
                case MagicReference.SpellWords.In_Sanct:
                case MagicReference.SpellWords.Rel_Tym:
                case MagicReference.SpellWords.Sanct_Lor:
                    break;
                default:
                    throw new Ultima5ReduxException($"Tried to create BuffSpellSubType with {MagicRef.SpellEnum}");
            }

            return spellResult;
        }
    }
}