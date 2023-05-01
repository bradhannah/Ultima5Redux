using System.Diagnostics.CodeAnalysis;
using Ultima5Redux.MapUnits.NonPlayerCharacters;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public sealed class NpcTalkInteraction : TurnResult, INonPlayerCharacterInteraction
    {
        // ReSharper disable once NotAccessedField.Global
        public readonly string CustomDialogueId = "";

        public NpcTalkInteraction(NonPlayerCharacter npc) : base(TurnResultType.NpcTalkInteraction,
            TurnResulActionType.ActionRequired) => Npc = npc;

        public NpcTalkInteraction(NonPlayerCharacter npc, string customDialogueId) : base(TurnResultType
            .NpcTalkInteraction, TurnResulActionType.ActionRequired)
        {
            CustomDialogueId = customDialogueId;
            Npc = npc;
        }

        public NonPlayerCharacter Npc { get; }
    }
}