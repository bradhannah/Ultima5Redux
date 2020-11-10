using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    /// <summary>
    ///     Describes individual Shoppe Keepers
    /// </summary>
    [DataContract] public class ShoppeKeeperReference
    {
#pragma warning disable CS0649
        [DataMember(Name = "Location")] private string _locationStr;
        [DataMember(Name = "ShoppeKeeperType")] private string _shoppeKeeperType;
#pragma warning restore CS0649
        public string ShoppeKeeperName { get; set; }
        public string ShoppeName { get; set; }

        public List<DataOvlReference.Equipment> EquipmentForSaleList { get; set; }

        public SmallMapReferences.SingleMapReference.Location ShoppeKeeperLocation
        {
            get
            {
                bool bSuccess = Enum.TryParse(_locationStr,
                    out SmallMapReferences.SingleMapReference.Location npcLocation);
                if (!bSuccess)
                    throw new Ultima5ReduxException("Asked for an NPC " + _locationStr +
                                                    " type but the type didn't exist");

                return npcLocation;
            }
        }

        public NonPlayerCharacterReference.NPCDialogTypeEnum TheShoppeKeeperType
        {
            get
            {
                bool bSuccess = Enum.TryParse(_shoppeKeeperType,
                    out NonPlayerCharacterReference.NPCDialogTypeEnum npcDialogTypeEnum);
                if (!bSuccess)
                    throw new Ultima5ReduxException("Asked for an NPC " + _shoppeKeeperType +
                                                    " type but the type didn't exist");

                return npcDialogTypeEnum;
            }
        }

        public NonPlayerCharacterReference NpcRef { get; set; }
    }
}