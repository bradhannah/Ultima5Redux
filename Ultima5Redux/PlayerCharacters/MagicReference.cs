using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ultima5Redux.PlayerCharacters.Inventory;

namespace Ultima5Redux.PlayerCharacters
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [DataContract]  
    public class MagicReference
    {
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

        private readonly Dictionary<Reagent.ReagentTypeEnum, bool> _reagentsDictionary =
            new Dictionary<Reagent.ReagentTypeEnum, bool>();
        
        [JsonConverter(typeof(StringEnumConverter))]
        public enum MagicTypeEnum { Peace, Support, Attack, Debuff, None}  
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TimePermittedEnum { Peace, Combat, Anytime, Combat_Dungeon, Dungeon, Never}

        public bool IsReagentRequired(Reagent.ReagentTypeEnum reagentType) => _reagentsDictionary[reagentType];
        
        [DataMember] public SpellWords SpellEnum;
        [DataMember] public string Spell;

        private void SetReagentState(Reagent.ReagentTypeEnum reagentType, bool bSpellRequirement)
        {
            if (_reagentsDictionary.ContainsKey(reagentType)) _reagentsDictionary[reagentType] = bSpellRequirement;
            else _reagentsDictionary.Add(reagentType, bSpellRequirement);
        }

        private bool GetReagentState(Reagent.ReagentTypeEnum reagentType) => 
            _reagentsDictionary.ContainsKey(reagentType) && _reagentsDictionary[reagentType];
        
        [DataMember]
        private bool SulfurAsh
        {
            get => _reagentsDictionary.ContainsKey(Reagent.ReagentTypeEnum.SulfurAsh);
            set => SetReagentState(Reagent.ReagentTypeEnum.SulfurAsh, value);
        }
        [DataMember] private bool Ginseng
        {
            get => _reagentsDictionary.ContainsKey(Reagent.ReagentTypeEnum.Ginseng);
            set => SetReagentState(Reagent.ReagentTypeEnum.Ginseng, value);
        }

        [DataMember] private bool Garlic
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.Garlic);
            set => SetReagentState(Reagent.ReagentTypeEnum.Garlic, value);
        }

        [DataMember] private bool SpiderSilk
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.SpiderSilk);
            set => SetReagentState(Reagent.ReagentTypeEnum.SpiderSilk, value);
        }

        [DataMember] private bool BloodMoss
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.BloodMoss);
            set => SetReagentState(Reagent.ReagentTypeEnum.BloodMoss, value);
        }

        [DataMember] private bool BlackPearl
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.BlackPearl);
            set => SetReagentState(Reagent.ReagentTypeEnum.BlackPearl, value);
        }

        [DataMember] private bool NightShade
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.NightShade);
            set => SetReagentState(Reagent.ReagentTypeEnum.NightShade, value);
        }

        [DataMember] private bool MandrakeRoot
        {
            get => GetReagentState(Reagent.ReagentTypeEnum.MandrakeRoot);
            set => SetReagentState(Reagent.ReagentTypeEnum.MandrakeRoot, value);
        }

        [DataMember] public int Circle;
        [DataMember] public MagicTypeEnum Type;
        [DataMember] public string SimpleDescription;
        [DataMember] public string SimilarFunction;
        [DataMember] public TimePermittedEnum TimePermitted;
        [DataMember] public int Gold;
        [DataMember] public string RawGoldReagents;
    }
}