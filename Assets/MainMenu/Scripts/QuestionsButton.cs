using UnityEngine;
using System.Collections;

public class QuestionsButton : MonoBehaviour
{
    [SerializeField] private GameObject _questions;
    [SerializeField] private GameObject _modalBlocker;
    [SerializeField] private SoundController soundController;

    public void OnQuestionsButtonUp()
    {
        _questions.GetComponent<RectTransform>().localScale = Vector3.one;
    }

    public void OnQuestionsButtonDown()
    {
        _questions.GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundAndQuit());
    }

    private IEnumerator PlaySoundAndQuit()
    {
        soundController.PlayClickSound();
        yield return new WaitForSeconds(soundController.clickSound.length);
        _modalBlocker.SetActive(true);
    }
}