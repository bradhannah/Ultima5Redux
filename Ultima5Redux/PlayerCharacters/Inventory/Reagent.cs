using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.Inventory
{
    /// <summary>
    ///     Instance represents a single reagent type
    /// </summary>
    public class Reagent : InventoryItem
    {
        //0x2AA 1 0-99 Sulfur Ash
        //0x2AB 1 0-99 Ginseng
        //0x2AC 1 0-99 Garlic
        //0x2AD 1 0-99 Spider Silk
        //0x2AE 1 0-99 Blood Moss
        //0x2AF 1 0-99 Black Pearl
        //0x2B0 1 0-99 Nightshade
        //0x2B1 1 0-99 Mandrake Root
        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpecificReagentType
        {
            SulfurAsh = 0x2AA, Ginseng = 0x2AB, Garlic = 0x2AC, SpiderSilk = 0x2AD, BloodMoss = 0x2AE,
            BlackPearl = 0x2AF, NightShade = 0x2B0, MandrakeRoot = 0x2B1
        }

        private const int REAGENT_SPRITE = 259;

        [DataMember]
        public override int Quantity
        {
            get => base.Quantity;
            set => base.Quantity = value > MAX_INVENTORY_ITEM_QUANTITY ? MAX_INVENTORY_ITEM_QUANTITY : value;
        }

        [DataMember] public SpecificReagentType ReagentType { get; private set; }

        [IgnoreDataMember] public override int BasePrice => 0;
        [IgnoreDataMember] public override string FindDescription => InvRef.FriendlyItemName;

        [IgnoreDataMember] public override bool HideQuantity => false;

        [IgnoreDataMember] public override string InventoryReferenceString => ReagentType.ToString();
        [IgnoreDataMember] public override bool IsSellable => false;

        /// <summary>
        ///     Standard index/order of reagents in data files
        /// </summary>
        [IgnoreDataMember]
        public int ReagentIndex => (int)ReagentType - (int)SpecificReagentType.SulfurAsh;

        [JsonConstructor] private Reagent()
        {
        }

        /// <summary>
        ///     Create a reagent
        /// </summary>
        /// <param name="specificReagentTypepe of reagent</param>
        /// <param name="quantity">how many the party has</param>
        public Reagent(SpecificReagentType specificReagentType, int quantity) : base(quantity, REAGENT_SPRITE,
            InventoryReferences.InventoryReferenceType.Reagent)
        {
            ReagentType = specificReagentType;
        }

        /// <summary>
        ///     Get the correct price adjust for the specific location and
        /// </summary>
        /// <param name="records"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public override int GetAdjustedBuyPrice(PlayerCharacterRecords records,
            SmallMapReferences.SingleMapReference.Location location)
        {
            if (!GameReferences.ReagentReferences.IsReagentSoldAtLocation(location, ReagentType))
                throw new Ultima5ReduxException("Requested reagent " + LongName + " from " + location +
                                                " which is not sold here");

            // A big thank you to Markus Brenner (@minstrel_dragon) for digging in and figuring out the Karma calculation
            // price = Base Price * (1 + (100 - Karma) / 100)
            int nAdjustedPrice = GameReferences.ReagentReferences.GetPriceAndQuantity(location, ReagentType).Price *
                                 (1 + (100 - GameStateReference.State.Karma) / 100);
            return nAdjustedPrice;
        }

        /// <summary>
        ///     Get bundle quantity based on location
        ///     Different merchants sell in different quantities
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="Ultima5ReduxException"></exception>
        public override int GetQuantityForSale(SmallMapReferences.SingleMapReference.Location location)
        {
            if (!GameReferences.ReagentReferences.IsReagentSoldAtLocation(location, ReagentType))
                throw new Ultima5ReduxException("Requested reagent " + LongName + " from " + location +
                                                " which is not sold here");

            return GameReferences.ReagentReferences.GetPriceAndQuantity(location, ReagentType).Quantity;
        }

        /// <summary>
        ///     Does a particular location sell a particular reagent?
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public bool IsReagentForSale(SmallMapReferences.SingleMapReference.Location location)
        {
            return GameReferences.ReagentReferences.IsReagentSoldAtLocation(location, ReagentType);
        }
    }
}