using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public class Potions : InventoryItems<Potion.PotionColor, Potion>
    {
        [DataMember] public override Dictionary<Potion.PotionColor, Potion> Items { get; internal set; } =
            new Dictionary<Potion.PotionColor, Potion>(8);

        [JsonConstructor] private Potions()
        {
        }

        public Potions(ImportedGameState importedGameState)
        {
            void addPotionLegacy(Potion.PotionColor color) =>
                AddPotion(color, importedGameState.GetPotionQuantity(color));

            addPotionLegacy(Potion.PotionColor.Blue);
            addPotionLegacy(Potion.PotionColor.Yellow);
            addPotionLegacy(Potion.PotionColor.Red);
            addPotionLegacy(Potion.PotionColor.Green);
            addPotionLegacy(Potion.PotionColor.Orange);
            addPotionLegacy(Potion.PotionColor.Purple);
            addPotionLegacy(Potion.PotionColor.Black);
            addPotionLegacy(Potion.PotionColor.White);
        }

        private void AddPotion(Potion.PotionColor color, int nQuantity)
        {
            Items[color] = new Potion(color, nQuantity);
        }
    }
}