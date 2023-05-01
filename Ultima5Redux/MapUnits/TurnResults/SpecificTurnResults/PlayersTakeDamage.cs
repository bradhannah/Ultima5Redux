using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class PlayersTakeDamage : TurnResult, IDamageAmount, ISinglePlayerCharacterAffected
    {
        public enum DamageType
        {
            RoughSeas, Cactus, Waterfall, Fall, OverTimePoisoned, OverTimeBurning, Acid, Arrow, CannonBall, Fire
        }

        public PlayersTakeDamage(DamageType damageType, PlayerCharacterRecord record, int damageAmount) :
            base(TurnResultType.PlayerTakesDamage)
        {
            DamageAmount = damageAmount;
            Record = record;
            TheDamageType = damageType;
        }

        public DamageType TheDamageType { get; }
        public int DamageAmount { get; }
        public PlayerCharacterRecord Record { get; }
    }
}