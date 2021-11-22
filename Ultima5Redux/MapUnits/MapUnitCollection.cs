using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.MapUnits.CombatMapUnits;
using Ultima5Redux.MapUnits.NonPlayerCharacters;
using Ultima5Redux.MapUnits.SeaFaringVessels;

namespace Ultima5Redux.MapUnits
{
    [DataContract] public sealed class MapUnitCollection
    {
        [DataMember] public List<Avatar> Avatars => GetMapUnitByType<Avatar>();

        private void ReplaceAll<T>(IEnumerable<T> newMapUnits) where T : MapUnit
        {
            AllMapUnits.RemoveAll(item => item is T);
            AllMapUnits.AddRange(newMapUnits);
        }

        [DataMember] public List<CombatPlayer> CombatPlayers
        {
            get => GetMapUnitByType<CombatPlayer>();
            set => ReplaceAll(value);
        }

        [DataMember] public List<EmptyMapUnit> EmptyMapUnits
        {
            get => GetMapUnitByType<EmptyMapUnit>();
            set => ReplaceAll(value);
        }

        [DataMember] public List<Enemy> Enemies
        {
            get => GetMapUnitByType<Enemy>();
            set => ReplaceAll(value);
        }

        [DataMember] public List<Frigate> Frigates
        {
            get => GetMapUnitByType<Frigate>();
            set => ReplaceAll(value);
        }

        [DataMember] public List<Horse> Horses
        {
            get => GetMapUnitByType<Horse>();
            set => ReplaceAll(value);
        }

        [DataMember] public List<MagicCarpet> MagicCarpets
        {
            get => GetMapUnitByType<MagicCarpet>();
            set => ReplaceAll(value);
        }

        [DataMember] public List<NonPlayerCharacter> NonPlayerCharacters
        {
            get => GetMapUnitByType<NonPlayerCharacter>();
            set => ReplaceAll(value);
        }

        [DataMember] public List<Skiff> Skiffs
        {
            get => GetMapUnitByType<Skiff>();
            set => ReplaceAll(value);
        }

        [IgnoreDataMember] public List<MapUnit> AllActiveMapUnits => AllMapUnits.Where(s => s.IsActive).ToList();
        [IgnoreDataMember] public List<CombatMapUnit> AllCombatMapUnits => GetMapUnitByType<CombatMapUnit>();
        [IgnoreDataMember] public List<MapUnit> AllMapUnits { get; } = new List<MapUnit>(MapUnits.MAX_MAP_CHARACTERS);

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

        public void Add(MapUnit mapUnit)
        {
            AllMapUnits.Add(mapUnit);
        }

        public void Clear() => AllMapUnits.Clear();

        internal List<T> GetMapUnitByType<T>() where T : MapUnit => AllMapUnits.OfType<T>().ToList();
    }
}