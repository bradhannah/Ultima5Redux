using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Potions : InventoryItems<Potion.PotionColor, Potion>
    {
        public Potions(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
            AddPotion(Potion.PotionColor.Blue, DataOvlReference.PotionsStrings.BLUE, dataOvlRef);
            AddPotion(Potion.PotionColor.Yellow, DataOvlReference.PotionsStrings.YELLOW, dataOvlRef);
            AddPotion(Potion.PotionColor.Red, DataOvlReference.PotionsStrings.RED, dataOvlRef);
            AddPotion(Potion.PotionColor.Green, DataOvlReference.PotionsStrings.GREEN, dataOvlRef);
            AddPotion(Potion.PotionColor.Orange, DataOvlReference.PotionsStrings.ORANGE, dataOvlRef);
            AddPotion(Potion.PotionColor.Purple, DataOvlReference.PotionsStrings.PURPLE, dataOvlRef);
            AddPotion(Potion.PotionColor.Black, DataOvlReference.PotionsStrings.BLACK, dataOvlRef);
            AddPotion(Potion.PotionColor.White, DataOvlReference.PotionsStrings.WHITE, dataOvlRef);
        }

        public override Dictionary<Potion.PotionColor, Potion> Items { get; } =
            new Dictionary<Potion.PotionColor, Potion>(8);

        private void AddPotion(Potion.PotionColor color, DataOvlReference.PotionsStrings potStr, DataOvlReference dataOvlReference)
        {
            Items[color] = new Potion(color, GameStateByteArray[(int)color],
                dataOvlReference.StringReferences.GetString(potStr),
                dataOvlReference.StringReferences.GetString(potStr));
        }
    }
}