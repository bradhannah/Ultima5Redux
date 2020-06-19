using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapCharacters
{
    public class ShoppeKeeperReferences
    {
        private readonly Dictionary<string, ShoppeKeeperReference> _shoppeKeepers = new Dictionary<string, ShoppeKeeperReference>();
        private readonly Dictionary<int, ShoppeKeeperReference> _shoppeKeepersByIndex;

        private readonly Dictionary<SmallMapReferences.SingleMapReference.Location,
                Dictionary<NonPlayerCharacterReference.NPCDialogTypeEnum, ShoppeKeeperReference>>
            _shoppeKeepersByLocationAndType = new Dictionary<SmallMapReferences.SingleMapReference.Location, Dictionary<NonPlayerCharacterReference.NPCDialogTypeEnum, ShoppeKeeperReference>>();
        private readonly DataOvlReference _dataOvlReference;

        public ShoppeKeeperReferences(DataOvlReference dataOvlReference, NonPlayerCharacterReferences npcReferences)
        {
            _dataOvlReference = dataOvlReference;
            
            // we load a list of all shoppe keeper locations which we unfortunately had to map 
            // ourselves because it appears most shoppe keeper data is in the code (OVL) files
            _shoppeKeepersByIndex = ShoppeKeeperReferences.LoadShoppeKeepersByIndex(_dataOvlReference);
            
            List<string> shoppeNames = dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.STORE_NAMES)
                .GetChunkAsStringList().Strs;
            List<string> shoppeKeeperNames = dataOvlReference
                .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_NAMES).GetChunkAsStringList().Strs;

            // Dammit Simplon, how the hell did you sneak into the Shoppe Keeper array!?!
            shoppeKeeperNames.Remove(@"Simplon");
            
            Debug.Assert(shoppeNames.Count == shoppeKeeperNames.Count, "Must be same number of shoppe keepers to shoppes");

            for (int i = 0; i < shoppeNames.Count; i++)
            {
                // create a new shoppe keeper object then add it to the list
                ShoppeKeeperReference shoppeKeeper = _shoppeKeepersByIndex[i];//new TheShoppeKeeperReference();
                string shoppeKeeperName = shoppeKeeperNames[i];
                shoppeKeeper.ShoppeName = shoppeNames[i];
                shoppeKeeper.ShoppeKeeperName = shoppeKeeperName;
                
                List<NonPlayerCharacterReference> npcRefs = npcReferences.GetNonPlayerCharacterByLocationAndNPCType(shoppeKeeper.ShoppeKeeperLocation, shoppeKeeper.TheShoppeKeeperType);
                Debug.Assert(npcRefs.Count == 1);

                shoppeKeeper.NpcRef = npcRefs[0];
                _shoppeKeepers.Add(shoppeKeeperName, shoppeKeeper);

                // we keep track of the location + type for easier world access to shoppe keeper reference
                if (!_shoppeKeepersByLocationAndType.ContainsKey(shoppeKeeper.ShoppeKeeperLocation))
                {
                    _shoppeKeepersByLocationAndType.Add(shoppeKeeper.ShoppeKeeperLocation, new Dictionary<NonPlayerCharacterReference.NPCDialogTypeEnum, ShoppeKeeperReference>());
                }

                _shoppeKeepersByLocationAndType[shoppeKeeper.ShoppeKeeperLocation]
                    .Add(shoppeKeeper.TheShoppeKeeperType, shoppeKeeper);
                
                // if it's a blacksmith then we load their items for sale
                if (shoppeKeeper.NpcRef.NPCType == NonPlayerCharacterReference.NPCDialogTypeEnum.Blacksmith)
                {
                    shoppeKeeper.EquipmentForSaleList = GetEquipmentList(i);
                }
            }
        }

        private List<DataOvlReference.Equipment> GetEquipmentList(int nTown)
        {
            if (nTown > 9) return new List<DataOvlReference.Equipment>();
            
            List<byte> equipmentByteList = _dataOvlReference
                .GetDataChunk(DataOvlReference.DataChunkName.WEAPONS_SOLD_BY_MERCHANTS).GetAsByteList();
            const int nMaxItemsPerTown = 8;
            int nStartIndex = nTown * nMaxItemsPerTown;

            List<byte> equipmentByteListForTown = equipmentByteList.GetRange(nStartIndex,  nMaxItemsPerTown);
            List<DataOvlReference.Equipment> equipmentList = equipmentByteListForTown
                .Select(b => (DataOvlReference.Equipment) b).ToList();

            equipmentList.Remove(DataOvlReference.Equipment.Nothing);
            return equipmentList;
        }
        
        public ShoppeKeeperReference GetShoppeKeeperReference(SmallMapReferences.SingleMapReference.Location location,
            NonPlayerCharacterReference.NPCDialogTypeEnum npcType)
        {
            if (!_shoppeKeepersByLocationAndType.ContainsKey(location))
            {
                throw new Ultima5ReduxException("You asked for "+location+" and it wasn't in the _shoppeKeepersByLocationAndType");
            }

            if (!_shoppeKeepersByLocationAndType[location].ContainsKey(npcType))
            {
                throw new Ultima5ReduxException("You asked for "+npcType+" in "+location+" and it wasn't in the _shoppeKeepersByLocationAndType");
            }
            return _shoppeKeepersByLocationAndType[location][npcType];
        }
        
        private static Dictionary<int, ShoppeKeeperReference> LoadShoppeKeepersByIndex(DataOvlReference dataOvlReference)
        {
            Dictionary<int, ShoppeKeeperReference> result = JsonConvert.DeserializeObject<Dictionary<int, ShoppeKeeperReference>>(Properties.Resources.ShoppeKeeperMap);
            foreach (ShoppeKeeperReference shoppeKeeperReference in result.Values)
            {
                shoppeKeeperReference.TheDataOvlReference = dataOvlReference;
            }
            return result;
        }
        
        // Custom = -1, Guard = 0, Blacksmith = 0x81, Barkeeper = 0x82, HorseSeller = 0x83, ShipSeller = 0x84, Healer = 0x87,
        // InnKeeper = 0x88, MagicSeller = 0x85, GuildMaster = 0x86, Unknown = 0xFF
        // [0] = {string} "Iolo's Bows"
        // [1] = {string} "Naughty Nomaan's"
        // [2] = {string} "Arms of Justice"
        // [3] = {string} "Darkwatch Armoury"
        // [4] = {string} "The Paladin's Protectorate!"
        // [5] = {string} "North Star Armoury"
        // [6] = {string} "Buccaneers Booty"
        // [7] = {string} "The Shattered Shield"
        // [8] = {string} "Siege Crafters"
        // [9] = {string} "The Honest Meal"
        // [10] = {string} "The Wayfarer Tavern"
        // [11] = {string} "The Sword and Keg"
        // [12] = {string} "The Slaughtered Lamb"
        // [13] = {string} "The Humble Palate"
        // [14] = {string} "The Blue Boar Tavern"
        // [15] = {string} "The Cat's Lair"
        // [16] = {string} "The Fallen Virgin"
        // [17] = {string} "The Folley Tap"
        // [18] = {string} "Horse & Rider"
        // [19] = {string} "The Stablehouse"
        // [20] = {string} "Wishing Well Horses"
        // [21] = {string} "Island Shipwrights"
        // [22] = {string} "The Crow's Nest"
        // [23] = {string} "The Oaken Oar"
        // [24] = {string} "The Rusty Bucket"
        // [25] = {string} "The Herbalist"
        // [26] = {string} "Healers Herbs"
        // [27] = {string} "The Alchemist"
        // [28] = {string} "Mysticism"
        // [29] = {string} "The Sharper Mage"
        // [30] = {string} "The Den"
        // [31] = {string} "The Guild"
        // [32] = {string} "The Nemesis"
        // [33] = {string} "The Healers Mission"
        // [34] = {string} "Wounds of Honour"
        // [35] = {string} "The Spirit Healers"
        // [36] = {string} "Healers' Sanctum"
        // [37] = {string} "Sanctuary"
        // [38] = {string} "The Shield of Truth"
        // [39] = {string} "The Empath"
        // [40] = {string} "The Wayfarer Inn"
        // [41] = {string} "The Warrior's Stead"
        // [42] = {string} "The Haunting Inn"
        // [43] = {string} "Hotel Brittany"
        // [44] = {string} "The Smugglers' Inn"
        // [45] = {string} "The King's Ransom Inn"
        
        // [0] = {string} "Gwenneth"
        // [1] = {string} "Nomaan"
        // [2] = {string} "Ronan"
        // [3] = {string} "Shenstone"
        // [4] = {string} "Paul"
        // [5] = {string} "Max"
        // [6] = {string} "Kitiara"
        // [7] = {string} "Steve"
        // [8] = {string} "Thol"
        // [9] = {string} "Sam"
        // [10] = {string} "Tika"
        // [11] = {string} "Nicole"
        // [12] = {string} "Duclas"
        // [13] = {string} "Felicity"
        // [14] = {string} "Jaymes"
        // [15] = {string} "Dr. Cat"
        // [16] = {string} "Nikki"
        // [17] = {string} "Rob"
        // [18] = {string} "Hettar"
        // [19] = {string} "Theoan"
        // [20] = {string} "Ferru"
        // [21] = {string} "Simplon"
        // [22] = {string} "Bantral"
        // [23] = {string} "Captain Blyth"
        // [24] = {string} "Master Hawkins"
        // [25] = {string} "Jones"
        // [26] = {string} "Nilrem"
        // [27] = {string} "Madam Pendra"
        // [28] = {string} "Toama"
        // [29] = {string} "Enlor"
        // [30] = {string} "Virden"
        // [31] = {string} "Braunam"
        // [32] = {string} "Danfits"
        // [33] = {string} "Daem"
        // [34] = {string} "Regina"
        // [35] = {string} "Leila"
        // [36] = {string} "Temptious"
        // [37] = {string} "Milan"
        // [38] = {string} "Jessica"
        // [39] = {string} "Faye"
        // [40] = {string} "Jessip"
        // [41] = {string} "Donya"
        // [42] = {string} "Gremnor"
        // [43] = {string} "Rogi"
        // [44] = {string} "Terbor"
        // [45] = {string} "Lorien"
        // [46] = {string} "Ransack"
    }
}