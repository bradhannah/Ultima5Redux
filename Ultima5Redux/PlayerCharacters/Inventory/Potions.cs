using System.Collections.Generic;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Potions : InventoryItems<Potion.PotionColor, Potion>
    {
        public override Dictionary<Potion.PotionColor, Potion> Items { get; } = new Dictionary<Potion.PotionColor, Potion>(8);

        private void AddPotion(Potion.PotionColor color, DataOvlReference.PotionsStrings potStr)
        {
            Items[color] = new Potion(color, GameStateByteArray[(int)color],
                DataOvlRef.StringReferences.GetString(potStr),
                DataOvlRef.StringReferences.GetString(potStr));
        }

        public Potions(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base (dataOvlRef, gameStateByteArray)
        {
            AddPotion(Potion.PotionColor.Blue, DataOvlReference.PotionsStrings.BLUE);
            AddPotion(Potion.PotionColor.Yellow, DataOvlReference.PotionsStrings.YELLOW);
            AddPotion(Potion.PotionColor.Red, DataOvlReference.PotionsStrings.RED);
            AddPotion(Potion.PotionColor.Green, DataOvlReference.PotionsStrings.GREEN);
            AddPotion(Potion.PotionColor.Orange, DataOvlReference.PotionsStrings.ORANGE);
            AddPotion(Potion.PotionColor.Purple, DataOvlReference.PotionsStrings.PURPLE);
            AddPotion(Potion.PotionColor.Black, DataOvlReference.PotionsStrings.BLACK);
            AddPotion(Potion.PotionColor.White, DataOvlReference.PotionsStrings.WHITE);
        }
    }
}