using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class OutputModalText : CutOrIntroSceneScriptLineTurnResult {
        public OutputModalText(CutOrIntroSceneScriptLine scriptLine) : base(TurnResultType.Script_OutputModalText,
            TurnResulActionType.ActionRequired, scriptLine) {
        }

        public string Text {
            get {
                string strParam = ScriptLine.StrParam;
                if (strParam.Contains("/")) {
                    return strParam.Split('/')[0];
                }

                return strParam;
            }
        }

        public string ButtonText {
            get {
                string strParam = ScriptLine.StrParam;
                if (strParam.Contains("/")) {
                    return strParam.Split('/')[1];
                }

                return "OK";
            }
        }
    }
}