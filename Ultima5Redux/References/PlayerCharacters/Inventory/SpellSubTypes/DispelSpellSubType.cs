namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class DispelSpellSubType : SpellSubType
    {
        public DispelSpellSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        public override SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellResult spellResult = new(MagicRef.SpellEnum);

            DoSetupAndStateCheck(state, spellResult);

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (MagicRef.SpellEnum)
            {
                case MagicReference.SpellWords.An_Sanct:
                case MagicReference.SpellWords.An_Xen_Corp:
                case MagicReference.SpellWords.An_Grav:
                case MagicReference.SpellWords.Wis_Quas:
                    break;
                default:
                    throw new Ultima5ReduxException($"Tried to create DispelSpellSubType with {MagicRef.SpellEnum}");
            }

            return spellResult;
        }
    }
}