using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    [DataContract] public sealed class WeaponReference : CombatItemReference
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpecificWeaponType
        {
            BareHands = -2, SmallShield = 0x21E, LargeShield = 0x21F, SpikedShield = 0x220, MagicShield = 0x221,
            JewelShield = 0x222, Dagger = 0x22a, Sling, Club, FlamingOil, MainGauche, Spear, ThrowingAxe, ShortSword,
            Mace, MorningStar, Bow, Arrows, Crossbow, Quarrels, LongSword, TwoHHammer, TwoHAxe, TwoHSword, Halberd,
            SwordofChaos, MagicBow, SilverSword, MagicAxe, GlassSword, JeweledSword, MysticSword
        }

        [DataMember] public SpecificWeaponType WeaponType { get; private set; }

        [IgnoreDataMember] public override bool CanSell => BasePrice > 0 || IsAmmo;

        [JsonConstructor] private WeaponReference()
        {
        }

        public WeaponReference(DataOvlReference dataOvlReference, InventoryReference inventoryReference) : base(
            dataOvlReference, inventoryReference) =>
            WeaponType =
                (SpecificWeaponType)Enum.Parse(typeof(SpecificWeaponType),
                    inventoryReference.GetAsEquipment().ToString());
    }
}