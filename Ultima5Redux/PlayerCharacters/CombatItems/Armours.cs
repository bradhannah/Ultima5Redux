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

        // private List<string> _equipmentNames;

        public Armours(CombatItemReferences combatItemReferences, List<byte> gameStateByteArray)
            : base(combatItemReferences, gameStateByteArray)
        {
            // InitializeHelms();
            // InitializeChestArmour();
            // InitializeAmulets();
            // InitializeRings();

            foreach (ArmourReference armourReference in combatItemReferences.AllArmour)
            {
                AddArmour(armourReference);
            }
        }

        // public Armours(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef,
        //     gameStateByteArray)
        // {
        //     // _equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES)
        //     //     .GetAsStringListFromIndexes();
        //
        //     InitializeHelms();
        //     InitializeChestArmour();
        //     InitializeAmulets();
        //     InitializeRings();
        // }

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
            //ChestArmour.ChestArmourEnum chestArmour, DataOvlReference.Equipment equipment)
        {
//                new ChestArmour(chestArmour, equipment, DataOvlRef, GameStateByteArray[(int)equipment]);
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

        // private void AddHelm(ArmourReference armourReference)
        // {
        //     Helm armour = 
        //         //new Helm(helm, equipment, DataOvlRef, GameStateByteArray[(int)equipment]);
        //     Helms.Add(armour);
        //     ItemsFromEquipment.Add(equipment, armour);
        // }
        //
        // private void AddAmulet(Amulet.AmuletEnum amulet, DataOvlReference.Equipment equipment)
        // {
        //     Amulet armour = new Amulet(amulet, equipment, DataOvlRef, GameStateByteArray[(int)equipment]);
        //     Amulets.Add(armour);
        //     ItemsFromEquipment.Add(equipment, armour);
        // }
        //
        // private void AddRing(Ring.RingEnum ring, DataOvlReference.Equipment equipment)
        // {
        //     Ring armour = new Ring(ring, equipment, DataOvlRef, GameStateByteArray[(int)equipment]);
        //     Rings.Add(armour);
        //     ItemsFromEquipment.Add(equipment, armour);
        // }

        // private void InitializeRings()
        // {
        //     AddArmour(Ring.RingEnum.RingInvisibility, DataOvlReference.Equipment.RingInvis);
        //     AddArmour(Ring.RingEnum.RingProtection, DataOvlReference.Equipment.RingProtection);
        //     AddArmour(Ring.RingEnum.RingRegeneration, DataOvlReference.Equipment.RingRegen);
        // }
        //
        // private void InitializeAmulets()
        // {
        //     AddArmour(Amulet.AmuletEnum.AmuletTurning, DataOvlReference.Equipment.Amuletofturning);
        //     AddArmour(Amulet.AmuletEnum.SpikeCollar, DataOvlReference.Equipment.SpikedCollar);
        //     AddArmour(Amulet.AmuletEnum.Ankh, DataOvlReference.Equipment.Ankh);
        // }
        //
        // private void InitializeHelms()
        // {
        //     AddArmour(Helm.HelmEnum.LeatherHelm, DataOvlReference.Equipment.LeatherHelm);
        //     AddArmour(Helm.HelmEnum.ChainCoif, DataOvlReference.Equipment.ChainCoif);
        //     AddArmour(Helm.HelmEnum.IronHelm, DataOvlReference.Equipment.IronHelm);
        //     AddArmour(Helm.HelmEnum.SpikedHelm, DataOvlReference.Equipment.SpikedHelm);
        // }
        //
        // private void InitializeChestArmour()
        // {
        //     AddArmour(ChestArmour.ChestArmourEnum.ClothArmour, DataOvlReference.Equipment.ClothArmour);
        //     AddArmour(ChestArmour.ChestArmourEnum.LeatherArmour, DataOvlReference.Equipment.LeatherArmour);
        //     AddArmour(ChestArmour.ChestArmourEnum.Ringmail, DataOvlReference.Equipment.Ringmail);
        //     AddArmour(ChestArmour.ChestArmourEnum.ScaleMail, DataOvlReference.Equipment.ScaleMail);
        //     AddArmour(ChestArmour.ChestArmourEnum.ChainMail, DataOvlReference.Equipment.ChainMail);
        //     AddArmour(ChestArmour.ChestArmourEnum.PlateMail, DataOvlReference.Equipment.PlateMail);
        //     AddArmour(ChestArmour.ChestArmourEnum.MysticArmour, DataOvlReference.Equipment.MysticArmour);
        // }
    }
}