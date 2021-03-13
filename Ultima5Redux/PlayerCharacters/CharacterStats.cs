using System;
using Ultima5Redux.PlayerCharacters.CombatItems;

namespace Ultima5Redux.PlayerCharacters
{
    public class CharacterStats
    {
        private int _currentHp;
        private PlayerCharacterRecord.CharacterStatus _status; 
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Intelligence { get; set; }
        public int CurrentMp { get; set; }

        public int CurrentHp
        {
            get => Status == PlayerCharacterRecord.CharacterStatus.Dead ? 0 : _currentHp;
            set => _currentHp = Math.Max(0,value);
        }

        public int MaximumHp { get; set; }
        public int ExperiencePoints { get; set; }
        public int Level { get; set; }

        public PlayerCharacterRecord.CharacterStatus Status {
            get => _currentHp <= 0 ? PlayerCharacterRecord.CharacterStatus.Dead : _status;
            set => _status = value;
        }
    }
}