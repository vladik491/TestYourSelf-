using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CrossForQuestionMarkButton : MonoBehaviour
{
    [SerializeField] private Button _cross;
    [SerializeField] private GameObject _modalBlocker;
    [SerializeField] private SoundController soundController;

    private RectTransform crossRect;

    private void Awake()
    {
        crossRect = _cross.GetComponent<RectTransform>();
    }

    public void OnCrossButtonUp()
    {
        _cross.GetComponent<RectTransform>().localScale = Vector3.one;
    }

    public void OnCrossButtonDown()
    {
        _cross.GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundAndCloseQuestionMark());
    }

    private IEnumerator PlaySoundAndCloseQuestionMark()
    {
        soundController.PlayClickSound();
        yield return new WaitForSeconds(soundController.clickSound.length);
        _modalBlocker.SetActive(false);
        if (crossRect != null) crossRect.localScale = Vector3.one;
    }
}
