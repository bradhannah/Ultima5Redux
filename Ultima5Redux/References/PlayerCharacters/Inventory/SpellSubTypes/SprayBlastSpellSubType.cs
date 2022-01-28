namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class SprayBlastSpellSubType : SpellSubType
    {
        public SprayBlastSpellSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        private enum SprayType { Poison, Energy, Fire }

        public override SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellResult spellResult = new(MagicRef.SpellEnum);

            DoSetupAndStateCheck(state, spellResult);

            SprayType sprayType = MagicRef.SpellEnum switch
            {
                MagicReference.SpellWords.In_Vas_Grav_Corp => SprayType.Poison,
                MagicReference.SpellWords.In_Nox_Hur => SprayType.Energy,
                MagicReference.SpellWords.In_Flam_Hur => SprayType.Fire,
                _ => throw new Ultima5ReduxException(
                    $"Tried to create SprayBlastSpellSubType with {MagicRef.SpellEnum}")
            };

            CastSpray(sprayType);

            return spellResult;
        }

        private void CastSpray(SprayType sprayType)
        {
            // placeholder
        }
    }
}