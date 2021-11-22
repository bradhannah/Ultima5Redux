using System;
using System.Runtime.Serialization;

namespace Ultima5Redux.PlayerCharacters
{
    [DataContract] public class CharacterStats
    {
        [IgnoreDataMember] private int _currentHp;
        [IgnoreDataMember] private PlayerCharacterRecord.CharacterStatus _status;

        [DataMember] public int CurrentHp
        {
            get => Status == PlayerCharacterRecord.CharacterStatus.Dead ? 0 : _currentHp;
            set => _currentHp = Math.Max(0, value);
        }

        [DataMember] public int CurrentMp { get; set; }
        [DataMember] public int Strength { get; set; }
        [DataMember] public int Intelligence { get; set; }
        [DataMember] public int Dexterity { get; set; }
        [DataMember] public int ExperiencePoints { get; set; }
        [DataMember] public int Level { get; set; }
        [DataMember] public int MaximumHp { get; set; }

        [DataMember] public PlayerCharacterRecord.CharacterStatus Status
        {
            get => _currentHp <= 0 ? PlayerCharacterRecord.CharacterStatus.Dead : _status;
            set => _status = value;
        }

    }
}