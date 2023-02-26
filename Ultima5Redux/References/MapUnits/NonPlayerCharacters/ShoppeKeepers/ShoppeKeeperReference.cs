using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.References.MapUnits.NonPlayerCharacters.ShoppeKeepers
{
    /// <summary>
    ///     Describes individual Shoppe Keepers
    /// </summary>
    [DataContract]
    public class ShoppeKeeperReference
    {
        public List<DataOvlReference.Equipment> EquipmentForSaleList { get; set; }

        public NonPlayerCharacterReference NpcRef { get; set; }

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

        public string ShoppeKeeperName { get; set; }
        public string ShoppeName { get; set; }

        public NonPlayerCharacterReference.SpecificNpcDialogType TheShoppeKeeperType
        {
            get
            {
                bool bSuccess = Enum.TryParse(_shoppeKeeperType,
                    out NonPlayerCharacterReference.SpecificNpcDialogType npcDialogTypeEnum);
                if (!bSuccess)
                    throw new Ultima5ReduxException("Asked for an NPC " + _shoppeKeeperType +
                                                    " type but the type didn't exist");

                return npcDialogTypeEnum;
            }
        }
#pragma warning disable CS0649
        [DataMember(Name = "Location")] private string _locationStr;
        [DataMember(Name = "ShoppeKeeperType")]
        private string _shoppeKeeperType;
#pragma warning restore CS0649
    }
}