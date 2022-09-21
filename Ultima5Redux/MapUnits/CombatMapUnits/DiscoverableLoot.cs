using System.Collections.Generic;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    /// <summary>
    ///     Represents an invisible mapunit that includes inner items
    ///     This a secret that can be found by searching - such as the axe in Jhelom
    /// </summary>
    public class DiscoverableLoot : NonAttackingUnit
    {
        public override string FriendlyName => "Loot";
        public override string PluralName => "Loot";
        public override string SingularName => "Loot";
        public override string Name => "Loot";
        public override bool ExposeInnerItemsOnOpen => false;
        public override bool ExposeInnerItemsOnSearch => true;
        public override bool IsOpenable => false;
        public override bool IsSearchable => true;
        public override bool DoesTriggerTrap(PlayerCharacterRecord record) => false;
        public override bool IsInvisible => true;

        private List<SearchItem> _listOfSearchItems;

        public DiscoverableLoot(List<SearchItem> searchItems) => _listOfSearchItems = searchItems;
    }
}