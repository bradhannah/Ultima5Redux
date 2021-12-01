using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    [DataContract] public class Armours : CombatItems<ArmourReference.ArmourType, List<Armour>>
    {

        [DataMember] public List<Amulet> Amulets { get; private set; } = new List<Amulet>();
        [DataMember] public List<ChestArmour> ChestArmours { get; private set; } = new List<ChestArmour>();
        [DataMember] public List<Helm> Helms { get; private set; } = new List<Helm>();
        [DataMember] public List<Ring> Rings { get; private set; } = new List<Ring>();

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

        [IgnoreDataMember]
        public override Dictionary<ArmourReference.ArmourType, List<Armour>> Items { get; internal set; } =
            new Dictionary<ArmourReference.ArmourType, List<Armour>>();

        private Dictionary<ArmourReference.ArmourType, List<Armour>> _savedItems;

        [JsonConstructor] private Armours()
        {
        }

        public Armours(ImportedGameState importedGameState)
        {
            foreach (ArmourReference armourReference in GameReferences.CombatItemRefs.AllArmour)
            {
                AddArmour(armourReference, importedGameState.GetEquipmentQuantity(armourReference.SpecificEquipment));
            }
        }

        [OnDeserialized] private void PostDeserialize(StreamingContext context)
        {
            foreach (Armour armour in AllCombatItems.OfType<Armour>())
            {
                ArmourReference.ArmourType armourType = armour.ArmourRef.TheArmourType;
                ItemsFromEquipment.Add(armour.SpecificEquipment, armour);
                if (!Items.ContainsKey(armourType)) Items.Add(armourType, new List<Armour>());
                Items[armourType].Add(armour);
            }
        }

        [OnSerialized] private void PostSerialize(StreamingContext context)
        {
            Items = _savedItems;
        }

        [OnSerializing] private void PreSerialize(StreamingContext context)
        {
            _savedItems = Items;
            Items = new Dictionary<ArmourReference.ArmourType, List<Armour>>();
        }

        private void AddArmour(ArmourReference armourReference, int nQuantity)
        {
            Armour armour;
            switch (armourReference.TheArmourType)
            {
                case ArmourReference.ArmourType.Amulet:
                    Amulet amulet = new Amulet(armourReference, nQuantity);
                    //GameStateByteArray[(int)armourReference.SpecificEquipment]
                    armour = amulet;
                    Amulets.Add(amulet);
                    break;
                case ArmourReference.ArmourType.ChestArmour:
                    ChestArmour chestArmour = new ChestArmour(armourReference, nQuantity);
                    //GameStateByteArray[(int)armourReference.SpecificEquipment]);
                    armour = chestArmour;
                    ChestArmours.Add(chestArmour);
                    break;
                case ArmourReference.ArmourType.Helm:
                    Helm helm = new Helm(armourReference, nQuantity);
                    //GameStateByteArray[(int)armourReference.SpecificEquipment]);
                    armour = helm;
                    Helms.Add(helm);
                    break;
                case ArmourReference.ArmourType.Ring:
                    Ring ring = new Ring(armourReference, nQuantity);
                    //GameStateByteArray[(int)armourReference.SpecificEquipment]);
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