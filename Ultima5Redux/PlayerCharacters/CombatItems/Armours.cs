using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public class Armours : CombatItems<Armours.ArmourTypeEnum, List<Armour>>
    {
        [JsonConverter(typeof(StringEnumConverter))] public enum ArmourTypeEnum { Shield, Chest, Helm, Ring, Amulet }

        [IgnoreDataMember] private Dictionary<DataOvlReference.Equipment, Armour> ItemsFromEquipment { get; } =
            new Dictionary<DataOvlReference.Equipment, Armour>();

        // override to allow for inserting entire lists
        [IgnoreDataMember] public override IEnumerable<InventoryItem> GenericItemList
        {
            get
            {
                List<InventoryItem> itemList = Helms.Cast<InventoryItem>().ToList();
                itemList.AddRange(ChestArmours);
                itemList.AddRange(Amulets);
                itemList.AddRange(Rings);
                return itemList;
            }
        }

        [IgnoreDataMember] public override Dictionary<ArmourTypeEnum, List<Armour>> Items =>
            new Dictionary<ArmourTypeEnum, List<Armour>>();

        [DataMember] public readonly List<Amulet> Amulets = new List<Amulet>();
        [DataMember] public readonly List<ChestArmour> ChestArmours = new List<ChestArmour>();
        [DataMember] public readonly List<Helm> Helms = new List<Helm>();
        [DataMember] public readonly List<Ring> Rings = new List<Ring>();

        [JsonConstructor] public Armours()
        {
        }

        public Armours(List<byte> gameStateByteArray) : base(gameStateByteArray)
        {
            foreach (ArmourReference armourReference in GameReferences.CombatItemRefs.AllArmour)
            {
                AddArmour(armourReference);
            }
        }

        private void AddArmour(ArmourReference armourReference)
        {
            Armour armour;
            switch (armourReference.TheArmourType)
            {
                case ArmourReference.ArmourType.Amulet:
                    Amulet amulet = new Amulet(armourReference,
                        GameStateByteArray[(int)armourReference.SpecificEquipment]);
                    armour = amulet;
                    Amulets.Add(amulet);
                    break;
                case ArmourReference.ArmourType.ChestArmour:
                    ChestArmour chestArmour = new ChestArmour(armourReference,
                        GameStateByteArray[(int)armourReference.SpecificEquipment]);
                    armour = chestArmour;
                    ChestArmours.Add(chestArmour);
                    break;
                case ArmourReference.ArmourType.Helm:
                    Helm helm = new Helm(armourReference, GameStateByteArray[(int)armourReference.SpecificEquipment]);
                    armour = helm;
                    Helms.Add(helm);
                    break;
                case ArmourReference.ArmourType.Ring:
                    Ring ring = new Ring(armourReference, GameStateByteArray[(int)armourReference.SpecificEquipment]);
                    armour = ring;
                    Rings.Add(ring);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ItemsFromEquipment.Add(armour.SpecificEquipment, armour);
        }

        public Armour GetArmourFromEquipment(DataOvlReference.Equipment equipment)
        {
            if (equipment == DataOvlReference.Equipment.Nothing) return null;
            return ItemsFromEquipment.ContainsKey(equipment) ? ItemsFromEquipment[equipment] : null;
        }
    }
}