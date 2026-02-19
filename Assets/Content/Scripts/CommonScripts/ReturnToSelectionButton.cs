using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

public class ReturnToSelectionButton : MonoBehaviour
{
    // Глобальный флаг для предотвращения одновременного нажатия нескольких кнопок
    public static bool IsCriticalActionInProgress = false;

    [Header("Ссылки")]
    [SerializeField] private SoundController soundController;
    [SerializeField] private GameObject targetButton;
    [SerializeField] private string sceneName;

    private bool adInProgress = false;
    private bool transitionStarted = false;

    private const float MUSIC_FADE_DURATION = 0.6f;

    private void OnEnable()
    {
        // Подписываемся на события межстраничной рекламы
        YG2.onCloseInterAdv += OnAdClosed;
        YG2.onErrorInterAdv += OnAdError;
        IsCriticalActionInProgress = false;
    }

    private void OnDisable() => Cleanup();

    // Визуальный отклик при отпускании кнопки
    public void OnButtonUp()
    {
        if (targetButton != null)
            targetButton.GetComponent<RectTransform>().localScale = Vector3.one;
    }

    // Основная логика при нажатии кнопки
    public void OnButtonDown()
    {
        // Если переход уже идет или нажата другая важная кнопка — игнорируем
        if (transitionStarted || IsCriticalActionInProgress) return;

        IsCriticalActionInProgress = true;

        if (targetButton != null)
            targetButton.GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);

        StartCoroutine(PlaySoundAndShowAd());
    }

    private IEnumerator PlaySoundAndShowAd()
    {
        // 1. Короткая задержка для звука клика
        if (soundController != null)
        {
            soundController.SelectionOfCategories();
            float wait = soundController.clickSound != null ? soundController.clickSound.length : 0f;
            yield return new WaitForSecondsRealtime(Mathf.Min(wait, 0.3f));
        }
        else
        {
            yield return null;
        }

        // 2. Проверка возможности показа рекламы по таймеру SDK
        if (YG2.isTimerAdvCompleted && !YG2.nowInterAdv)
        {
            adInProgress = true;
            YG2.InterstitialAdvShow();

            // ЗАПУСКАЕМ СТРАХОВКУ: Если за 1 сек. реклама не перехватит управление — уходим
            StartCoroutine(WatchdogForAdStart());
        }
        else
        {
            ContinueTransition();
        }
    }

    // Страховочная корутина (Watchdog)
    private IEnumerator WatchdogForAdStart()
    {
        float timer = 0f;
        float timeout = 1.0f; // Оптимальное время ожидания для игрока

        while (timer < timeout)
        {
            // Если реклама открылась (nowInterAdv) или время встало на паузу (TimeScale)
            if (YG2.nowInterAdv || Time.timeScale == 0)
            {
                yield break; // Реклама успешно запущена, страховка больше не нужна
            }

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Если мы здесь — реклама не ответила за 1 секунду. Принудительно выходим.
        if (adInProgress && !transitionStarted)
        {
            Debug.LogWarning("[ReturnButton] Тайм-аут рекламы. Форсированный переход.");
            adInProgress = false;
            ContinueTransition();
        }
    }

    private void OnAdClosed()
    {
        if (adInProgress && !transitionStarted)
        {
            adInProgress = false;
            // Стабилизация (пропуск кадра) после закрытия оверлея рекламы
            StartCoroutine(PostAdStabilization());
        }
    }

    private void OnAdError()
    {
        if (adInProgress && !transitionStarted)
        {
            adInProgress = false;
            ContinueTransition();
        }
    }

    private IEnumerator PostAdStabilization()
    {
        yield return null;
        ContinueTransition();
    }

    private void ContinueTransition()
    {
        // Защита от двойного вызова перехода
        if (transitionStarted) return;
        transitionStarted = true;

        // Снимаем блокировку кнопок
        IsCriticalActionInProgress = false;

        Cleanup();
        Time.timeScale = 1f; // Гарантируем нормальную скорость времени

        StartCoroutine(TransitionCoroutine());
    }

    private IEnumerator TransitionCoroutine()
    {
        // Управление музыкой
        if (BackgroundMusicManager.Instance != null)
        {
            StartCoroutine(BackgroundMusicManager.Instance.FadeOutAndDestroy(MUSIC_FADE_DURATION));
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.EndTestMode(MUSIC_FADE_DURATION);
        }

        // Запуск экрана загрузки или мгновенная смена сцены
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(sceneName);
        }
        else
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = true;
            while (!asyncLoad.isDone) yield return null;
        }

        yield break;
    }

    private void Cleanup()
    {
        YG2.onCloseInterAdv -= OnAdClosed;
        YG2.onErrorInterAdv -= OnAdError;
    }
}