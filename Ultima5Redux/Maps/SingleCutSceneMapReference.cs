using System.Collections.Generic;

namespace Ultima5Redux.Maps
{
    public class SingleCutOrIntroSceneMapReference
    {
        /// <summary>
        ///     Actual numbers of bytes the map data takes up in data file (including padding)
        /// </summary>
        public int N_MAP_COLS_PER_ROW => IsCutsceneMap ? 11 : 19;
        public int N_MAP_ROWS_PER_MAP => IsCutsceneMap ? 11 : 4;

        private static int GetActualBytesPerRow(CutOrIntroSceneMapType nMapType) =>
            GetIsCutsceneMap(nMapType) ? 16 : 32;

        public static int GetNBytesForMap(CutOrIntroSceneMapType nMapType) =>
            (GetIsCutsceneMap(nMapType) ? 11 : 4) * GetActualBytesPerRow(nMapType);

        public SingleCutOrIntroSceneMapReference(CutOrIntroSceneMapType nTheCutOrIntroSceneMapType,
            IReadOnlyList<byte> rawData)
        {
            TheCutOrIntroSceneMapType = nTheCutOrIntroSceneMapType;
            MapTiles = new byte[N_MAP_COLS_PER_ROW, N_MAP_ROWS_PER_MAP];
            int nActualBytesPerRow = GetActualBytesPerRow(TheCutOrIntroSceneMapType);

            for (int nRow = 0; nRow < N_MAP_ROWS_PER_MAP; nRow++)
            {
                for (int nCol = 0; nCol < N_MAP_COLS_PER_ROW; nCol++)
                {
                    byte tileByte = rawData[nRow * nActualBytesPerRow + nCol];
                    MapTiles[nCol, nRow] = tileByte;
                }
            }
        }

        public enum CutOrIntroSceneMapType
        {
            BlackthornInterrogation = 0, ShrineOfVirtueInterior, ShrineOfTheCodexInterior, LordBritishMirrorRoom,
            IntroEarthBedroom, IntroCircleOfStones, IntroShadowlordEncounter, IntroIolosHut
        }

        public static int GetMapDataOffset(CutOrIntroSceneMapType nMapType)
        {
            const int nTotalCutMaps = 4;
            if (GetIsCutsceneMap(nMapType))
                return (int)nMapType * GetNBytesForMap(nMapType);
            return GetNBytesForMap(CutOrIntroSceneMapType.BlackthornInterrogation) * nTotalCutMaps
                   + ((int)nMapType - nTotalCutMaps) * GetNBytesForMap(nMapType);
        }

        public CutOrIntroSceneMapType TheCutOrIntroSceneMapType { get; }

        public byte[,] MapTiles { get; }

        /// <summary>
        ///     See intro.cpp IntroController::drawMap from Xu4
        /// </summary>
        private enum CutSceneCommands
        {
            SetObjectPositionAndTile0 = 0, SetObjectPositionAndTile1 = 1, SetObjectPositionAndTile2 = 2,
            SetObjectPositionAndTile3 = 3, SetObjectPositionAndTile4 = 4, DeleteObject = 7,

            /// <summary>
            ///     Redraw intro map and objects, then go to sleep
            ///     Format: 8c
            ///     c = cycles to sleep
            /// </summary>
            RedrawIntroMapAndSleep = 8,
            JumpToStartOfScriptTable = 0xF
        }


        public bool IsCutsceneMap => GetIsCutsceneMap(TheCutOrIntroSceneMapType);

        public static bool GetIsCutsceneMap(CutOrIntroSceneMapType nMapType) => nMapType is
            CutOrIntroSceneMapType.BlackthornInterrogation
            or CutOrIntroSceneMapType.LordBritishMirrorRoom
            or CutOrIntroSceneMapType.ShrineOfVirtueInterior
            or CutOrIntroSceneMapType.ShrineOfTheCodexInterior;

        public byte[][] GetMap() {
            // oof - this is expensive!
            byte[][] map = Utils.Init2DByteArray(MapTiles.GetLength(0), MapTiles.GetLength(1));
            for (int i = 0; i < MapTiles.GetLength(0); i++) {
                for (int j = 0; j < MapTiles.GetLength(1); j++) {
                    map[i][j] = MapTiles[i, j];
                }
            }

            return map;
        }
    }
}