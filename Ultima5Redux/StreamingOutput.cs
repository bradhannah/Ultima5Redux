using System.Collections.Generic;

namespace Ultima5Redux
{
    public class StreamingOutput
    {
        private static StreamingOutput instance;

        public static StreamingOutput Instance => instance ??= new StreamingOutput();

        private Queue<StreamingOutputItem> OutputItems { get; } = new Queue<StreamingOutputItem>();

        public void PushMessage(string message, bool bUseArrow = true, bool bForceNewline = true)
            => PushOutputItem(new StreamingOutputItem(message, bUseArrow, bForceNewline));

        public StreamingOutputItem PopOutputItem()
        {
            return OutputItems.Dequeue();
        }

        public void PushOutputItem(StreamingOutputItem item)
        {
            OutputItems.Enqueue(item);
        }

        public bool HasItems => OutputItems.Count > 0;

        private bool _bLastMessageEndedWithNewline;

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
    }
}