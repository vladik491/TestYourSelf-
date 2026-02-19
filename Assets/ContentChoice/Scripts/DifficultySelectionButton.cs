using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using PlayerPrefs = RedefineYG.PlayerPrefs;

public class DifficultySelectionButton : MonoBehaviour
{
    [SerializeField] private TMP_Text easyDifficultyText;
    [SerializeField] private TMP_Text mediumDifficultyText;
    [SerializeField] private TMP_Text complexDifficultyText;
    [SerializeField] private SoundController soundController;
    [SerializeField] private GameObject difficultySelectionPanel;
    [SerializeField] private string contentId;

    private TMP_Text buttonText;

    private void Awake()
    {
        buttonText = GetComponentInChildren<TMP_Text>();
    }

    public void OnButtonDown()
    {
        GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundAndActivate());
    }

    public void OnButtonUp()
    {
        GetComponent<RectTransform>().localScale = Vector3.one;
    }

    public void OnButtonClick()
    {
        if (buttonText != null && easyDifficultyText != null && mediumDifficultyText != null && complexDifficultyText != null)
        {
            easyDifficultyText.gameObject.SetActive(false);
            mediumDifficultyText.gameObject.SetActive(false);
            complexDifficultyText.gameObject.SetActive(false);

            string currentText = buttonText.text;
            string prefKey = "SelectedDifficulty_" + contentId;
            if (currentText == "Лёгкий")
            {
                easyDifficultyText.gameObject.SetActive(true);
                PlayerPrefs.SetString(prefKey, "Лёгкий");
            }
            else if (currentText == "Средний")
            {
                mediumDifficultyText.gameObject.SetActive(true);
                PlayerPrefs.SetString(prefKey, "Средний");
            }
            else if (currentText == "Сложный")
            {
                complexDifficultyText.gameObject.SetActive(true);
                PlayerPrefs.SetString(prefKey, "Сложный");
            }
            PlayerPrefs.Save();

            if (difficultySelectionPanel != null)
            {
                difficultySelectionPanel.SetActive(false);
            }
        }
    }

    private IEnumerator PlaySoundAndActivate()
    {
        soundController.SelectionOfCategories();
        yield return new WaitForSeconds(soundController.clickSound.length);
    }
}