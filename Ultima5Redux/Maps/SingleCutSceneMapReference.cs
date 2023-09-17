using System.Collections.Generic;

namespace Ultima5Redux.Maps
{
    public class SingleCutOrIntroSceneMapReference
    {
        public int N_BYTES_PER_MAP => N_MAP_ROWS_PER_MAP * N_MAP_COLS_PER_ROW;
        public int N_MAP_COLS_PER_ROW => IsCutsceneMap ? 11 : 19;
        public int N_MAP_ROWS_PER_MAP => IsCutsceneMap ? 11 : 4;

        public SingleCutOrIntroSceneMapReference(CutOrIntroSceneMapTypes theCutOrIntroSceneMapType,
            IReadOnlyList<byte> rawData)
        {
            TheCutOrIntroSceneMapType = theCutOrIntroSceneMapType;
            MapTiles = new byte[N_MAP_ROWS_PER_MAP, N_MAP_COLS_PER_ROW];

            for (int nRow = 0; nRow < N_MAP_ROWS_PER_MAP; nRow++)
            {
                for (int nCol = 0; nCol < N_MAP_COLS_PER_ROW; nCol++)
                {
                    byte tileByte =
                        rawData[(int)theCutOrIntroSceneMapType * N_BYTES_PER_MAP * (nRow * N_MAP_COLS_PER_ROW + nCol)];
                    MapTiles[nCol, nRow] = tileByte;
                }
            }
        }

        public enum CutOrIntroSceneMapTypes
        {
            BlackthornInterrogation = 0, ShrineOfVirtueInterior, ShrineOfTheCodexInterior, LordBritishMirrorRoom,
            IntroEarthBedroom, IntroCircleOfStones, IntroShadowlordEncounter, IntroIolosHut
        }

        public CutOrIntroSceneMapTypes TheCutOrIntroSceneMapType { get; }

        public byte[,] MapTiles { get; }

        /// <summary>
        ///     See intro.cpp IntroController::drawMap from Xu4
        /// </summary>
        private enum CutSceneCommands
        {
            SetObjectPositionAndTile0 = 0, SetObjectPositionAndTile1 = 1, SetObjectPositionAndTile2 = 2,
            SetObjectPositionAndTile3 = 3, SetObjectPositionAndTile4 = 4, DeleteObject = 7,

            /* ----------------------------------------------
                   Redraw intro map and objects, then go to sleep
                   Format: 8c
                   c = cycles to sleep
                   ---------------------------------------------- */ RedrawIntroMapAndSleep = 8,
            JumpToStartOfScriptTable = 0xF
        }

        private bool IsCutsceneMap => TheCutOrIntroSceneMapType is
            CutOrIntroSceneMapTypes.BlackthornInterrogation
            or CutOrIntroSceneMapTypes.LordBritishMirrorRoom
            or CutOrIntroSceneMapTypes.ShrineOfVirtueInterior
            or CutOrIntroSceneMapTypes.ShrineOfTheCodexInterior;
    }
}