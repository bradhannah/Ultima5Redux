using System.Runtime.Serialization;

namespace Ultima5Redux
{
    [DataContract] public class GameOverrides
    {
        public enum LockPickingOverrides { None, AlwaysSucceed, AlwaysFail }

        /// <summary>
        ///     Force lock picking overrides
        /// </summary>
        [DataMember]
        public LockPickingOverrides DebugTheLockPickingOverrides { get; set; }

        [DataMember] public bool PreferenceFreedPeopleDontDie { get; set; } = true;
    }
}