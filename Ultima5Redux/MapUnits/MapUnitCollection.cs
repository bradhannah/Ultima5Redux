using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;

namespace Ultima5Redux.MapUnits
{
    [DataContract]
    public class MapUnitCollection
    {
        [IgnoreDataMember] public List<MapUnit> AllMapUnits { get; } = new List<MapUnit>(MapUnits.MAX_MAP_CHARACTERS);

        [IgnoreDataMember]
        public Avatar TheAvatar
        {
            get
            {
                if (Avatars.Count < 1 || Avatars.Count > 1)
                    throw new Ultima5ReduxException("Tried to get a single Avatar and had " + Avatars.Count);
                return Avatars[0];
            }
        }

        [IgnoreDataMember] public List<MapUnit> AllActiveMapUnits => AllMapUnits.Where(s => s.IsActive).ToList();
        [IgnoreDataMember] public List<CombatMapUnit> AllCombatMapUnits => GetMapUnitByType<CombatMapUnit>();
        [DataMember] public List<Avatar> Avatars => GetMapUnitByType<Avatar>();
        [DataMember] public List<Enemy> Enemies => GetMapUnitByType<Enemy>();
        [DataMember] public List<CombatPlayer> CombatPlayers => GetMapUnitByType<CombatPlayer>();
        [DataMember] public List<NonPlayerCharacter> NonPlayerCharacters => GetMapUnitByType<NonPlayerCharacter>();
        [DataMember] public List<Horse> Horses => GetMapUnitByType<Horse>();
        [DataMember] public List<MagicCarpet> MagicCarpets => GetMapUnitByType<MagicCarpet>();
        [DataMember] public List<Skiff> Skiffs => GetMapUnitByType<Skiff>();
        [DataMember] public List<Frigate> Frigates => GetMapUnitByType<Frigate>();
        [DataMember] public List<EmptyMapUnit> EmptyMapUnits => GetMapUnitByType<EmptyMapUnit>();

        internal List<T> GetMapUnitByType<T>() where T : MapUnit =>  AllMapUnits.OfType<T>().ToList();

        public void Add(MapUnit mapUnit)
        {
            AllMapUnits.Add(mapUnit);
        }

        public void Clear() => AllMapUnits.Clear();
    }
}