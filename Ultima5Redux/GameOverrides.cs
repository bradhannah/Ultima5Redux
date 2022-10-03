using System.Runtime.Serialization;

namespace Ultima5Redux
{
    [DataContract] public class GameOverrides
    {
        public enum LockPickingOverrides { None, AlwaysSucceed, AlwaysFail }

        [DataMember] public LockPickingOverrides TheLockPickingOverrides { get; set; }
    }
}