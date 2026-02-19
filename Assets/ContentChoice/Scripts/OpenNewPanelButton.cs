using UnityEngine;
using System.Collections;

public class OpenNewPanelButton : MonoBehaviour
{
    [SerializeField] private GameObject OuterPanel;
    [SerializeField] private GameObject ScrollAndPage;
    [SerializeField] private GameObject NewPanel;
    [SerializeField] private GameObject GoBackButton;
    [SerializeField] private SoundController soundController;
    [SerializeField] private GameObject difficultySelectionPanel;

    private RectTransform buttonRect; // Кэшируем

    private void Awake()
    {
        buttonRect = GetComponent<RectTransform>();
    }

    public void OnButtonDown()
    {
        buttonRect.localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundAndActivate());
    }

    public void OnButtonUp()
    {
        buttonRect.localScale = Vector3.one;
    }

    private IEnumerator PlaySoundAndActivate()
    {
        soundController.SelectionOfCategories();
        yield return new WaitForSeconds(soundController.clickSound.length);

        // Восстанавливаем масштаб перед активацией
        buttonRect.localScale = Vector3.one;

        difficultySelectionPanel.SetActive(false);
        OuterPanel.SetActive(false);
        ScrollAndPage.SetActive(false);
        NewPanel.SetActive(true);
        GoBackButton.SetActive(true);
    }
}