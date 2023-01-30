using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ultima5Redux.PlayerCharacters;
using Ultima5Redux.PlayerCharacters.CombatItems;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    [DataContract] public sealed class CombatPlayer : CombatMapUnit
    {
        public override Avatar.AvatarState BoardedAvatarState => Avatar.AvatarState.Hidden;
        public override string BoardXitName => "GET OFF ME YOU BRUTE!";

        public override int ClosestAttackRange => GetAttackWeapons().Min(item => item.TheCombatItemReference.Range);

        public override int Defense => GameStateReference.State.PlayerInventory.GetCharacterTotalDefense(Record);

        public override int Dexterity => (byte)Record.Stats.Dexterity;
        public override int Experience => 0;
        public override string FriendlyName => Record.Name;

        public override bool IsActive => !HasEscaped;

        public override bool IsAttackable => true;

        public override bool IsInvisible => Record.IsInvisible;

        public override TileReference KeyTileReference
        {
            get
            {
                if (Record.IsInvisible)
                {
                    return GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("Apparition");
                }

                if (Record.IsRat)
                {
                    return GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("Rat1");
                }

                switch (Stats.Status)
                {
                    case PlayerCharacterRecord.CharacterStatus.Dead:
                    case PlayerCharacterRecord.CharacterStatus.Asleep:
                        return GameReferences.Instance.SpriteTileReferences.GetTileReferenceByName("DeadBody");
                    default: return base.KeyTileReference;
                }
            }
            set => base.KeyTileReference = value;
        }

        public override string Name => Record.Name;
        public override string PluralName => FriendlyName;
        public override string SingularName => FriendlyName;

        public override CharacterStats Stats
        {
            get => Record.Stats;
            protected set => Record.Stats = value;
        }

        public PlayerCharacterRecord Record { get; }
        protected internal override Dictionary<Point2D.Direction, string> DirectionToTileNameBoarded => default;

        protected override Dictionary<Point2D.Direction, string> DirectionToTileName => default;

        public CombatPlayer(PlayerCharacterRecord record, Point2D xy)
        {
            Record = record;
            KeyTileReference = GameReferences.Instance.SpriteTileReferences.GetTileReference(record.PrimarySpriteIndex);
            MapUnitPosition = new MapUnitPosition(xy.X, xy.Y, 0);
        }

        [JsonConstructor] public CombatPlayer()
        {
        }

        public override bool IsMyEnemy(CombatMapUnit combatMapUnit) => combatMapUnit is Enemy;

        public override string ToString() => Record.Name;

        public bool CanReachForAttack(CombatMapUnit opponentCombatMapUnit, CombatItem item) =>
            CanReachForMeleeAttack(opponentCombatMapUnit, item.TheCombatItemReference.Range);

        /// <summary>
        ///     Gets a list of all weapons that are available for use by given player character. The list is ordered.
        /// </summary>
        /// <returns>List of attack weapons</returns>
        public List<CombatItem> GetAttackWeapons()
        {
            List<CombatItem> weapons = new();

            bool bBareHands = false;

            bool isAttackingCombatItem(DataOvlReference.Equipment equipment)
            {
                if (equipment == DataOvlReference.Equipment.Nothing) return false;
                CombatItem combatItem = GameStateReference.State.PlayerInventory.GetItemFromEquipment(equipment);
                return combatItem.TheCombatItemReference.AttackStat > 0;
            }

            if (isAttackingCombatItem(Record.Equipped.Helmet))
                weapons.Add(GameStateReference.State.PlayerInventory.GetItemFromEquipment(Record.Equipped.Helmet));

            if (isAttackingCombatItem(Record.Equipped.LeftHand))
                weapons.Add(GameStateReference.State.PlayerInventory.GetItemFromEquipment(Record.Equipped.LeftHand));
            else
                bBareHands = true;

            if (isAttackingCombatItem(Record.Equipped.RightHand))
                weapons.Add(GameStateReference.State.PlayerInventory.GetItemFromEquipment(Record.Equipped.RightHand));
            else
                bBareHands = true;

            if (weapons.Count != 0) return weapons;

            Debug.Assert(bBareHands);
            weapons.Add(
                GameStateReference.State.PlayerInventory.GetItemFromEquipment(DataOvlReference.Equipment.BareHands));

            return weapons;
        }

        /// <summary>
        ///     Gets the string used to describe all available weapons that will be outputted to user
        /// </summary>
        /// <returns></returns>
        public string GetAttackWeaponsString()
        {
            List<CombatItem> combatItems = GetAttackWeapons();

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
    }
}