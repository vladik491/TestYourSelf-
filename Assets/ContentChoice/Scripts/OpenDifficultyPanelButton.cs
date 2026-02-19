using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OpenDifficultyPanelButton : MonoBehaviour
{
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private SoundController soundController;

    public void OnButtonDown()
    {
        GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySound());
    }

    public void OnButtonUp()
    {
        GetComponent<RectTransform>().localScale = Vector3.one;
    }

    private IEnumerator PlaySound()
    {
        soundController.SelectionOfCategories();
        yield return new WaitForSeconds(soundController.clickSound.length);

        difficultyPanel.SetActive(true);
    }
}