using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Potions : InventoryItems<Potion.PotionColor, Potion>
    {
        [DataMember] public override Dictionary<Potion.PotionColor, Potion> Items { get; } =
            new Dictionary<Potion.PotionColor, Potion>(8);

        [JsonConstructor] private Potions()
        {
        }

        public Potions(List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
            AddPotion(Potion.PotionColor.Blue);
            AddPotion(Potion.PotionColor.Yellow);
            AddPotion(Potion.PotionColor.Red);
            AddPotion(Potion.PotionColor.Green);
            AddPotion(Potion.PotionColor.Orange);
            AddPotion(Potion.PotionColor.Purple);
            AddPotion(Potion.PotionColor.Black);
            AddPotion(Potion.PotionColor.White);
        }

        private void AddPotion(Potion.PotionColor color)
        {
            Items[color] = new Potion(color, GameStateByteArray[(int)color]);
        }
    }
}