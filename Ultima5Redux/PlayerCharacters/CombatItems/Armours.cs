using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization;
using System.Threading;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract]
    public class Armours : CombatItems<Armours.ArmourTypeEnum, List<Armour>>
    {
        public enum ArmourTypeEnum { Shield, Chest, Helm, Ring, Amulet }

        public readonly List<Amulet> Amulets = new List<Amulet>();
        public readonly List<ChestArmour> ChestArmours = new List<ChestArmour>();
        public readonly List<Helm> Helms = new List<Helm>();
        public readonly List<Ring> Rings = new List<Ring>();

        public Armours(CombatItemReferences combatItemReferences, List<byte> gameStateByteArray)
            : base(combatItemReferences, gameStateByteArray)
        {
            foreach (ArmourReference armourReference in combatItemReferences.AllArmour)
            {
                AddArmour(armourReference);
            }
        }

        [DataMember] private Dictionary<DataOvlReference.Equipment, Armour> ItemsFromEquipment { get; } =
            new Dictionary<DataOvlReference.Equipment, Armour>();

        [IgnoreDataMember] public override Dictionary<ArmourTypeEnum, List<Armour>> Items =>
            new Dictionary<ArmourTypeEnum, List<Armour>>();

        // [OnDeserialized] public void OnDeserialized(StreamingContext context)
        // {
        //     
        // }
        
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

        public Armour GetArmourFromEquipment(DataOvlReference.Equipment equipment)
        {
            if (equipment == DataOvlReference.Equipment.Nothing) return null;
            return ItemsFromEquipment.ContainsKey(equipment) ? ItemsFromEquipment[equipment] : null;
        }

        private void AddArmour(ArmourReference armourReference)
        {
            Armour armour;
            switch (armourReference.TheArmourType)
            {
                case ArmourReference.ArmourType.Amulet:
                    Amulet amulet = new Amulet(armourReference, GameStateByteArray[(int)armourReference.SpecificEquipment]);
                    armour = amulet;
                    Amulets.Add(amulet);
                    break;
                case ArmourReference.ArmourType.ChestArmour:
                    ChestArmour chestArmour = new ChestArmour(armourReference, GameStateByteArray[(int)armourReference.SpecificEquipment]);
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
    }
}