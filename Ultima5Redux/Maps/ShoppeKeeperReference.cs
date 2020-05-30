using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Data;

namespace Ultima5Redux.Maps
{
    public class ShoppeKeeperReference
    {

        private class ShoppeKeeperMap
        {
            [DataMember] public string Location = default;
        }
        
        private readonly List<SmallMapReferences.SingleMapReference.Location> _locationList;
        private readonly List<string> _shoppeKeeperNameList = new List<string>();
        private readonly List<string> _shoppeNameList = new List<string>();
        private readonly DataOvlReference _dataOvlReference;

        public ShoppeKeeperReference(DataOvlReference dataOvlReference)
        {
            _dataOvlReference = dataOvlReference;
            
            _locationList = ShoppeKeeperReference.LoadLocationMap();

            foreach (string shoppeKeeperName in dataOvlReference
                .GetDataChunk(DataOvlReference.DataChunkName.SHOPPE_KEEPER_NAMES).GetChunkAsStringList().Strs)
            {
                _shoppeKeeperNameList.Add(shoppeKeeperName);
            }

            foreach (string shoppeName in dataOvlReference.GetDataChunk(DataOvlReference.DataChunkName.STORE_NAMES)
                .GetChunkAsStringList().Strs)
            {
                _shoppeNameList.Add(shoppeName);
            }

            // Dammit Simplon, how the hell did you sneak into the Shoppe Keeper array!?!
            _shoppeKeeperNameList.Remove("Simplon");
        }

        private static List<SmallMapReferences.SingleMapReference.Location> LoadLocationMap()
        {
            Dictionary<int, ShoppeKeeperMap> result = JsonConvert.DeserializeObject<Dictionary<int, ShoppeKeeperMap>>(Properties.Resources.ShoppeKeeperMap);
            List<SmallMapReferences.SingleMapReference.Location> locationList = new List<SmallMapReferences.SingleMapReference.Location>(result.Count);
            foreach (ShoppeKeeperMap locationStr in result.Values)
            {
                bool bSucceeded = Enum.TryParse<SmallMapReferences.SingleMapReference.Location>(locationStr.Location, out SmallMapReferences.SingleMapReference.Location location);
                if (!bSucceeded)
                {
                    Console.WriteLine(@"DERP");
                }
                locationList.Add(location);

            }
            return locationList;
        }
        
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