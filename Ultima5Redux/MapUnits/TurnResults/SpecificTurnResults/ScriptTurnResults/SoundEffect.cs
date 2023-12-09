using System;
using Ultima5Redux.Maps;

namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults.ScriptTurnResults
{
    public class SoundEffect : CutOrIntroSceneScriptLineTurnResult
    {
        public enum SoundEffectType { WalkOnGrass, DaaaaDoooo, HighPitchedYay }

        public SoundEffectType TheSoundEffectType { get; private set; }

        public SoundEffect(CutOrIntroSceneScriptLine scriptLine) : base(
            TurnResultType.Script_SoundEffect, TurnResulActionType.ActionRequired, scriptLine) =>
            TheSoundEffectType = (SoundEffectType)Enum.Parse(typeof(SoundEffectType), scriptLine.StrParam);
    }
}