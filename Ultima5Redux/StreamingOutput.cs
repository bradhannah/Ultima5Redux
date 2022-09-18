using System.Collections.Generic;

namespace Ultima5Redux
{
    public class StreamingOutput
    {
        private static StreamingOutput _instance;

        private bool _bLastMessageEndedWithNewline;

        private Queue<StreamingOutputItem> OutputItems { get; } = new();

        public static StreamingOutput Instance => _instance ??= new StreamingOutput();

        public bool HasItems => OutputItems.Count > 0;

        public string GetFinalString(StreamingOutputItem item)
        {
            string outputStr = item.Message.Trim();
            if (item.UseArrow)
            {
                string addStr = _bLastMessageEndedWithNewline ? "\n" : "\n\n";
                outputStr = addStr + "> " + outputStr;
            }

            if (item.ForceNewLine) outputStr += "\n";

            _bLastMessageEndedWithNewline = outputStr.EndsWith("\n");
            return outputStr;
        }

        public StreamingOutputItem PopOutputItem() => OutputItems.Dequeue();

        public void PushMessage(string message, bool bUseArrow = true, bool bForceNewline = true)
        {
            PushOutputItem(new StreamingOutputItem(message, bUseArrow, bForceNewline));
        }

        public void PushOutputItem(StreamingOutputItem item)
        {
            OutputItems.Enqueue(item);
        }
    }
}