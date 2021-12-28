using System;

namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public abstract class SpellSubType
    {
        protected SpellSubType(MagicReference magicRef)
        {
            MagicRef = magicRef;
        }

        public MagicReference MagicRef { get; }

        public abstract SpellResult CastSpell(GameState state, SpellCastingDetails details);

        /// <summary>
        ///     Gets the spell set up and checks to see if it is valid in the current context
        /// </summary>
        /// <param name="state">current game state</param>
        /// <param name="spellResult">the current spell result object keeping tracking of status</param>
        /// <returns>true if it is castable, false if it is not</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        protected bool DoSetupAndStateCheck(GameState state, SpellResult spellResult)
        {
            spellResult.OutputStringBuilder.Append($"Casting {MagicRef.Spell}\n");

            bool bResult = MagicRef.TimePermitted switch
            {
                MagicReference.TimePermittedEnum.Peace => !state.TheVirtualMap.IsCombatMap,
                MagicReference.TimePermittedEnum.Combat => state.TheVirtualMap.IsCombatMap,
                MagicReference.TimePermittedEnum.Anytime => true,
                MagicReference.TimePermittedEnum.Combat_Dungeon => state.TheVirtualMap.IsCombatMap || true,
                MagicReference.TimePermittedEnum.Dungeon => false,
                MagicReference.TimePermittedEnum.Never => false,
                _ => throw new ArgumentOutOfRangeException()
            };

            spellResult.OutputStringBuilder.Append(GameReferences.DataOvlRef.StringReferences
                .GetString(DataOvlReference.ExclaimStrings.FAILED_BANG_N).Trim());
            spellResult.Status =
                bResult ? SpellResult.SpellResultStatus.Success : SpellResult.SpellResultStatus.Failure;

            return bResult;
        }
    }
}