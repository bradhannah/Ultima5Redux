using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.Data;

namespace Ultima5Redux.PlayerCharacters
{
    public class Armours : CombatItems<Armours.ArmourTypeEnum, List<Armour>>
    {
        //public List<Shield> Shields = new List<Shield>();
        public readonly List<ChestArmour> ChestArmours = new List<ChestArmour>();
        public readonly List<Helm> Helms = new List<Helm>();
        public readonly List<Amulet> Amulets = new List<Amulet>();
        public readonly List<Ring> Rings = new List<Ring>();
      
        private List<string> _equipmentNames;

        private Dictionary<DataOvlReference.Equipment, Armour> ItemsFromEquipment { get; } = new Dictionary<DataOvlReference.Equipment, Armour>();
        public Armour GetArmourFromEquipment(DataOvlReference.Equipment equipment)
        {
            if (equipment == DataOvlReference.Equipment.Nothing)
            {
                return null;
            }
            if (!ItemsFromEquipment.ContainsKey(equipment))
            {
                return null;
            }
            return ItemsFromEquipment[equipment];
        }

        public enum ArmourTypeEnum { Shield, Chest, Helm, Ring, Amulet }
        public Armours(DataOvlReference dataOvlRef, List<byte> gameStateByteArray) : base(dataOvlRef, gameStateByteArray)
        {
            _equipmentNames = dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.EQUIP_INDEXES).GetAsStringListFromIndexes();

            InitializeHelms();
            //InitializeShields();
            InitializeChestArmour();
            InitializeAmulets();
            InitializeRings();
        }

        // override to allow for inserting entire lists
        public override IEnumerable<InventoryItem> GenericItemList
        {
            get
            {
                List<InventoryItem> itemList = Helms.Cast<InventoryItem>().ToList();
                itemList.AddRange(ChestArmours.Cast<InventoryItem>());
                itemList.AddRange(Amulets.Cast<InventoryItem>());
                itemList.AddRange(Rings.Cast<InventoryItem>());
                return itemList;
                //foreach (Shield shield in Shields) { itemList.Add(shield); }
                //foreach (Amulet amulet in Amulets) { itemList.Add(amulet); }
            }
        }

        private void AddChestArmour(ChestArmour.ChestArmourEnum chestArmour, DataOvlReference.Equipment equipment)
        {
            ChestArmour armour = new ChestArmour(chestArmour, equipment, DataOvlRef, GameStateByteArray);
            ChestArmours.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        //private void AddShield(Shield.ShieldTypeEnum shield, DataOvlReference.EQUIPMENT equipment)
        //{
        //    Shields.Add(new Shield(shield, equipment, dataOvlRef, gameStateByteArray));
        //}

        private void AddHelm(Helm.HelmEnum helm, DataOvlReference.Equipment equipment)
        {
            Helm armour = new Helm(helm, equipment, DataOvlRef, GameStateByteArray);
            Helms.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        private void AddAmulet(Amulet.AmuletEnum amulet, DataOvlReference.Equipment equipment)
        {
            Amulet armour = new Amulet(amulet, equipment, DataOvlRef, GameStateByteArray);
            Amulets.Add(armour);
            ItemsFromEquipment.Add(equipment, armour);
        }

        private void AddRing(Ring.RingEnum ring, DataOvlReference.Equipment equipment)
        {
            Ring armour = new Ring(ring, equipment, DataOvlRef, GameStateByteArray);
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

        //private void InitializeShields()
        //{
        //    AddShield(Shield.ShieldTypeEnum.SmallShield, DataOvlReference.EQUIPMENT.SmallShield);
        //    AddShield(Shield.ShieldTypeEnum.LargeShield, DataOvlReference.EQUIPMENT.LargeShield);
        //    AddShield(Shield.ShieldTypeEnum.SpikedShield, DataOvlReference.EQUIPMENT.SpikedShield);
        //    AddShield(Shield.ShieldTypeEnum.MagicShield, DataOvlReference.EQUIPMENT.MagicShield);
        //    AddShield(Shield.ShieldTypeEnum.JewelShield, DataOvlReference.EQUIPMENT.JewelShield);
        //}

        public override Dictionary<ArmourTypeEnum, List<Armour>> Items => new Dictionary<ArmourTypeEnum, List<Armour>>();
    }
}