namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class UtilitySpellSubType : SpellSubType
    {
        public UtilitySpellSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        public override SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellResult spellResult = new(MagicRef.SpellEnum);

            DoSetupAndStateCheck(state, spellResult);

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (MagicRef.SpellEnum)
            {
                case MagicReference.SpellWords.In_Lor:
                    state.TurnsToExtinguish = World.N_DEFAULT_NUMBER_OF_TURNS_FOR_TORCH;
                    spellResult.Status = SpellResult.SpellResultStatus.Success;
                    break;
                case MagicReference.SpellWords.An_Ylem:
                case MagicReference.SpellWords.Rel_Hur:
                case MagicReference.SpellWords.In_Wis:
                case MagicReference.SpellWords.In_Xen_Mani:
                case MagicReference.SpellWords.Vas_Lor:
                case MagicReference.SpellWords.In_Por:
                case MagicReference.SpellWords.An_Ex_Por:
                case MagicReference.SpellWords.In_Ex_Por:
                case MagicReference.SpellWords.Wis_An_Ylem:
                case MagicReference.SpellWords.In_Quas_Wis:
                case MagicReference.SpellWords.Vas_Rel_Por:
                    break;
                default:
                    throw new Ultima5ReduxException($"Tried to create UtilitySpellSubType with {MagicRef.SpellEnum}");
            }

            return spellResult;
        }
    }
}