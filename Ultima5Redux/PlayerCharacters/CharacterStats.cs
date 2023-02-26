using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Ultima5Redux.PlayerCharacters
{
    [DataContract]
    public class CharacterStats
    {
        [DataMember]
        public int CurrentHp
        {
            get => Status == PlayerCharacterRecord.CharacterStatus.Dead ? 0 : _currentHp;
            set => _currentHp = Math.Max(0, value);
        }

        [DataMember] public int CurrentMp { get; set; }
        [DataMember] public int Dexterity { get; set; }
        [DataMember] public int ExperiencePoints { get; set; }
        [DataMember] public int Intelligence { get; set; }
        [DataMember] public int Level { get; set; }
        [DataMember] public int MaximumHp { get; set; }

        [DataMember]
        public PlayerCharacterRecord.CharacterStatus Status
        {
            get => _currentHp <= 0 ? PlayerCharacterRecord.CharacterStatus.Dead : _status;
            set => _status = value;
        }

        [DataMember] public int Strength { get; set; }
        [IgnoreDataMember] private int _currentHp;
        [IgnoreDataMember] private PlayerCharacterRecord.CharacterStatus _status;

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public int GetMaximumMp(PlayerCharacterRecord.CharacterClass characterClass)
        {
            // this differs from Ultima IV, and is far more conservative
            switch (characterClass)
            {
                case PlayerCharacterRecord.CharacterClass.Avatar:
                case PlayerCharacterRecord.CharacterClass.Mage:
                    return Intelligence;
                case PlayerCharacterRecord.CharacterClass.Bard:
                    return Intelligence / 2;
                case PlayerCharacterRecord.CharacterClass.Fighter:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(characterClass), characterClass, null);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public int Heal()
        {
            int nCurrentHp = CurrentHp;
            CurrentHp = MaximumHp;
            return MaximumHp - nCurrentHp;
        }

        public bool Poison()
        {
            if (Status == PlayerCharacterRecord.CharacterStatus.Dead) return false;
            Debug.Assert(Status is PlayerCharacterRecord.CharacterStatus.Good
                or PlayerCharacterRecord.CharacterStatus.Poisoned);
            Status = PlayerCharacterRecord.CharacterStatus.Poisoned;
            return true;
        }

        public int ProcessTurnAcid()
        {
            int nDamageAmount = Utils.GetNumberFromAndTo(OddsAndLogic.ACID_DAMAGE_MIN, OddsAndLogic.ACID_DAMAGE_MAX);
            CurrentHp -= nDamageAmount;
            return nDamageAmount;
        }

        public void ProcessTurnBomb()
        {
            CurrentHp -= Utils.GetNumberFromAndTo(OddsAndLogic.BOMB_DAMAGE_MIN, OddsAndLogic.BOMB_DAMAGE_MAX);
        }

        public void ProcessTurnElectric()
        {
            CurrentHp -= Utils.GetNumberFromAndTo(OddsAndLogic.BOMB_DAMAGE_MIN, OddsAndLogic.BOMB_DAMAGE_MAX);
        }

        public int ProcessTurnPoison()
        {
            if (Status != PlayerCharacterRecord.CharacterStatus.Poisoned) return 0;

            CurrentHp -= OddsAndLogic.POISON_DAMAGE_MIN;
            return OddsAndLogic.POISON_DAMAGE_MIN;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public bool Resurrect()
        {
            if (Status != PlayerCharacterRecord.CharacterStatus.Dead) return false;
            Status = PlayerCharacterRecord.CharacterStatus.Good;
            CurrentHp = MaximumHp;
            return true;
        }

        public bool Sleep()
        {
            if (Status is PlayerCharacterRecord.CharacterStatus.Dead
                or PlayerCharacterRecord.CharacterStatus.Poisoned) return false;
            Status = PlayerCharacterRecord.CharacterStatus.Asleep;
            return true;
        }
    }
}