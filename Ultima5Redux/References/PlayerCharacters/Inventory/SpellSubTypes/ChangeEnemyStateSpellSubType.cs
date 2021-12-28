namespace Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes
{
    public class ChangeEnemyStateSpellSubType : SpellSubType
    {
        public ChangeEnemyStateSpellSubType(MagicReference magicRef) : base(magicRef)
        {
        }

        public override SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellResult spellResult = new(MagicRef.SpellEnum);

            DoSetupAndStateCheck(state, spellResult);

            switch (MagicRef.SpellEnum)
            {
                case MagicReference.SpellWords.In_Zu:
                case MagicReference.SpellWords.Quas_An_Wis:
                case MagicReference.SpellWords.An_Xen_Ex:
                case MagicReference.SpellWords.In_Quas_Corp:
                    break;
                default:
                    throw new Ultima5ReduxException(
                        $"Tried to create ChangeEnemyStateSpellSubType with {MagicRef.SpellEnum}");
            }

            return spellResult;
        }
    }
}