using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DungeonSlime.Services;

namespace DungeonSlime.Services
{
    public class HighScoreEntry
    {
        public string PlayerName { get; set; } = "Anonymous";
        public int Score { get; set; }
        public DifficultyMode Difficulty { get; set; }
        public DateTime Date { get; set; }
    }

    public class HighScoreData
    {
        public List<HighScoreEntry> EasyScores { get; set; } = new List<HighScoreEntry>();
        public List<HighScoreEntry> MediumScores { get; set; } = new List<HighScoreEntry>();
        public List<HighScoreEntry> HardScores { get; set; } = new List<HighScoreEntry>();
    }

    public static class HighScoreService
    {
        private const int MAX_SCORES_PER_DIFFICULTY = 10;
        private static readonly string SaveFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DungeonSlime",
            "highscores.json"
        );

        private static HighScoreData _highScores;

        static HighScoreService()
        {
            LoadHighScores();
        }

        /// <summary>
        /// Adds a new score and returns true if it's a high score.
        /// </summary>
        public static bool AddScore(int score, DifficultyMode difficulty, string playerName = "Anonymous")
        {
            var entry = new HighScoreEntry
            {
                PlayerName = playerName,
                Score = score,
                Difficulty = difficulty,
                Date = DateTime.Now
            };

            List<HighScoreEntry> targetList = GetScoreList(difficulty);
            
            // Add the new score
            targetList.Add(entry);
            
            // Sort by score (highest first)
            targetList.Sort((a, b) => b.Score.CompareTo(a.Score));
            
            // Keep only top 10
            if (targetList.Count > MAX_SCORES_PER_DIFFICULTY)
            {
                targetList.RemoveRange(MAX_SCORES_PER_DIFFICULTY, targetList.Count - MAX_SCORES_PER_DIFFICULTY);
            }

            // Check if the new score made it into the top 10
            bool isHighScore = targetList.Contains(entry);

            // Save the updated scores
            SaveHighScores();

            return isHighScore;
        }

        /// <summary>
        /// Gets the high scores for a specific difficulty.
        /// </summary>
        public static List<HighScoreEntry> GetHighScores(DifficultyMode difficulty)
        {
            return new List<HighScoreEntry>(GetScoreList(difficulty));
        }

        /// <summary>
        /// Gets all high scores for all difficulties.
        /// </summary>
        public static HighScoreData GetAllHighScores()
        {
            return _highScores;
        }

        /// <summary>
        /// Clears all high scores (for testing/reset purposes).
        /// </summary>
        public static void ClearHighScores()
        {
            _highScores = new HighScoreData();
            SaveHighScores();
        }

        private static List<HighScoreEntry> GetScoreList(DifficultyMode difficulty)
        {
            return difficulty switch
            {
                DifficultyMode.Easy => _highScores.EasyScores,
                DifficultyMode.Medium => _highScores.MediumScores,
                DifficultyMode.Hard => _highScores.HardScores,
                _ => _highScores.EasyScores
            };
        }

        private static void LoadHighScores()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    _highScores = JsonSerializer.Deserialize<HighScoreData>(json) ?? new HighScoreData();
                }
                else
                {
                    _highScores = new HighScoreData();
                }
            }
            catch (Exception)
            {
                // If loading fails, start with empty scores
                _highScores = new HighScoreData();
            }
        }

        private static void SaveHighScores()
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(SaveFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize and save
                string json = JsonSerializer.Serialize(_highScores, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception)
            {
                // Silently fail if we can't save (maybe no write permissions)
                // In a production game, you might want to log this error
            }
        }
    }
}
