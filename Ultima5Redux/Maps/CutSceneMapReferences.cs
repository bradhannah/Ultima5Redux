using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ultima5Redux.References;

namespace Ultima5Redux.Maps
{
    public class CutOrIntroSceneMapReferences
    {
        private readonly Dictionary<SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType,
            SingleCutOrIntroSceneMapReference> _cutOrIntroScenes = new();

        public IEnumerable<SingleCutOrIntroSceneMapReference> CutScenes =>
            _cutOrIntroScenes.Where(f => f.Value.IsCutsceneMap).Select(f => f.Value);

        public IEnumerable<SingleCutOrIntroSceneMapReference> IntroScenes =>
            _cutOrIntroScenes.Where(f => !f.Value.IsCutsceneMap).Select(f => f.Value);

        public IEnumerable<SingleCutOrIntroSceneMapReference> AllScenes =>
            _cutOrIntroScenes.Select(f => f.Value);

        public CutOrIntroSceneMapReferences(string legacyDataDatFilePath)
        {
            byte[] cutOrIntroSceneContents;
            string miscMapsFilename = Path.Combine(legacyDataDatFilePath, FileConstants.MISCMAPS_DAT);
            try
            {
                cutOrIntroSceneContents = File.ReadAllBytes(miscMapsFilename);
            } catch (Exception e)
            {
                throw new Ultima5ReduxException("Error opening and reading MISCMAPS.DAT\n" + e);
            }

            List<byte> cutOrIntroSceneListContents = cutOrIntroSceneContents.ToList();

            foreach (SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType cutOrIntroSceneMapTypes in Enum.GetValues(
                         typeof(SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType)))
            {
                int nOffset = SingleCutOrIntroSceneMapReference.GetMapDataOffset(cutOrIntroSceneMapTypes);
                int bBytesPerMap = SingleCutOrIntroSceneMapReference.GetNBytesForMap(cutOrIntroSceneMapTypes);
                var singleCutOrIntroSceneMapReference = new SingleCutOrIntroSceneMapReference(cutOrIntroSceneMapTypes,
                    cutOrIntroSceneListContents.GetRange(nOffset, bBytesPerMap));
                _cutOrIntroScenes.Add(cutOrIntroSceneMapTypes, singleCutOrIntroSceneMapReference);
            }
        }

        public SingleCutOrIntroSceneMapReference GetSingleCutOrIntroSceneMapReference(
            SingleCutOrIntroSceneMapReference.CutOrIntroSceneMapType nMapType) =>
            _cutOrIntroScenes[nMapType];
    }
}