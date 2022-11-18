using System;
using System.ComponentModel;

namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public abstract class SpellSubType
    {
        public MagicReference MagicRef { get; }

        protected SpellSubType(MagicReference magicRef) => MagicRef = magicRef;

        public abstract SpellResult CastSpell(GameState state, SpellCastingDetails details);

        public bool IsCastablePresently(GameState state)
        {
            bool bResult = MagicRef.TimePermitted switch
            {
                MagicReference.SpecificTimePermitted.Peace => !state.TheVirtualMap.IsCombatMap,
                MagicReference.SpecificTimePermitted.Combat => state.TheVirtualMap.IsCombatMap,
                MagicReference.SpecificTimePermitted.Anytime => true,
                MagicReference.SpecificTimePermitted.Combat_Dungeon => state.TheVirtualMap.IsCombatMap || true,
                MagicReference.SpecificTimePermitted.Dungeon => false,
                MagicReference.SpecificTimePermitted.Never => false,
                _ => throw new InvalidEnumArgumentException(((int)MagicRef.TimePermitted).ToString())
            };

            return bResult;
        }

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

            bool bResult = IsCastablePresently(state);

            if (bResult)
            {
                spellResult.OutputStringBuilder.Append(GameReferences.Instance.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.ExclaimStrings.SUCCESS_BANG_N).Trim());
                spellResult.Status = SpellResult.SpellResultStatus.Success;
            }
            else
            {
                spellResult.OutputStringBuilder.Append(GameReferences.Instance.DataOvlRef.StringReferences
                    .GetString(DataOvlReference.ExclaimStrings.FAILED_BANG_N).Trim());
                spellResult.Status = SpellResult.SpellResultStatus.Failure;
            }

            return bResult;
        }
    }
}