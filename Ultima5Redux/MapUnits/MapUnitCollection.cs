using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public class MapUnitCollection
    {
        private Avatar _theAvatar;

        [DataMember(Name = "Avatars")]
        private Avatar[] SaveAvatars
        {
            get => GetMapUnitByTypeToArray<Avatar>();
            set
            {
                ReplaceAll(value);
                if (value.Length > 0)
                    _theAvatar = value[0];
            }
        }

        [DataMember(Name = "CombatPlayers")]
        private CombatPlayer[] SaveCombatPlayers
        {
            get => GetMapUnitByTypeToArray<CombatPlayer>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "EmptyMapUnits")]
        private EmptyMapUnit[] SaveEmptyMapUnits
        {
            get => GetMapUnitByTypeToArray<EmptyMapUnit>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "Enemies")]
        private Enemy[] SaveEnemies
        {
            get => GetMapUnitByTypeToArray<Enemy>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "Frigates")]
        private Frigate[] SaveFrigates
        {
            get => GetMapUnitByTypeToArray<Frigate>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "Horses")]
        private Horse[] SaveHorses
        {
            get => GetMapUnitByTypeToArray<Horse>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "MagicCarpets")]
        private MagicCarpet[] SaveMagicCarpets
        {
            get => GetMapUnitByTypeToArray<MagicCarpet>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "NonPlayerCharacters")]
        private NonPlayerCharacter[] SaveNonPlayerCharacters
        {
            get => GetMapUnitByTypeToArray<NonPlayerCharacter>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "Skiffs")]
        private Skiff[] SaveSkiffs
        {
            get => GetMapUnitByTypeToArray<Skiff>();
            set => ReplaceAll(value);
        }

        [IgnoreDataMember]
        public IEnumerable<MapUnit> AllActiveMapUnits => AllMapUnits.Where(s => s.IsActive); //.ToList();

        [IgnoreDataMember] public IEnumerable<CombatMapUnit> AllCombatMapUnits => GetMapUnitByType<CombatMapUnit>();
        [IgnoreDataMember] public List<MapUnit> AllMapUnits { get; } = new(MapUnits.MAX_MAP_CHARACTERS);

        [IgnoreDataMember] public IEnumerable<Avatar> Avatars => GetMapUnitByType<Avatar>();

        [IgnoreDataMember] public IEnumerable<CombatPlayer> CombatPlayers => GetMapUnitByType<CombatPlayer>();

        [IgnoreDataMember] public IEnumerable<EmptyMapUnit> EmptyMapUnits => GetMapUnitByType<EmptyMapUnit>();

        [IgnoreDataMember] public IEnumerable<Enemy> Enemies => GetMapUnitByType<Enemy>();

        [IgnoreDataMember] public IEnumerable<Frigate> Frigates => GetMapUnitByType<Frigate>();

        [IgnoreDataMember] public IEnumerable<Horse> Horses => GetMapUnitByType<Horse>();

        [IgnoreDataMember] public IEnumerable<MagicCarpet> MagicCarpets => GetMapUnitByType<MagicCarpet>();

        [IgnoreDataMember]
        public IEnumerable<NonPlayerCharacter> NonPlayerCharacters =>
            GetMapUnitByType<NonPlayerCharacter>();

        [IgnoreDataMember] public IEnumerable<Skiff> Skiffs => GetMapUnitByType<Skiff>();

        [IgnoreDataMember]
        public Avatar TheAvatar
        {
            get
            {
                if (_theAvatar == null)
                    return _theAvatar = Avatars.ToList()[0];
                return _theAvatar;
                // if (Avatars.Count is < 1 or > 1)
                //     throw new Ultima5ReduxException("Tried to get a single Avatar and had " + Avatars.Count);
                // return Avatars[0];
            }
        }

        [JsonConstructor] public MapUnitCollection()
        {
        }

        internal IEnumerable<T> GetMapUnitByType<T>() where T : MapUnit
        {
            return AllMapUnits.OfType<T>();
        }

        private T[] GetMapUnitByTypeToArray<T>() where T : MapUnit
        {
            return AllMapUnits.OfType<T>().ToArray();
        }

        private void ReplaceAll<T>(IEnumerable<T> newMapUnits) where T : MapUnit
        {
            AllMapUnits.RemoveAll(item => item is T);
            AllMapUnits.AddRange(newMapUnits);
        }

        public void Add(MapUnit mapUnit)
        {
            AllMapUnits.Add(mapUnit);
        }

        public void Clear() => AllMapUnits.Clear();
    }
}