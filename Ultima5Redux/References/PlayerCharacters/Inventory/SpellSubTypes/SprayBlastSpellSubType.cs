using System.Diagnostics.CodeAnalysis;

namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class SprayBlastSpellSubType : SpellSubType
    {
        private enum SprayType
        {
            Poison,
            Energy,
            Fire
        }

        public SprayBlastSpellSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private void CastSpray(SprayType sprayType)
        {
            // placeholder
        }

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
    }
}