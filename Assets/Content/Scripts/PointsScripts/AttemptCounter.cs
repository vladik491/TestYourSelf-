using UnityEngine;
using TMPro;

public class AttemptCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text attemptText;
    private int attempts = 0;

    void Start()
    {
        if (DailyRunTracker.Instance != null)
            UpdateFromTracker();
    }

    // Обновление UI значениями из трекера
    public void UpdateFromTracker()
    {
        if (LevelDifficultyController.Instance != null && DailyRunTracker.Instance != null)
        {
            string cid = LevelDifficultyController.Instance.CurrentContentId;
            var diff = LevelDifficultyController.Instance.CurrentDifficulty;

            attempts = DailyRunTracker.Instance.GetCompletionCount(diff, cid);
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (attemptText != null)
            attemptText.text = attempts.ToString();
    }
}