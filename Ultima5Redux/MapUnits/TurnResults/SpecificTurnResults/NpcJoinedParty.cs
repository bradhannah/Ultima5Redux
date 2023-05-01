using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.References.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class NpcJoinedParty : TurnResult
    {
        public NonPlayerCharacterReference NpcReference { get; }

        public NpcJoinedParty(NonPlayerCharacterReference npcReference) : base(TurnResultType.NpcJoinedParty,
            TurnResulActionType.ActionAlreadyPerformed) =>
            NpcReference = npcReference;
    }
}