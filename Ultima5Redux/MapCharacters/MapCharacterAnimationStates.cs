using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace Ultima5Redux
{

    public class MapCharacterAnimationStates
    {
        public enum MapCharacterAnimationStatesFiles { SAVED_GAM, BRIT_OOL, UNDER_OOL };


        private const int MAX_CHARACTER_STATES = 0x20;

        private List<MapCharacterAnimationState> characterStates = new List<MapCharacterAnimationState>(MAX_CHARACTER_STATES);

        private DataChunk overworldAnimationStatesDataChunk;
        private DataChunk underworldAnimationStatesDataChunk;
        private DataChunk animationStatesDataChunk;
        private TileReferences tileReferences;

        public MapCharacterAnimationState GetCharacterState(int nIndex)
        {
            return characterStates[nIndex];
        }

        public MapCharacterAnimationState GetCharacterStateByPosition(Point2D xy, int nFloor)
        {
            foreach (MapCharacterAnimationState characterState in characterStates)
            {
                if (characterState.X == xy.X && characterState.Y == xy.Y && characterState.Floor == nFloor)
                    return characterState;
            }
            return null;
        }

        public void Load(MapCharacterAnimationStatesFiles file)
        {
            DataChunk selectedDataChunk = null;

            switch (file)
            {
                case MapCharacterAnimationStatesFiles.SAVED_GAM:
                    selectedDataChunk = animationStatesDataChunk;
                    break;
                case MapCharacterAnimationStatesFiles.BRIT_OOL:
                    selectedDataChunk = overworldAnimationStatesDataChunk;
                    break;
                case MapCharacterAnimationStatesFiles.UNDER_OOL:
                    selectedDataChunk = underworldAnimationStatesDataChunk;
                    break;
            }

            List<byte> characterStateBytes = selectedDataChunk.GetAsByteList();

            for (int i = 0; i < MAX_CHARACTER_STATES; i++)
            {
                characterStates.Add(new MapCharacterAnimationState(tileReferences, characterStateBytes.GetRange(i * MapCharacterAnimationState.NBYTES, MapCharacterAnimationState.NBYTES).ToArray()));
            }
        }

        public MapCharacterAnimationStates(DataChunk animationStatesDataChunk, DataChunk overworldAnimationStatesDataChunk, DataChunk underworldAnimationStatesDataChunk,
            TileReferences tileReferences)
        {
            this.tileReferences = tileReferences;
            this.animationStatesDataChunk = animationStatesDataChunk;
            this.overworldAnimationStatesDataChunk = overworldAnimationStatesDataChunk;
            this.underworldAnimationStatesDataChunk = underworldAnimationStatesDataChunk;


           
        }

    }
}
