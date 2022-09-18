namespace Ultima5Redux
{
    public class StreamingOutputItem
    {
        public bool ForceNewLine { get; }
        public string Message { get; }
        public bool UseArrow { get; }

        public StreamingOutputItem(string message, bool bUseArrow = true, bool bForceNewLine = true)
        {
            Message = message;
            UseArrow = bUseArrow;
            ForceNewLine = bForceNewLine;
        }
    }
}