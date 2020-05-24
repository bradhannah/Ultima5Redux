namespace Ultima5Redux.PlayerCharacters
{
    public class Reagent : InventoryItem
    {
        private const int REAGENT_SPRITE = 259;
        public Reagent(ReagentTypeEnum reagentType, int quantity, string longName, string shortName) : base(quantity, longName, shortName, REAGENT_SPRITE)
        {
            ReagentType = reagentType;
        }

        public override bool HideQuantity => false;
        public override bool IsSellable => false;
        public override int BasePrice => 0;
        public ReagentTypeEnum ReagentType { get; }

        //0x2AA 1 0-99 Sulfur Ash
        //0x2AB 1 0-99 Ginseng
        //0x2AC 1 0-99 Garlic
        //0x2AD 1 0-99 Spider Silk
        //0x2AE 1 0-99 Blood Moss
        //0x2AF 1 0-99 Black Pearl
        //0x2B0 1 0-99 Nightshade
        //0x2B1 1 0-99 Mandrake Root
        public enum ReagentTypeEnum { SulfurAsh = 0x2AA , Ginseng = 0x2AB, Garlic = 0x2AC, SpiderSilk = 0x2AD, BloodMoss = 0x2AE, BlackPearl = 0x2AF, 
            NightShade = 0x2B0, MandrakeRoot = 0x2B1 };
    }
}
