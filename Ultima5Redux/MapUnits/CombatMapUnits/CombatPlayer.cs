using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.Maps;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public sealed class CombatPlayer : CombatMapUnit
    {
        private readonly Inventory _inventory;

        public CombatPlayer(PlayerCharacterRecord record, Point2D xy, Inventory inventory)  
        {
            _inventory = inventory;
            Record = record;
            KeyTileReference = GameReferences.SpriteTileReferences.GetTileReference(record.PrimarySpriteIndex);
            MapUnitPosition = new MapUnitPosition(xy.X, xy.Y, 0);
        }

        public CombatPlayer()
        {
        }

        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        public override bool IsActive => !HasEscaped && Stats.Status != PlayerCharacterRecord.CharacterStatus.Dead;

        public override bool IsAttackable => true;

        public override bool IsInvisible => Record.IsInvisible;

        public override CharacterStats Stats => Record.Stats;

        public override int ClosestAttackRange => GetAttackWeapons().Min(item => item.TheCombatItemReference.Range);

        public override int Defense => _inventory.GetCharacterTotalDefense(Record);

        public override int Dexterity => (byte)Record.Stats.Dexterity;
        public override int Experience => 0;

        public PlayerCharacterRecord Record { get; }
        public override string BoardXitName => "GET OFF ME YOU BRUTE!";
        public override string FriendlyName => Record.Name;

        public override string Name => Record.Name;
        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;

        public override TileReference KeyTileReference
        {
            get
            {
                if (Record.IsInvisible)
                {
                    return GameReferences.SpriteTileReferences.GetTileReferenceByName("Apparition");
                }

                if (Record.IsRat)
                {
                    return GameReferences.SpriteTileReferences.GetTileReferenceByName("Rat1");
                }

                switch (Stats.Status)
                {
                    case PlayerCharacterRecord.CharacterStatus.Dead:
                    case PlayerCharacterRecord.CharacterStatus.Asleep:
                        return GameReferences.SpriteTileReferences.GetTileReferenceByName("DeadBody");
                    default: return base.KeyTileReference; 
                }
            }
            set => base.KeyTileReference = value;
        }

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName { get; } = default;
        protected override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded { get; } = default;
        public override bool IsMyEnemy(CombatMapUnit combatMapUnit) => combatMapUnit is Enemy;

        public override string ToString()
        {
            return Record.Name;
        }

        /// <summary>
        ///     Gets the string used to describe all available weapons that will be outputted to user
        /// </summary>
        /// <returns></returns>
        public string GetAttackWeaponsString()
        {
            List<CombatItem> combatItems = GetAttackWeapons();

            // if (combatItems == null) return "bare hands";

            string combatItemString = "";
            for (int index = 0; index < combatItems.Count; index++)
            {
                CombatItem item = combatItems[index];
                if (index > 0)
                    combatItemString += ", ";
                combatItemString += item.LongName;
            }

            return combatItemString;
        }

        /// <summary>
        ///     Gets a list of all weapons that are available for use by given player character. The list is ordered.
        /// </summary>
        /// <returns>List of attack weapons</returns>
        public List<CombatItem> GetAttackWeapons()
        {
            List<CombatItem> weapons = new List<CombatItem>();

            bool bBareHands = false;

            bool isAttackingCombatItem(DataOvlReference.Equipment equipment)
            {
                if (equipment == DataOvlReference.Equipment.Nothing) return false;
                CombatItem combatItem = _inventory.GetItemFromEquipment(equipment);    
                return combatItem.TheCombatItemReference.AttackStat > 0;
            }

            if (isAttackingCombatItem(Record.Equipped.Helmet))
                weapons.Add(_inventory.GetItemFromEquipment(Record.Equipped.Helmet));

            if (isAttackingCombatItem(Record.Equipped.LeftHand))
                weapons.Add(_inventory.GetItemFromEquipment(Record.Equipped.LeftHand));
            else
                bBareHands = true;

            if (isAttackingCombatItem(Record.Equipped.RightHand))
                weapons.Add(_inventory.GetItemFromEquipment(Record.Equipped.RightHand));
            else
                bBareHands = true;

            if (weapons.Count == 0)
            {
                Debug.Assert(bBareHands);
                weapons.Add(_inventory.GetItemFromEquipment(DataOvlReference.Equipment.BareHands));
            }

            return weapons;
        }

        public bool CanReachForAttack(CombatMapUnit opponentCombatMapUnit, CombatItem item) =>
            CanReachForMeleeAttack(opponentCombatMapUnit, item.TheCombatItemReference.Range);
    }
}