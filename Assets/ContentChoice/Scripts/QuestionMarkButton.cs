using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuestionMarkButton : MonoBehaviour
{
    [SerializeField] private SoundController soundController;
    [SerializeField] private GameObject _modalBlocker;

    public Button questionMarkButton;

    public void OnQuestionMarkButtonUp()
    {
        questionMarkButton.GetComponent<RectTransform>().localScale = Vector3.one;
    }

    public void OnQuestionMarkButtonDown()
    {
        questionMarkButton.GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySound());
    }

    private IEnumerator PlaySound()
    {
        soundController.PlayClickSound();
        yield return new WaitForSeconds(soundController.clickSound.length);
        _modalBlocker.SetActive(true);
    }
}
