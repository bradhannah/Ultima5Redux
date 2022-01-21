using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References.PlayerCharacters.Inventory.SpellSubTypes;

namespace Ultima5Redux.References.PlayerCharacters.Inventory
{
    [SuppressMessage("ReSharper", "InconsistentNaming")] [DataContract]
    public class MagicReference
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpecificMagicType { Peace, Support, Attack, Debuff, None }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpellWords
        {
            // taking a bit of a risk and just let the subsequent values be assigned since they should be in order
            In_Lor = 0x24A, Grav_Por, An_Zu, An_Nox, Mani, An_Ylem, An_Sanct, An_Xen_Corp, Rel_Hur, In_Wis, Kal_Xen,
            In_Xen_Mani, Vas_Lor, Vas_Flam, In_Flam_Grav, In_Nox_Grav, In_Zu_Grav, In_Por, An_Grav, In_Sanct,
            In_Sanct_Grav, Uus_Por, Des_Por, Wis_Quas, In_Bet_Xen, An_Ex_Por, In_Ex_Por, Vas_Mani, In_Zu, Rel_Tym,
            In_Vas_Por_Ylem, Quas_An_Wis, In_An, Wis_An_Ylem, An_Xen_Ex, Rel_Xen_Bet, Sanct_Lor, Xen_Corp, In_Quas_Xen,
            In_Quas_Wis, In_Nox_Hur, In_Quas_Corp, In_Mani_Corp, Kal_Xen_Corp, In_Vas_Grav_Corp, In_Flam_Hur,
            Vas_Rel_Por, An_Tym, Nox
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpecificTimePermitted { Peace, Combat, Anytime, Combat_Dungeon, Dungeon, Never }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpecificSpellTargetType
        {
            NoSelection, CastingCombatPlayer, SelectedCombatPlayer, SelectedMapUnit, Direction,
            SelectedCombatMapPosition, SelectedMapPosition
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpecificSpellSubType
        {
            /// <summary>
            ///     Adds new MapUnit to map
            /// </summary>
            SummonCreature,

            /// <summary>
            ///     Performs some sort of healing or curing to one or more party members
            /// </summary>
            Healing,

            /// <summary>
            ///     Sprays a bunch of X in a pattern
            /// </summary>
            SprayBlast,

            /// <summary>
            ///     Improves something about one or more party members
            /// </summary>
            Buff,

            /// <summary>
            ///     Performs a simple thing typically in the overworld
            /// </summary>
            Utility,

            /// <summary>
            ///     Dispels someone or something
            /// </summary>
            Dispel,

            /// <summary>
            ///     Magical attacks that aren't otherwise categorized
            /// </summary>
            MagicAttack,

            /// <summary>
            ///     Ascend or Descend spells
            /// </summary>
            AscendDescend,

            /// <summary>
            ///     Changes the state of the enemy, but doesn't damage them (ie. Charm, Sleep)
            /// </summary>
            ChangeEnemyState,

            /// <summary>
            ///     One off things...
            /// </summary>
            Other
        }

        [DataMember]
        private bool BlackPearl
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.BlackPearl);
            set => SetReagentState(Reagent.ReagentTypeEnum.BlackPearl, value);
        }

        [DataMember]
        private bool BloodMoss
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.BloodMoss);
            set => SetReagentState(Reagent.ReagentTypeEnum.BloodMoss, value);
        }

        [DataMember]
        private bool Garlic
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.Garlic);
            set => SetReagentState(Reagent.ReagentTypeEnum.Garlic, value);
        }

        [DataMember]
        private bool Ginseng
        {
            get => _reagentsDictionary.ContainsKey(Reagent.ReagentTypeEnum.Ginseng);
            set => SetReagentState(Reagent.ReagentTypeEnum.Ginseng, value);
        }

        [DataMember]
        private bool MandrakeRoot
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.MandrakeRoot);
            set => SetReagentState(Reagent.ReagentTypeEnum.MandrakeRoot, value);
        }

        [DataMember]
        private bool NightShade
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.NightShade);
            set => SetReagentState(Reagent.ReagentTypeEnum.NightShade, value);
        }

        [DataMember]
        private bool SpiderSilk
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.SpiderSilk);
            set => SetReagentState(Reagent.ReagentTypeEnum.SpiderSilk, value);
        }

        [DataMember]
        private bool SulfurAsh
        {
            get => _reagentsDictionary.ContainsKey(Reagent.ReagentTypeEnum.SulfurAsh);
            set => SetReagentState(Reagent.ReagentTypeEnum.SulfurAsh, value);
        }

        [DataMember] public int Circle { get; private set; }

        [DataMember] public int Gold { get; private set; }

        [DataMember] public string RawGoldReagents { get; private set; }
        [DataMember] public string SimilarFunction { get; private set; }
        [DataMember] public string SimpleDescription { get; private set; }
        [DataMember] public string Spell { get; private set; }

        [DataMember] public SpellWords SpellEnum { get; set; }

        [DataMember] public SpecificTimePermitted TimePermitted { get; private set; }
        [DataMember] public SpecificMagicType MagicType { get; private set; }
        [DataMember] public SpecificSpellSubType SpellSubType { get; private set; }
        [DataMember] public SpecificSpellTargetType SpellTargetType { get; private set; }

        private readonly Dictionary<Reagent.ReagentTypeEnum, bool> _reagentsDictionary = new();

        private bool GetReagentState(Reagent.ReagentTypeEnum reagentType) =>
            _reagentsDictionary.ContainsKey(reagentType) && _reagentsDictionary[reagentType];

        private void SetReagentState(Reagent.ReagentTypeEnum reagentType, bool bSpellRequirement)
        {
            if (_reagentsDictionary.ContainsKey(reagentType)) _reagentsDictionary[reagentType] = bSpellRequirement;
            else _reagentsDictionary.Add(reagentType, bSpellRequirement);
        }

        public bool IsCorrectReagents(IEnumerable<Reagent.ReagentTypeEnum> reagents)
        {
            int nReagents = 0;
            foreach (Reagent.ReagentTypeEnum reagent in reagents)
            {
                if (!IsReagentRequired(reagent)) return false;
                nReagents++;
            }

            return (nReagents == _reagentsDictionary.Count(r => r.Value));
        }

        public bool IsReagentRequired(Reagent.ReagentTypeEnum reagentType) => _reagentsDictionary[reagentType];

        internal SpellResult CastSpell(GameState state, SpellCastingDetails details)
        {
            SpellSubType spellSubType = CreateSpellCasting();

            return spellSubType.CastSpell(state, details);
        }

        public bool IsCastablePresently(GameState state)
        {
            return CreateSpellCasting().IsCastablePresently(state);
        }

        private SpellSubType CreateSpellCasting()
        {
            return SpellSubType switch
            {
                SpecificSpellSubType.SummonCreature => new SummonCreatureSubType(this),
                SpecificSpellSubType.Healing => new HealingSpellSubType(this),
                SpecificSpellSubType.SprayBlast => new SprayBlastSpellSubType(this),
                SpecificSpellSubType.Buff => new BuffSpellSubType(this),
                SpecificSpellSubType.Utility => new UtilitySpellSubType(this),
                SpecificSpellSubType.Dispel => new DispelSpellSubType(this),
                SpecificSpellSubType.MagicAttack => new MagicAttackSpellSubType(this),
                SpecificSpellSubType.AscendDescend => new AscendDescendSpellSubType(this),
                SpecificSpellSubType.ChangeEnemyState => new ChangeEnemyStateSpellSubType(this),
                SpecificSpellSubType.Other => new OtherSpellSubType(this),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}