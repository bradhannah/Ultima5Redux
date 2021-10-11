using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters.CombatItems
{
    public class Armours : CombatItems<Armours.ArmourTypeEnum, List<Armour>>
    {
        public enum ArmourTypeEnum { Shield, Chest, Helm, Ring, Amulet }

        public readonly List<Amulet> Amulets = new List<Amulet>();
        public readonly List<ChestArmour> ChestArmours = new List<ChestArmour>();
        public readonly List<Helm> Helms = new List<Helm>();
        public readonly List<Ring> Rings = new List<Ring>();

        private List<string> _equipmentNames;

        public Armours(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef,
            gameStateByteArray)
        {
            _equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES)
                .GetAsStringListFromIndexes();

            InitializeHelms();
            InitializeChestArmour();
            InitializeAmulets();
            InitializeRings();
        }

        private Dictionary<DataOvlReference.Equipment, Armour> ItemsFromEquipment { get; } =
            new Dictionary<DataOvlReference.Equipment, Armour>();

        public override Dictionary<ArmourTypeEnum, List<Armour>> Items =>
            new Dictionary<ArmourTypeEnum, List<Armour>>();

        // override to allow for inserting entire lists
        public override IEnumerable<InventoryItem> GenericItemList
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


        private void AddChestArmour(ChestArmour.ChestArmourEnum chestArmour, DataOvlReference.Equipment equipment)
        {
            ChestArmour armour =
                new ChestArmour(chestArmour, equipment, DataOvlRef, GameStateByteArray[(int)equipment]);
            ChestArmours.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        private void AddHelm(Helm.HelmEnum helm, DataOvlReference.Equipment equipment)
        {
            Helm armour = new Helm(helm, equipment, DataOvlRef, GameStateByteArray[(int)equipment]);
            Helms.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        private void AddAmulet(Amulet.AmuletEnum amulet, DataOvlReference.Equipment equipment)
        {
            Amulet armour = new Amulet(amulet, equipment, DataOvlRef, GameStateByteArray[(int)equipment]);
            Amulets.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        private void AddRing(Ring.RingEnum ring, DataOvlReference.Equipment equipment)
        {
            Ring armour = new Ring(ring, equipment, DataOvlRef, GameStateByteArray[(int)equipment]);
            Rings.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        private void InitializeRings()
        {
            AddRing(Ring.RingEnum.RingInvisibility, DataOvlReference.Equipment.RingInvis);
            AddRing(Ring.RingEnum.RingProtection, DataOvlReference.Equipment.RingProtection);
            AddRing(Ring.RingEnum.RingRegeneration, DataOvlReference.Equipment.RingRegen);
        }

        private void InitializeAmulets()
        {
            AddAmulet(Amulet.AmuletEnum.AmuletTurning, DataOvlReference.Equipment.Amuletofturning);
            AddAmulet(Amulet.AmuletEnum.SpikeCollar, DataOvlReference.Equipment.SpikedCollar);
            AddAmulet(Amulet.AmuletEnum.Ankh, DataOvlReference.Equipment.Ankh);
        }

        private void InitializeHelms()
        {
            AddHelm(Helm.HelmEnum.LeatherHelm, DataOvlReference.Equipment.LeatherHelm);
            AddHelm(Helm.HelmEnum.ChainCoif, DataOvlReference.Equipment.ChainCoif);
            AddHelm(Helm.HelmEnum.IronHelm, DataOvlReference.Equipment.IronHelm);
            AddHelm(Helm.HelmEnum.SpikedHelm, DataOvlReference.Equipment.SpikedHelm);
        }

        private void InitializeChestArmour()
        {
            AddChestArmour(ChestArmour.ChestArmourEnum.ClothArmour, DataOvlReference.Equipment.ClothArmour);
            AddChestArmour(ChestArmour.ChestArmourEnum.LeatherArmour, DataOvlReference.Equipment.LeatherArmour);
            AddChestArmour(ChestArmour.ChestArmourEnum.Ringmail, DataOvlReference.Equipment.Ringmail);
            AddChestArmour(ChestArmour.ChestArmourEnum.ScaleMail, DataOvlReference.Equipment.ScaleMail);
            AddChestArmour(ChestArmour.ChestArmourEnum.ChainMail, DataOvlReference.Equipment.ChainMail);
            AddChestArmour(ChestArmour.ChestArmourEnum.PlateMail, DataOvlReference.Equipment.PlateMail);
            AddChestArmour(ChestArmour.ChestArmourEnum.MysticArmour, DataOvlReference.Equipment.MysticArmour);
        }
    }
}