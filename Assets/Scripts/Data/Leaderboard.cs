using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARSurvival.Data
{
    /// <summary>One finished round, as stored on the leaderboard.</summary>
    [Serializable]
    public class SessionResult
    {
        public int score;
        public int enemiesDefeated;
        public float timeSurvived;
        public bool survived;
        public string difficulty;
        public string playedAt; // ISO 8601, local time

        public DateTime PlayedAt => DateTime.TryParse(playedAt, out var t) ? t : DateTime.MinValue;
    }

    /// <summary>
    /// Local leaderboard of the latest 5 sessions (newest first), persisted as JSON in PlayerPrefs
    /// so it survives app restarts.
    /// </summary>
    public class Leaderboard
    {
        public const int MaxEntries = 5;
        const string PrefsKey = "ARSurvival.Leaderboard.v1";

        [Serializable]
        class SaveData { public List<SessionResult> entries = new(); }

        readonly List<SessionResult> entries;

        public IReadOnlyList<SessionResult> Entries => entries;

        public Leaderboard()
        {
            entries = Load();
        }

        public void Add(SessionResult result)
        {
            entries.Insert(0, result);
            if (entries.Count > MaxEntries)
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
            Save();
        }

        public void Clear()
        {
            entries.Clear();
            Save();
        }

        static List<SessionResult> Load()
        {
            var json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new List<SessionResult>();

            try
            {
                return JsonUtility.FromJson<SaveData>(json)?.entries ?? new List<SessionResult>();
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Leaderboard data was corrupt and has been reset.");
                return new List<SessionResult>();
            }
        }

        void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(new SaveData { entries = entries }));
            PlayerPrefs.Save();
        }
    }
}
