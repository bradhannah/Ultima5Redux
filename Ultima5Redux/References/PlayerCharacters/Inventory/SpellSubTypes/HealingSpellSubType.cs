namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class HealingSpellSubType : SpellSubType
    {
        public HealingSpellSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        public override SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellResult spellResult = new(MagicRef.SpellEnum);

            DoSetupAndStateCheck(state, spellResult);

            switch (MagicRef.SpellEnum)
            {
                case MagicReference.SpellWords.An_Zu:
                case MagicReference.SpellWords.An_Nox:
                case MagicReference.SpellWords.Mani:
                case MagicReference.SpellWords.Vas_Mani:
                case MagicReference.SpellWords.In_Mani_Corp:
                    break;
                default:
                    throw new Ultima5ReduxException($"Tried to create HealingSpellSubType with {MagicRef.SpellEnum}");
            }

            return spellResult;
        }
    }
}