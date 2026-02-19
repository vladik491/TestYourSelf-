using UnityEngine;
using PlayerPrefs = RedefineYG.PlayerPrefs;

public class PanelDifficultyActivator : MonoBehaviour
{
    [Header("Панели сложности")]
    [Tooltip("Перетащите GameObject панели EasyLevel")]
    [SerializeField] private GameObject easyPanel;
    [Tooltip("Перетащите GameObject панели MediumLevel")]
    [SerializeField] private GameObject mediumPanel;
    [Tooltip("Перетащите GameObject панели DifficultLevel")]
    [SerializeField] private GameObject difficultPanel;

    [Header("Идентификатор контента")]
    [SerializeField] private string contentId;

    private void Start()
    {
        // ключ, где хранится выбранная сложность
        string prefKey = "SelectedDifficulty_" + (string.IsNullOrEmpty(contentId) ? "" : contentId);
        string savedDifficulty = PlayerPrefs.GetString(prefKey, "Лёгкий");

        ActivatePanelForDifficulty(savedDifficulty);
    }

    private void ActivatePanelForDifficulty(string difficulty)
    {
        // выключаем все сначала
        if (easyPanel != null) easyPanel.SetActive(false);
        if (mediumPanel != null) mediumPanel.SetActive(false);
        if (difficultPanel != null) difficultPanel.SetActive(false);

        // включаем нужную по строке, совпадающей со значениями в DifficultySelectionButton
        if (difficulty == "Лёгкий")
        {
            if (easyPanel != null) easyPanel.SetActive(true);
        }
        else if (difficulty == "Средний")
        {
            if (mediumPanel != null) mediumPanel.SetActive(true);
        }
        else if (difficulty == "Сложный")
        {
            if (difficultPanel != null) difficultPanel.SetActive(true);
        }
    }
}
