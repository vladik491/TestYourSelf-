using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

public class ToContentSelection : MonoBehaviour
{
    [SerializeField] private SoundController soundController;
    [SerializeField] public GameObject backButton;

    private const int SELECTION_SCENE_INDEX = 1;
    private const float MUSIC_FADE_DURATION = 0.6f;

    private bool adInProgress = false;
    private bool transitionStarted = false;

    private void OnEnable()
    {
        YG2.onCloseInterAdv += OnAdClosed;
        YG2.onErrorInterAdv += OnAdError;
    }

    private void OnDisable() => Cleanup();

    public void OnBackButtonUp() => backButton.GetComponent<RectTransform>().localScale = Vector3.one;

    public void OnBackButtonDown()
    {
        if (transitionStarted) return;

        backButton.GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundAndShowAd());
    }

    private IEnumerator PlaySoundAndShowAd()
    {
        if (soundController != null)
        {
            soundController.PlayClickSound();

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

        Cleanup();
        
        // Запускаем корутину для плавного перехода аудио и загрузки сцены
        StartCoroutine(TransitionCoroutine());
    }
    
    private IEnumerator TransitionCoroutine()
    {
        // 1. Музыка уровня: Затухание и уничтожение.
        // !!! ИСПРАВЛЕНИЕ ЗАДЕРЖКИ: Запускаем корутину через StartCoroutine(), но НЕ используем yield return.
        if (BackgroundMusicManager.Instance != null)
        {
            // Музыка уровня будет затухать асинхронно, пока LoadingManager показывает экран.
            StartCoroutine(BackgroundMusicManager.Instance.FadeOutAndDestroy(MUSIC_FADE_DURATION));
        }

        // 2. Основная музыка: Запуск плавного появления
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.EndTestMode(MUSIC_FADE_DURATION);
        }
        
        // 3. Загрузка сцены: Запускаем немедленно
        LoadingManager.Instance.LoadScene(SELECTION_SCENE_INDEX);
        
        yield break;
    }

    private void Cleanup()
    {
        YG2.onCloseInterAdv -= OnAdClosed;
        YG2.onErrorInterAdv -= OnAdError;
    }
}