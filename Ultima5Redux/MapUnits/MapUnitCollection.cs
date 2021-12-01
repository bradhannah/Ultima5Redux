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
        [DataMember(Name = "Avatars")] private Avatar[] SaveAvatars
        {
            get => GetMapUnitByTypeToArray<Avatar>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "CombatPlayers")] private CombatPlayer[] SaveCombatPlayers
        {
            get => GetMapUnitByTypeToArray<CombatPlayer>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "EmptyMapUnits")] private EmptyMapUnit[] SaveEmptyMapUnits
        {
            get => GetMapUnitByTypeToArray<EmptyMapUnit>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "Enemies")] private Enemy[] SaveEnemies
        {
            get => GetMapUnitByTypeToArray<Enemy>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "Frigates")] private Frigate[] SaveFrigates
        {
            get => GetMapUnitByTypeToArray<Frigate>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "Horses")] private Horse[] SaveHorses
        {
            get => GetMapUnitByTypeToArray<Horse>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "MagicCarpets")] private MagicCarpet[] SaveMagicCarpets
        {
            get => GetMapUnitByTypeToArray<MagicCarpet>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "NonPlayerCharacters")] private NonPlayerCharacter[] SaveNonPlayerCharacters
        {
            get => GetMapUnitByTypeToArray<NonPlayerCharacter>();
            set => ReplaceAll(value);
        }

        [DataMember(Name = "Skiffs")] private Skiff[] SaveSkiffs
        {
            get => GetMapUnitByTypeToArray<Skiff>();
            set => ReplaceAll(value);
        }

        [IgnoreDataMember] public List<MapUnit> AllActiveMapUnits => AllMapUnits.Where(s => s.IsActive).ToList();
        [IgnoreDataMember] public List<CombatMapUnit> AllCombatMapUnits => GetMapUnitByType<CombatMapUnit>();
        [IgnoreDataMember] public List<MapUnit> AllMapUnits { get; } = new List<MapUnit>(MapUnits.MAX_MAP_CHARACTERS);

        [IgnoreDataMember] public List<Avatar> Avatars => GetMapUnitByType<Avatar>();

        [IgnoreDataMember] public List<CombatPlayer> CombatPlayers => GetMapUnitByType<CombatPlayer>();

        [IgnoreDataMember] public List<EmptyMapUnit> EmptyMapUnits => GetMapUnitByType<EmptyMapUnit>();

        [IgnoreDataMember] public List<Enemy> Enemies => GetMapUnitByType<Enemy>();

        [IgnoreDataMember] public List<Frigate> Frigates => GetMapUnitByType<Frigate>();

        [IgnoreDataMember] public List<Horse> Horses => GetMapUnitByType<Horse>();

        [IgnoreDataMember] public List<MagicCarpet> MagicCarpets => GetMapUnitByType<MagicCarpet>();

        [IgnoreDataMember] public List<NonPlayerCharacter> NonPlayerCharacters =>
            GetMapUnitByType<NonPlayerCharacter>();

        [IgnoreDataMember] public List<Skiff> Skiffs => GetMapUnitByType<Skiff>();

        [IgnoreDataMember] public Avatar TheAvatar
        {
            get
            {
                if (Avatars.Count < 1 || Avatars.Count > 1)
                    throw new Ultima5ReduxException("Tried to get a single Avatar and had " + Avatars.Count);
                return Avatars[0];
            }
        }

        [JsonConstructor] public MapUnitCollection()
        {
        }

        internal List<T> GetMapUnitByType<T>() where T : MapUnit
        {
            return AllMapUnits.OfType<T>().ToList();
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