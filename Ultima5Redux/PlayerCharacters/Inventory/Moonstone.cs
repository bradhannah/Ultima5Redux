using System.Linq;
using Ultima5Redux.DayNightMoon;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    public class Moonstone : InventoryItem
    {
        private const int MOONSTONE_SPRITE = 281;
        private readonly Moongates _moongates;

        public Moonstone(MoonPhaseReferences.MoonPhases phase, string longName, string shortName,
            string findDescription, Moongates moongates, InventoryReference invRef)
            : base(0, longName, shortName, findDescription, MOONSTONE_SPRITE)
        {
            Phase = phase;
            _moongates = moongates;
            InvRef = invRef;
        }

        public MoonPhaseReferences.MoonPhases Phase { get; }

        public int MoongateIndex => (int) Phase;

        public override string LongName => Utils.AddSpacesBeforeCaps(Phase.ToString());
        public override string ShortName => Utils.AddSpacesBeforeCaps(Phase.ToString());

        public override string InventoryReferenceString => Phase.ToString();
        
        /// <summary>
        ///     If the moonstone is buried, then it's not in your inventory
        ///     otherwise if it is NOT buried, then it has to be in your inventory
        /// </summary>
        public override int Quantity
        {
            get => _moongates.IsMoonstoneBuried((int) Phase) ? 0 : 1;
            // filthy hack - if the _moongates is null, then the base constructor has called it and it doesn't matter at that point
            set => _moongates?.SetMoonstoneBuried((int) Phase, value <= 0);
        }

        public override bool HideQuantity => true;

        // we will hold onto this enum for later when we assign custom sprites
        //public enum ItemTypeEnum { NewMoon = 0, CrescentWaxing, FirstQuarter, GibbousWaxing, FullMoon, GibbousWaning, LastQuarter, CrescentWaning, NoMoon }
    }
}