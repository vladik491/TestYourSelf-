using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using YG;
using PlayerPrefs = RedefineYG.PlayerPrefs;

public class FormatButtons : MonoBehaviour
{
    [SerializeField] private SoundController soundController;
    [SerializeField] private string sceneName;
    [SerializeField] private string contentId;
    private bool adInProgress = false;
    private bool transitionStarted = false;

    private const float MUSIC_FADE_DURATION = 0.6f;

    private void OnEnable()
    {
        YG2.onCloseInterAdv += OnAdClosed;
        YG2.onErrorInterAdv += OnAdError;
    }

    private void OnDisable() => Cleanup();

    public void OnButtonDown()
    {
        if (transitionStarted) return;

        GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundAndShowAd());
    }

    public void OnButtonUp()
    {
        GetComponent<RectTransform>().localScale = Vector3.one;
    }

    private IEnumerator PlaySoundAndShowAd()
    {
        if (soundController != null)
        {
            soundController.SelectionOfCategories();

            float wait = soundController.clickSound != null ? soundController.clickSound.length : 0f;
            yield return new WaitForSecondsRealtime(wait > 0f ? wait : 0.01f);
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.01f);
        }

        Debug.Log($"[AD] До рекламы: {YG2.timerInterAdv:F1} сек");

        if (adInProgress || transitionStarted) yield break;

        if (YG2.isTimerAdvCompleted && !YG2.nowInterAdv)
        {
            adInProgress = true;
            YG2.InterstitialAdvShow();
        }
        else
        {
            ContinueTransition();
        }
    }

    private void OnAdClosed()
    {
        if (adInProgress)
        {
            adInProgress = false;
            StartCoroutine(PostAdStabilizationAndContinue());
        }
    }

    private void OnAdError()
    {
        if (adInProgress)
        {
            adInProgress = false;
            ContinueTransition();
        }
    }

    // Корутина для стабилизации после рекламы
    private IEnumerator PostAdStabilizationAndContinue()
    {
        yield return null;
        yield return null;
        ContinueTransition();
    }

    private void ContinueTransition()
    {
        if (transitionStarted) return;
        transitionStarted = true;

        string id = string.IsNullOrEmpty(contentId) ? "DefaultContent" : contentId;
        PlayerPrefs.SetString("LastLoadedScene_" + id, sceneName);
        PlayerPrefs.Save();

        // 1. Музыка меню: Запуск плавного затухания
        if (MusicManager.Instance != null)
            MusicManager.Instance.StartTestMode(MUSIC_FADE_DURATION); // Используем плавный Fade

        Cleanup();

        // 2. Загрузка сцены (LoadingManager теперь отвечает только за экран)
        LoadingManager.Instance.LoadScene(sceneName);
    }

    private void Cleanup()
    {
        YG2.onCloseInterAdv -= OnAdClosed;
        YG2.onErrorInterAdv -= OnAdError;
    }
}