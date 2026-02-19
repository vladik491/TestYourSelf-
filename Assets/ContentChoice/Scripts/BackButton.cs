using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using YG;

public class BackButton : MonoBehaviour
{
    [SerializeField] private GameObject backButton;
    [SerializeField] private SoundController soundController;
    private bool adInProgress = false;

    private void OnEnable()
    {
        YG2.onCloseInterAdv += OnAdClosed;
        YG2.onErrorInterAdv += OnAdError;
    }

    private void OnDisable() => Cleanup();

    public void OnBackButtonUp() => backButton.GetComponent<RectTransform>().localScale = Vector3.one;

    public void OnBackButtonDown()
    {
        backButton.GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundAndShowAd());
    }

    private IEnumerator PlaySoundAndShowAd()
    {
        soundController.PlayClickSound();
        yield return new WaitForSeconds(soundController.clickSound.length);

        Debug.Log($"[AD] До рекламы: {YG2.timerInterAdv:F1} сек");

        if (adInProgress) yield break;

        if (YG2.isTimerAdvCompleted && !YG2.nowInterAdv)
        {
            adInProgress = true;
            YG2.InterstitialAdvShow();
        }
        else
        {
            LoadMenu();
        }
    }

    private void OnAdClosed()
    {
        if (adInProgress) { LoadMenu(); adInProgress = false; }
    }

    private void OnAdError()
    {
        if (adInProgress) { LoadMenu(); adInProgress = false; }
    }

    private void LoadMenu()
    {
        Cleanup();
        LoadingManager.Instance.LoadScene(0);
    }

    private void Cleanup()
    {
        YG2.onCloseInterAdv -= OnAdClosed;
        YG2.onErrorInterAdv -= OnAdError;
    }
}