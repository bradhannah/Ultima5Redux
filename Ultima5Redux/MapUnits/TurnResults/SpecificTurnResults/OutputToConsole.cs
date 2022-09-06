namespace Ultima5Redux.MapUnits.TurnResults.SpecificTurnResults
{
    public sealed class OutputToConsole : TurnResult, IOutputString
    {
        public OutputToConsole(string outputString, bool bUseArrow = true, bool bForceNewLine = true) : base(
            TurnResultType.OutputToConsole)
        {
            OutputString = outputString;
            UseArrow = bUseArrow;
            ForceNewLine = bForceNewLine;
        }

        public override string GetDebugString()
        {
            return $@"OutputString: {OutputString}
UseArrow: {UseArrow}
ForceNewLine: {ForceNewLine}";
        }

        public string OutputString { get; }
        public bool UseArrow { get; }
        public bool ForceNewLine { get; }
    }
}