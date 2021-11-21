using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public sealed class WeaponReference : CombatItemReference
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum WeaponTypeEnum
        {
            BareHands = -2, SmallShield = 0x21E, LargeShield = 0x21F, SpikedShield = 0x220, MagicShield = 0x221,
            JewelShield = 0x222, Dagger = 0x22a, Sling, Club, FlamingOil, MainGauche, Spear, ThrowingAxe, ShortSword,
            Mace, MorningStar, Bow, Arrows, Crossbow, Quarrels, LongSword, TwoHHammer, TwoHAxe, TwoHSword, Halberd,
            SwordofChaos, MagicBow, SilverSword, MagicAxe, GlassSword, JeweledSword, MysticSword
        }

        [IgnoreDataMember] public override bool CanSell => BasePrice > 0 || IsAmmo;

        [DataMember] public WeaponTypeEnum WeaponType { get; }

        [JsonConstructor] private WeaponReference()
        {
        }
        

        public WeaponReference(DataOvlReference dataOvlReference, InventoryReference inventoryReference) : base(
            dataOvlReference, inventoryReference)
        {
            WeaponType =
                (WeaponTypeEnum)Enum.Parse(typeof(WeaponTypeEnum), inventoryReference.GetAsEquipment().ToString());
        }
    }
}