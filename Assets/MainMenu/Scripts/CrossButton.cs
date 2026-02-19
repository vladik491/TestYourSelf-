using UnityEngine;
using System.Collections;

public class CrossButton : MonoBehaviour
{
    [SerializeField] private GameObject _cross;
    [SerializeField] private GameObject _modalBlocker;
    [SerializeField] private SoundController soundController;

    private RectTransform crossRect;

    private void Awake()
    {
        crossRect = _cross.GetComponent<RectTransform>(); 
    }

    public void OnCrossButtonUp()
    {
        if (crossRect != null) crossRect.localScale = Vector3.one;
    }

    public void OnCrossButtonDown()
    {
        if (crossRect != null) crossRect.localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundAndCloseSettings());
    }

    private IEnumerator PlaySoundAndCloseSettings()
    {
        soundController.PlayClickSound();
        yield return new WaitForSeconds(soundController.clickSound.length);
        _modalBlocker.SetActive(false);
        if (crossRect != null) crossRect.localScale = Vector3.one;
    }
}