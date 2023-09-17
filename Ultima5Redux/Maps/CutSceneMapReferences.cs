using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ultima5Redux.References;

namespace Ultima5Redux.Maps
{
    public class CutOrIntroSceneMapReferences
    {
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

            for (int i = 0; i < SingleCutOrIntroSceneMapReference.N_SCENES; i++)
            {
            }
        }
    }
}