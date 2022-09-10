using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public class DrankPotion : TurnResult
    {
        public Potion.PotionColor PotionColor { get; }
        public MagicReference.SpellWords SpellWord { get; }

        public DrankPotion(Potion.PotionColor potionColor, MagicReference.SpellWords spellWord) : base(TurnResultType
            .ActionUseDrankPotion)
        {
            PotionColor = potionColor;
            SpellWord = spellWord;
        }
    }
}