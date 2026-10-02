using System;
using ARSurvival.Core;
using ARSurvival.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARSurvival.UI
{
    /// <summary>Overlay listing the latest 5 sessions, newest first.</summary>
    public class LeaderboardScreen : UIScreen
    {
        [Serializable]
        class Row
        {
            public GameObject root;
            public TMP_Text date;
            public TMP_Text mode;
            public TMP_Text score;
            public TMP_Text kills;
            public TMP_Text time;
        }

        [SerializeField] Row[] rows;
        [SerializeField] TMP_Text emptyText;
        [SerializeField] Button closeButton;

        protected override void Awake()
        {
            base.Awake();
            Bind(closeButton, Hide);
        }

        protected override void OnShow()
        {
            var entries = GameManager.Instance.Leaderboard.Entries;
            emptyText.gameObject.SetActive(entries.Count == 0);

            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                bool hasEntry = i < entries.Count;
                row.root.SetActive(hasEntry);
                if (hasEntry)
                    Fill(row, entries[i]);
            }
        }

        static void Fill(Row row, SessionResult entry)
        {
            row.date.text = entry.PlayedAt == DateTime.MinValue ? "-" : entry.PlayedAt.ToString("dd MMM HH:mm");
            row.mode.text = entry.difficulty;
            row.score.text = entry.score.ToString();
            row.kills.text = entry.enemiesDefeated.ToString();
            row.time.text = UIStyle.FormatTime(entry.timeSurvived);
            row.time.color = entry.survived ? UIStyle.Accent : UIStyle.TextPrimary;
        }
    }
}
