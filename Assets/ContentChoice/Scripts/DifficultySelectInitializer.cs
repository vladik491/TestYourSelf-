using UnityEngine;
using TMPro;
using PlayerPrefs = RedefineYG.PlayerPrefs;

public class DifficultySelectInitializer : MonoBehaviour
{
    [SerializeField] private TMP_Text easyDifficultyText;
    [SerializeField] private TMP_Text mediumDifficultyText;
    [SerializeField] private TMP_Text complexDifficultyText;
    [SerializeField] private string contentId;

    private void Start()
    {
        string prefKey = "SelectedDifficulty_" + contentId;
        string savedDifficulty = PlayerPrefs.GetString(prefKey, "Лёгкий");

        easyDifficultyText.gameObject.SetActive(false);
        mediumDifficultyText.gameObject.SetActive(false);
        complexDifficultyText.gameObject.SetActive(false);

        if (savedDifficulty == "Лёгкий")
        {
            easyDifficultyText.gameObject.SetActive(true);
        }
        else if (savedDifficulty == "Средний")
        {
            mediumDifficultyText.gameObject.SetActive(true);
        }
        else if (savedDifficulty == "Сложный")
        {
            complexDifficultyText.gameObject.SetActive(true);
        }
    }
}