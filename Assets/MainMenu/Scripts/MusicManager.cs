using UnityEngine;
using System;
using System.Collections;
using PlayerPrefs = RedefineYG.PlayerPrefs;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    private AudioSource musicSource;

    private float defaultVolume;
    public float CurrentMasterVolume => defaultVolume;

    public static event Action<float> OnMusicVolumeChanged;

    private Coroutine fadeCoroutine;

    private bool isInTestMode = false;
    public bool IsInTestMode => isInTestMode;

    private const float DEFAULT_FADE_DURATION = 0.6f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            musicSource = GetComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);

            // Блок try-catch для безопасной загрузки громкости
            try
            {
                // Устанавливаем дефолтное значение 0.3f
                defaultVolume = PlayerPrefs.GetFloat("MusicVolume", 0.3f);
            }
            catch (System.FormatException e)
            {
                // Лог обновлен, чтобы отражать значение 0.3f
                Debug.LogError($"[MusicManager] Ошибка формата при загрузке MusicVolume: {e.Message}. Установка значения по умолчанию (0.3f) и сохранение.");
                defaultVolume = 0.3f; // Фоллбэк 0.3f
                PlayerPrefs.SetFloat("MusicVolume", defaultVolume);
                PlayerPrefs.Save();
            }

            musicSource.volume = defaultVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // установить мастер-громкость и уведомить подписчиков
    public void SetVolume(float v)
    {
        defaultVolume = Mathf.Clamp01(v);
        musicSource.volume = defaultVolume;
        PlayerPrefs.SetFloat("MusicVolume", defaultVolume);
        PlayerPrefs.Save();
        OnMusicVolumeChanged?.Invoke(defaultVolume);
    }

    // мгновенно установить громкость
    public void SetVolumeImmediate(float v)
    {
        defaultVolume = Mathf.Clamp01(v);
        musicSource.volume = defaultVolume;
        PlayerPrefs.SetFloat("MusicVolume", defaultVolume);
        PlayerPrefs.Save();
        OnMusicVolumeChanged?.Invoke(defaultVolume);
    }

    // Запуск режима теста (плавное приглушение)
    public Coroutine StartTestMode(float duration = DEFAULT_FADE_DURATION) // возвращает Coroutine
    {
        if (isInTestMode) return null;
        isInTestMode = true;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(0f, duration));
        return fadeCoroutine; // Возвращаем Coroutine
    }

    // Выход из теста (плавное восстановление)
    public Coroutine EndTestMode(float duration = DEFAULT_FADE_DURATION) // возвращает Coroutine
    {
        if (!isInTestMode) return null;
        isInTestMode = false;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        // Target volume is the stored defaultVolume
        fadeCoroutine = StartCoroutine(FadeRoutine(defaultVolume, duration));
        return fadeCoroutine; // Возвращаем Coroutine
    }

    // Мгновенный режим теста (приглушение)
    public void StartTestModeInstant()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        isInTestMode = true;
        musicSource.volume = 0f;
    }

    // Мгновенный выход из теста (восстановление)
    public void EndTestModeInstant()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        isInTestMode = false;
        musicSource.volume = defaultVolume;
    }

    private IEnumerator FadeRoutine(float targetVolume, float duration)
    {
        float start = musicSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, targetVolume, Mathf.Clamp01(t / duration));
            yield return null;
        }
        musicSource.volume = targetVolume;
        fadeCoroutine = null;
    }
}