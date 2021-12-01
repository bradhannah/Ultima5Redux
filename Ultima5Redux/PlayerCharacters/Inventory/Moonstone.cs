using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    [DataContract] public sealed class Moonstone : InventoryItem
    {
        private const int MOONSTONE_SPRITE = 281;

        [DataMember] public MoonPhaseReferences.MoonPhases Phase { get; private set; }

        [IgnoreDataMember] public override string FindDescription => GameReferences.DataOvlRef.StringReferences
            .GetString(DataOvlReference.ThingsIFindStrings.A_STRANGE_ROCK_BANG_N).TrimEnd();

        [IgnoreDataMember] public override bool HideQuantity => true;

        [IgnoreDataMember] public override string InventoryReferenceString => Phase.ToString();

        [IgnoreDataMember] public override string LongName => Utils.AddSpacesBeforeCaps(Phase.ToString());

        [IgnoreDataMember] public override string ShortName => Utils.AddSpacesBeforeCaps(Phase.ToString());

        [IgnoreDataMember] public int MoongateIndex => (int)Phase;

        /// <summary>
        ///     If the moonstone is buried, then it's not in your inventory
        ///     otherwise if it is NOT buried, then it has to be in your inventory
        /// </summary>
        [IgnoreDataMember] public override int Quantity
        {
            get => GameStateReference.State.TheMoongates.IsMoonstoneBuried((int)Phase) ? 0 : 1;
            // filthy hack - if the _moongates is null, then the base constructor has called it and it doesn't matter at that point
            set => GameStateReference.State?.TheMoongates?.SetMoonstoneBuried((int)Phase, value <= 0);
        }

        [JsonConstructor] private Moonstone()
        {
        }

        public Moonstone(MoonPhaseReferences.MoonPhases phase) : base(0, MOONSTONE_SPRITE,
            InventoryReferences.InventoryReferenceType.Item)
        {
            Phase = phase;
        }

        // we will hold onto this enum for later when we assign custom sprites
        //public enum ItemTypeEnum { NewMoon = 0, CrescentWaxing, FirstQuarter, GibbousWaxing, FullMoon, GibbousWaning, LastQuarter, CrescentWaning, NoMoon }
    }
}