using System;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults {
    public class ScreenEffect : CutOrIntroSceneScriptLineTurnResult {
        public enum ScreenEffectType { ShakeScreen, InvertColors }

        public bool StartScreenEffect { get; }

        public ScreenEffectType TheScreenEffectType { get; }
        
        public ScreenEffect(CutOrIntroSceneScriptLine scriptLine) : base(TurnResultType.Script_ScreenEffect,
            TurnResulActionType.ActionRequired, scriptLine) {
            TheScreenEffectType = (ScreenEffectType)Enum.Parse(typeof(ScreenEffectType), scriptLine.StrParam);
            StartScreenEffect = scriptLine.IntParam != 0;
        }
    }
}