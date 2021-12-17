using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Ultima5Redux.References;

namespace Ultima5Redux
{
    public class SaveGameSummaries
    {
        public IEnumerable<GameSummary> GameStateBases => _gameStates;

        private readonly List<GameSummary> _gameStates = new();

        /// <summary>
        /// </summary>
        /// <param name="legacyDataPath"></param>
        /// <param name="parentSavePath">the path of all the save files (typically Documents/UltimaVRedux</param>
        public void Initialize(string legacyDataPath, string parentSavePath)
        {
            if (!GameReferences.IsInitialized)
            {
                GameReferences.Initialize(legacyDataPath);
            }

            _gameStates.Clear();

            if (!Directory.Exists(parentSavePath))
            {
                Directory.CreateDirectory(parentSavePath);
                return;
            }

            foreach (string directory in Directory.EnumerateDirectories(parentSavePath))
            {
                string summaryFileAndPath = Path.Combine(directory, FileConstants.NEW_SAVE_SUMMARY_FILE);
                try
                {
                    if (File.Exists(summaryFileAndPath))
                    {
                        string summaryJson = File.ReadAllText(summaryFileAndPath);
                        GameSummary gameStateSummary = GameSummary.DeserializeGameSummary(summaryJson);
                        _gameStates.Add(gameStateSummary);
                    }
                } catch (Exception e)
                {
                    // we don't load it, and simply skip it
                    Debug.WriteLine("Error: Couldn't load " + summaryFileAndPath);
                }
            }
        }
    }
}