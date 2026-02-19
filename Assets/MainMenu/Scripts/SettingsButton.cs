using UnityEngine;
using System.Collections;

public class SettingsButton : MonoBehaviour
{
    [SerializeField] private GameObject _settings;
    [SerializeField] private GameObject _modalBlocker;
    [SerializeField] private SoundController soundController; 

    public void OnSettingsButtonUp()
    {
        _settings.GetComponent<RectTransform>().localScale = Vector3.one;
    }

    public void OnSettingsButtonDown()
    {
        _settings.GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundAndOpenSettings());
    }

    private IEnumerator PlaySoundAndOpenSettings()
    {
        soundController.PlayClickSound();
        yield return new WaitForSeconds(soundController.clickSound.length);
        _modalBlocker.SetActive(true);
    }
}