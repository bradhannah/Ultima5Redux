using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ultima5Redux.Data;
using Ultima5Redux.References;

namespace Ultima5Redux.Maps
{
    public class Signs
    {
        /// <summary>
        ///     Total number of expected signs in file
        /// </summary>
        private const short TOTAL_SIGNS = 0x21;

        /// <summary>
        ///     List of all assembled signs
        /// </summary>
        private readonly List<Sign> _signList = new List<Sign>(TOTAL_SIGNS);

        /// <summary>
        ///     Loads the "Look" descriptions
        /// </summary>
        /// <param name="u5directory">directory of data files</param>
        public Signs(string u5directory)
        {
            List<byte> signsByteArray = Utils.GetFileAsByteList(Path.Combine(u5directory, FileConstants.SIGNS_DAT));

            int nIndex = TOTAL_SIGNS * 2;
            // we are ignoring the "offsets" which are likely used to help optimize the lookup 
            // on older hardware, instead we will just be lazy and search for them by cycling
            // through the whole list
            do
            {
                string rawSignTxt = Utils.BytesToStringNullTerm(signsByteArray, nIndex + 4, 0xFF);
                int nRawSignTxtLength = rawSignTxt.Length;

                // there are often two "warning signs" in the main virtue townes. Only one of the signs text is actually 
                // populated - so if we see a "\n" as the only string, then we look ahead to the next signs text and use
                // it instead
                if (rawSignTxt.Trim() == string.Empty)
                {
                    int nNextSignAdjust = rawSignTxt.Length + 1 + 4;
                    rawSignTxt = Utils.BytesToStringNullTerm(signsByteArray, nIndex + 4 + nNextSignAdjust, 0xFF);
                }

                _signList.Add(new Sign((SmallMapReferences.SingleMapReference.Location)signsByteArray[nIndex],
                    signsByteArray[nIndex + 1], signsByteArray[nIndex + 2], signsByteArray[nIndex + 3], rawSignTxt,
                    nIndex));
                nIndex += nRawSignTxtLength + 1 +
                          4; // we hop over the string plus it's null byte plus the four bytes for definition
                // while we don't encounter four zero bytes in a row, which is essentially the end of the file
            } while (!(signsByteArray[nIndex] == 0 && signsByteArray[nIndex + 1] == 0 &&
                       signsByteArray[nIndex + 2] == 0 && signsByteArray[nIndex + 3] == 0));

            // there are some signs that are not included in the signs.dat file, so we manually pont to them and add them to our sign list
            List<byte> dataOvlSignsByteArray =
                Utils.GetFileAsByteList(Path.Combine(u5directory, FileConstants.DATA_OVL));
            List<byte> shSign = DataChunk.CreateDataChunk(DataChunk.DataFormatType.ByteList, "SH Sign of Eight Laws",
                dataOvlSignsByteArray, 0x743a, 0x66).GetAsByteList();
            _signList.Add(new Sign(SmallMapReferences.SingleMapReference.Location.Serpents_Hold, 0, 15, 19,
                shSign.ToArray(), 0x743a));
        }

        public Sign GetSign(int nSign)
        {
            return _signList[nSign];
        }

        public Sign GetSign(SmallMapReferences.SingleMapReference.Location location, int x, int y)
        {
            return _signList.FirstOrDefault(sign => sign.X == x && sign.Y == y && sign.Location == location);
        }
    }
}