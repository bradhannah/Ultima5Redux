using System;
using Ultima5Redux.Maps;
using Ultima5Redux.References.Maps;
using Ultima5Redux.State;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class ChangeShrineState : CutOrIntroSceneScriptLineTurnResult {
        public ShrineState.ShrineStatus TheNewShrineStatus;
        public ShrineReference TheShrineReference;

        public ChangeShrineState(CutOrIntroSceneScriptLine scriptLine,
            ShrineReference theShrineReference) : base(TurnResultType.Script_ChangeShrineState,
            TurnResulActionType.ActionRequired, scriptLine) {
            TheShrineReference = theShrineReference;

            TheNewShrineStatus =
                (ShrineState.ShrineStatus)Enum.Parse(typeof(ShrineState.ShrineStatus), scriptLine.StrParam);
        }
    }
}