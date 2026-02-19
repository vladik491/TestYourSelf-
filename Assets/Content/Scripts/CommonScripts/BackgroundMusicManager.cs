using UnityEngine;
using System.Collections;

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }
    private AudioSource _audio;
    public float targetVolume = 0.6f;
    private Coroutine fadeCoroutine;
    private bool wasPlaying;

    private const float DEFAULT_FADE_DURATION = 0.6f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _audio = GetComponent<AudioSource>();
            _audio.loop = true;
            _audio.volume = 0f;
            _audio.Play();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        MusicManager.OnMusicVolumeChanged += OnMasterVolumeChanged;
    }

    void OnDisable()
    {
        MusicManager.OnMusicVolumeChanged -= OnMasterVolumeChanged;
    }

    void Start()
    {
        float master = MusicManager.Instance.CurrentMasterVolume;
        float effectiveTarget = targetVolume * master;

        // Музыка уровня должна плавно появиться, если Main Music была приглушена
        if (MusicManager.Instance.IsInTestMode)
        {
            _audio.UnPause();
            FadeTo(effectiveTarget, DEFAULT_FADE_DURATION); // Плавное появление
        }
        else
        {
            _audio.Pause();
            _audio.volume = 0f;
        }
    }

    // Корутина для плавного затухания до 0 и уничтожения объекта
    public IEnumerator FadeOutAndDestroy(float duration = DEFAULT_FADE_DURATION)
    {
        // Ждем завершения фейда до 0
        yield return FadeToRoutine(0f, duration);

        _audio.Stop();
        Destroy(gameObject);
    }


    // Мгновенная установка на целевую громкость
    public void SetToTargetVolumeImmediate()
    {
        float master = MusicManager.Instance.CurrentMasterVolume;
        float effectiveTarget = targetVolume * master;
        _audio.volume = effectiveTarget;
    }

    // Обновленный FadeTo, который запускает корутину
    public void FadeTo(float target, float duration)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (duration <= 0f)
        {
            _audio.volume = target;
            fadeCoroutine = null;
            return;
        }
        fadeCoroutine = StartCoroutine(FadeToRoutine(target, duration));
    }

    // Корутина для плавного фейда
    private IEnumerator FadeToRoutine(float target, float duration)
    {
        float start = _audio.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _audio.volume = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        _audio.volume = target;
        fadeCoroutine = null; // Обнуляем только если корутина завершилась
    }

    // Мгновенное выключение и остановка (больше не используется для перехода, но оставлено)
    public void SetOutAndStopImmediate()
    {
        _audio.volume = 0f;
        _audio.Pause();
        MusicManager.Instance.EndTestModeInstant();
        Destroy(gameObject);
    }

    // Этот метод теперь устарел, но оставлен для совместимости
    public void FadeOutAndStop(float duration = 0f, float mainFadeDuration = 0f)
    {
        SetOutAndStopImmediate();
    }

    private void OnMasterVolumeChanged(float master)
    {
        float effectiveTarget = targetVolume * master;
        _audio.volume = effectiveTarget; // Мгновенная установка
    }
}