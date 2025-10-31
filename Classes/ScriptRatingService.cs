using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ScriptRunner
{
    public class ScriptRatingService
    {
        private readonly string _ratingsFilePath;
        private Dictionary<string, int> _ratings;

        public ScriptRatingService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string scriptRunnerDir = Path.Combine(appDataPath, "ScriptRunner");
            Directory.CreateDirectory(scriptRunnerDir);
            _ratingsFilePath = Path.Combine(scriptRunnerDir, "script_ratings.json");
            LoadRatings();
        }

        private void LoadRatings()
        {
            try
            {
                if (File.Exists(_ratingsFilePath))
                {
                    string json = File.ReadAllText(_ratingsFilePath);
                    _ratings = JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
                }
                else
                {
                    _ratings = new Dictionary<string, int>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading ratings: {ex.Message}");
                _ratings = new Dictionary<string, int>();
            }
        }

        private void SaveRatings()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_ratings, Formatting.Indented);
                File.WriteAllText(_ratingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving ratings: {ex.Message}");
            }
        }

        public int GetRating(string scriptPath)
        {
            if (string.IsNullOrEmpty(scriptPath))
                return 0;

            return _ratings.TryGetValue(scriptPath, out int rating) ? rating : 0;
        }

        public void SetRating(string scriptPath, int rating)
        {
            if (string.IsNullOrEmpty(scriptPath))
                return;

            if (rating < 0) rating = 0;
            if (rating > 5) rating = 5;

            _ratings[scriptPath] = rating;
            SaveRatings();
        }

        public void LoadRatingsForItems(IEnumerable<FileSystemItem> items)
        {
            foreach (var item in items)
            {
                if (!item.IsDirectory)
                {
                    item.Rating = GetRating(item.FullPath);
                }

                if (item.Children?.Count > 0)
                {
                    LoadRatingsForItems(item.Children);
                }
            }
        }
    }
}