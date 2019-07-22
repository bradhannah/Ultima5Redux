using System.Collections.Generic;

namespace Ultima5Redux
{
    public class SomeStrings
    {
        public List<string> Strs { get;  }

        public SomeStrings(List<byte> byteArray, int offset, int length)
        {
            string str=string.Empty;
            Strs = new List<string>();
            int curOffset = offset;

            while (curOffset < length)
            {
                str = Utils.BytesToStringNullTerm(byteArray, curOffset, length-curOffset);

                Strs.Add(str);
                curOffset += str.Length + 1;
                str = string.Empty;
            }
        }

        public void PrintSomeStrings()
        {
            foreach (string str in Strs)
            {
                System.Console.WriteLine(str);
            }
        }
    }
}
