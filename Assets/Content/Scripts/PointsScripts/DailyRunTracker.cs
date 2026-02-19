using System;
using System.Collections.Generic;
using UnityEngine;
using YG;
using PlayerPrefs = RedefineYG.PlayerPrefs;

public class DailyRunTracker : MonoBehaviour
{
    public static DailyRunTracker Instance { get; private set; }

    [Header("НАСТРОЙКИ ТЕСТИРОВАНИЯ")]
    public bool debugResetProgress = false;

    private const string PrefsDateKey = "DailyRunTracker_Date";
    private const string PrefsTrackedKey = "DailyRunTracker_Tracked";
    private string currentDateString;

    private Dictionary<string, Dictionary<LevelDifficultyController.Difficulty, int>> counts =
        new Dictionary<string, Dictionary<LevelDifficultyController.Difficulty, int>>(StringComparer.Ordinal);

    // Получение даты: приоритет серверу Яндекса, иначе время устройства
    private string GetServerDateString()
    {
        long sTime = YG2.ServerTime();
        if (sTime > 0)
            return DateTimeOffset.FromUnixTimeMilliseconds(sTime).UtcDateTime.ToString("yyyy-MM-dd");
        return DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // --- БЛОК СБРОСА ПРОГРЕССА (ДЛЯ ТЕСТОВ) ---
        if (debugResetProgress)
        {
            Debug.LogWarning("!!! ВНИМАНИЕ: Включен режим СБРОСА ПРОГРЕССА. Удаляем все сохранения...");
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            PointsManager.ResetPoints();
        }

        PointsManager.Init();
        currentDateString = PlayerPrefs.GetString(PrefsDateKey, "");

        // Подписка на готовность SDK для актуализации даты
        YG2.onGetSDKData += EnsureDate;
        EnsureDate();
        LoadProgressFromPrefs();
    }

    void OnDestroy() => YG2.onGetSDKData -= EnsureDate;

    // Проверка смены суток и очистка данных
    public void EnsureDate()
    {
        string now = GetServerDateString();
        if (now != currentDateString)
        {
            // Полное удаление старых записей попыток
            string tracked = PlayerPrefs.GetString(PrefsTrackedKey, "");
            if (!string.IsNullOrEmpty(tracked))
            {
                foreach (var id in tracked.Split('|'))
                {
                    PlayerPrefs.DeleteKey(GetPrefsKey(id, LevelDifficultyController.Difficulty.Easy));
                    PlayerPrefs.DeleteKey(GetPrefsKey(id, LevelDifficultyController.Difficulty.Medium));
                    PlayerPrefs.DeleteKey(GetPrefsKey(id, LevelDifficultyController.Difficulty.Hard));
                }
            }

            currentDateString = now;
            counts.Clear();
            PlayerPrefs.SetString(PrefsDateKey, currentDateString);
            PlayerPrefs.SetString(PrefsTrackedKey, "");
            PlayerPrefs.Save();

            UpdateVisualCounter();
        }
    }

    private void LoadProgressFromPrefs()
    {
        string tracked = PlayerPrefs.GetString(PrefsTrackedKey, "");
        if (!string.IsNullOrEmpty(tracked))
        {
            foreach (var id in tracked.Split('|'))
                if (!string.IsNullOrEmpty(id) && !counts.ContainsKey(id)) EnsureContentEntry(id);
        }
    }

    private string GetPrefsKey(string cid, LevelDifficultyController.Difficulty d) => $"DailyRun_{cid}_{d}";

    private void EnsureContentEntry(string contentId)
    {
        if (string.IsNullOrEmpty(contentId)) contentId = "global_default";
        if (!counts.ContainsKey(contentId))
        {
            counts[contentId] = new Dictionary<LevelDifficultyController.Difficulty, int>()
            {
                { LevelDifficultyController.Difficulty.Easy, PlayerPrefs.GetInt(GetPrefsKey(contentId, LevelDifficultyController.Difficulty.Easy), 0) },
                { LevelDifficultyController.Difficulty.Medium, PlayerPrefs.GetInt(GetPrefsKey(contentId, LevelDifficultyController.Difficulty.Medium), 0) },
                { LevelDifficultyController.Difficulty.Hard, PlayerPrefs.GetInt(GetPrefsKey(contentId, LevelDifficultyController.Difficulty.Hard), 0) }
            };
        }
    }

    public int GetCompletionCount(LevelDifficultyController.Difficulty difficulty, string contentId)
    {
        EnsureDate();
        EnsureContentEntry(contentId);
        return counts[contentId][difficulty];
    }

    public void IncrementCompletion(LevelDifficultyController.Difficulty difficulty, string contentId)
    {
        EnsureDate();
        EnsureContentEntry(contentId);

        counts[contentId][difficulty]++;
        PlayerPrefs.SetInt(GetPrefsKey(contentId, difficulty), counts[contentId][difficulty]);

        string tracked = PlayerPrefs.GetString(PrefsTrackedKey, "");
        if (!tracked.Contains(contentId))
            PlayerPrefs.SetString(PrefsTrackedKey, (tracked + "|" + contentId).Trim('|'));

        PlayerPrefs.Save();
    }

    public void UpdateVisualCounter()
    {
        AttemptCounter ui = UnityEngine.Object.FindFirstObjectByType<AttemptCounter>();
        if (ui != null) ui.UpdateFromTracker();
    }
}