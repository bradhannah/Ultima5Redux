using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.Maps;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;

namespace Ultima5Redux.MapUnits
{
    [DataContract]
    public class MapUnitCollection
    {
        [DataMember(Name = "Avatars")]
        private Avatar[] SaveAvatars
        {
            get => GetMapUnitByTypeToArray<Avatar>();
            set
            {
                ReplaceAll(value);
                _bForceNewAvatar = true;
            }
        }

        [DataMember(Name = "BloodSpatters")]
        private BloodSpatter[] SaveBloodSpatters
        {
            get => GetMapUnitByTypeToArray<BloodSpatter>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "Chest")]
        private Chest[] SaveChests
        {
            get => GetMapUnitByTypeToArray<Chest>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "CombatPlayers")]
        private CombatPlayer[] SaveCombatPlayers
        {
            get => GetMapUnitByTypeToArray<CombatPlayer>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "DeadBodies")]
        private DeadBody[] SaveDeadBodies
        {
            get => GetMapUnitByTypeToArray<DeadBody>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "DiscoverableLoot")]
        private DiscoverableLoot[] SaveDiscoverableLoot
        {
            get => GetMapUnitByTypeToArray<DiscoverableLoot>();
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

        [DataMember(Name = "ItemStacks")]
        private ItemStack[] SaveItemStacks
        {
            get => GetMapUnitByTypeToArray<ItemStack>();
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

        [IgnoreDataMember] public IEnumerable<MapUnit> AllActiveMapUnits => AllMapUnits.Where(s => s.IsActive);

        [IgnoreDataMember] public IEnumerable<CombatMapUnit> AllCombatMapUnits => GetMapUnitByType<CombatMapUnit>();
        [IgnoreDataMember] public List<MapUnit> AllMapUnits { get; } = new(Map.MAX_MAP_CHARACTERS);

        [IgnoreDataMember] public IEnumerable<Avatar> Avatars => GetMapUnitByType<Avatar>();

        [IgnoreDataMember] public IEnumerable<CombatPlayer> CombatPlayers => GetMapUnitByType<CombatPlayer>();

        [IgnoreDataMember] public IEnumerable<EmptyMapUnit> EmptyMapUnits => GetMapUnitByType<EmptyMapUnit>();

        [IgnoreDataMember] public IEnumerable<Enemy> Enemies => GetMapUnitByType<Enemy>();

        [IgnoreDataMember] public IEnumerable<Frigate> Frigates => GetMapUnitByType<Frigate>();

        [IgnoreDataMember] public IEnumerable<Horse> Horses => GetMapUnitByType<Horse>();

        [IgnoreDataMember] public IEnumerable<MagicCarpet> MagicCarpets => GetMapUnitByType<MagicCarpet>();

        [IgnoreDataMember]
        public IEnumerable<NonAttackingUnit> NonAttackingUnits => GetMapUnitByType<NonAttackingUnit>();

        [IgnoreDataMember]
        public IEnumerable<NonPlayerCharacter> NonPlayerCharacters =>
            GetMapUnitByType<NonPlayerCharacter>();

        [IgnoreDataMember] public IEnumerable<Skiff> Skiffs => GetMapUnitByType<Skiff>();

        [IgnoreDataMember]
        public Avatar TheAvatar
        {
            get
            {
                if (_avatar != null && !_bForceNewAvatar) return _avatar;
                _bForceNewAvatar = false;
                _avatar = Avatars.Any() ? Avatars.First() : null;
                return _avatar;
            }
        }

        private Avatar _avatar;
        private bool _bForceNewAvatar;

        public Dictionary<Point2D, List<MapUnit>> CachedActiveDictionary { get; private set; }

        [JsonConstructor]
        public MapUnitCollection()
        {
        }

        [OnDeserialized]
        private void PostDeserialize(StreamingContext context)
        {
            _bForceNewAvatar = true;
            RefreshActiveDictionaryCache();
        }

        internal IEnumerable<T> GetMapUnitByType<T>() where T : MapUnit => AllMapUnits.OfType<T>();

        private Dictionary<Point2D, List<MapUnit>> CreateMapUnitByPositionDictionary()
        {
            Dictionary<Point2D, List<MapUnit>> mapUnitDictionary = new();

            foreach (MapUnit mapUnit in AllActiveMapUnits)
            {
                if (mapUnit is EmptyMapUnit) continue;

                Point2D xy = mapUnit.MapUnitPosition.XY;
                if (!mapUnitDictionary.ContainsKey(xy))
                {
                    mapUnitDictionary.Add(xy, new List<MapUnit>());
                }

                mapUnitDictionary[xy].Add(mapUnit);
            }

            return mapUnitDictionary;
        }

        private T[] GetMapUnitByTypeToArray<T>() where T : MapUnit => AllMapUnits.OfType<T>().ToArray();

        private void ReplaceAll<T>(IEnumerable<T> newMapUnits) where T : MapUnit
        {
            AllMapUnits.RemoveAll(item => item is T);
            AllMapUnits.AddRange(newMapUnits);
            _bForceNewAvatar = true;
        }

        public void Add(MapUnit mapUnit)
        {
            AllMapUnits.Add(mapUnit);
            if (mapUnit is Avatar) _bForceNewAvatar = true;
        }

        public void AddMapUnit(MapUnit mapUnit)
        {
            AllMapUnits.Add(mapUnit);
        }

        public void Clear()
        {
            AllMapUnits.Clear();
        }

        public bool ClearMapUnit(MapUnit mapUnit) => AllMapUnits.Remove(mapUnit);

        public void RefreshActiveDictionaryCache()
        {
            CachedActiveDictionary = CreateMapUnitByPositionDictionary();
        }
    }
}