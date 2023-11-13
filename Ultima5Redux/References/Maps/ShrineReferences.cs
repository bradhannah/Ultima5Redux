using System.Collections.Generic;
using System.Linq;

namespace Ultima5Redux.References.Maps
{
    public class ShrineReferences
    {
        private readonly Dictionary<VirtueReference.VirtueType, ShrineReference> _shrineReferences = new();
        private readonly Dictionary<Point2D, ShrineReference> _shrineReferencesByPosition = new();

        public ShrineReference GetShrineReferenceByVirtue(VirtueReference.VirtueType virtue) =>
            _shrineReferences[virtue];

        public static bool DoesUserInputMatchShrine(string userInput, VirtueReference.VirtueType virtueType) {
            if (userInput.Length < 4) return false;

            string cleanedUserInput = userInput.ToLower().Substring(0, 4);
            string virtueStr = virtueType.ToString().ToLower().Substring(0, 4);
            return cleanedUserInput == virtueStr;
        }

        public ShrineReference GetShrineReferenceByUserInput(string userInput) {
            KeyValuePair<VirtueReference.VirtueType, ShrineReference> shrineRefKvp =
                _shrineReferences.FirstOrDefault(s => DoesUserInputMatchShrine(userInput, s.Key));
            return shrineRefKvp.Value;
        }
        
        public ShrineReference GetShrineReferenceByPosition(Point2D position) =>
            _shrineReferencesByPosition.ContainsKey(position) ? _shrineReferencesByPosition[position] : null;

        public IEnumerable<ShrineReference> Shrines => _shrineReferences.Values;

        public ShrineReferences(DataOvlReference dataOvlRef) {
            foreach (VirtueReference virtue in GameReferences.Instance.VirtueReferences.Virtues) {
                var shrineReference = new ShrineReference(virtue,
                    new Point2D(
                        dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.SHRINE_X_COORDS).GetAsByteList()[
                            (int)virtue.Virtue],
                        dataOvlRef.GetDataChunk(DataOvlReference.DataChunkName.SHRINE_Y_COORDS).GetAsByteList()[
                            (int)virtue.Virtue]
                    )
                );
                _shrineReferences.Add(virtue.Virtue, shrineReference);
                _shrineReferencesByPosition.Add(shrineReference.Position, shrineReference);
            }
        }
    }
}