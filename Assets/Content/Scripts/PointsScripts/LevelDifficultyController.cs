using UnityEngine;

public class LevelDifficultyController : MonoBehaviour
{
    public static LevelDifficultyController Instance { get; private set; }

    public enum Difficulty { Easy, Medium, Hard }

    public Difficulty CurrentDifficulty = Difficulty.Easy;

    // ƒобавл€ем текущий content id (должен устанавливатьс€ при выборе теста)
    public string CurrentContentId = "global_default";

    private const int BASE_EASY = 5;
    private const int BASE_MEDIUM = 15;
    private const int BASE_HARD = 40;

    // multipliers: 1-й, 2-й, 3-й, 4+
    private readonly float[] dayMultipliers = new float[] { 1f, 0.6f, 0.3f, 0.1f };

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        Instance = this;
    }

    public int BasePointsForDifficulty(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Medium: return BASE_MEDIUM;
            case Difficulty.Hard: return BASE_HARD;
            default: return BASE_EASY;
        }
    }

    // ¬ызываетс€ при выигрыше уровн€
    public void AwardPointsForWin()
    {
        // гарантируем, что трекер существует
        if (DailyRunTracker.Instance == null)
        {
            var go = new GameObject("DailyRunTracker_Auto");
            go.hideFlags = HideFlags.DontSaveInBuild;
            go.AddComponent<DailyRunTracker>();
        }

        var tracker = DailyRunTracker.Instance;

        int basePoints = BasePointsForDifficulty(CurrentDifficulty);

        int prevCountToday = 0;
        if (tracker != null)
            prevCountToday = tracker.GetCompletionCount(CurrentDifficulty, CurrentContentId);

        int attemptIndex = prevCountToday; // prevCountToday = сколько уже было до этого выигрыша
        float mult = dayMultipliers[Mathf.Clamp(attemptIndex, 0, dayMultipliers.Length - 1)];
        int awarded = Mathf.RoundToInt(basePoints * mult);
        if (awarded < 1) awarded = 1;

        PointsManager.AddPoints(awarded);

        // увеличиваем счЄтчик завершений сегодн€ дл€ конкретного контента + сложности
        tracker?.IncrementCompletion(CurrentDifficulty, CurrentContentId);

        var attemptCounter = Object.FindFirstObjectByType<AttemptCounter>();
        if (attemptCounter != null)
            attemptCounter.UpdateFromTracker();
    }
}
