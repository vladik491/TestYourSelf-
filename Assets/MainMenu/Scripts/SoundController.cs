using UnityEngine;
using PlayerPrefs = RedefineYG.PlayerPrefs;

public class SoundController : MonoBehaviour
{
    private AudioSource _audio;

    public AudioClip clickSound;
    public AudioClip soundCategories;
    public AudioClip soundOfCorrectAnswer;
    public AudioClip soundOfWrongAnswer;
    public AudioClip soundOfLoss;
    public AudioClip soundOfWin;

    private void Start()
    {
        _audio = GetComponent<AudioSource>();
        float savedVolume = 0.3f;

        // Блок try-catch для безопасной загрузки громкости звуков
        try
        {
            savedVolume = PlayerPrefs.GetFloat("SoundVolume", 0.3f);
        }
        catch (System.FormatException e)
        {
            // Лог обновлен, чтобы отражать значение 0.3f
            Debug.LogError($"[SoundController] Ошибка формата при загрузке SoundVolume: {e.Message}. Установка значения по умолчанию (0.3f) и сохранение.");
            savedVolume = 0.3f; // Фоллбэк 0.3f
            PlayerPrefs.SetFloat("SoundVolume", savedVolume);
            PlayerPrefs.Save();
        }

        SetVolume(savedVolume);
    }

    public void SetVolume(float v)
    {
        if (_audio == null)
        {
            _audio = GetComponent<AudioSource>();
        }
        _audio.volume = v;
    }

    public void PlayClickSound()
    {
        _audio.PlayOneShot(clickSound);
    }

    public void SelectionOfCategories()
    {
        _audio.PlayOneShot(soundCategories);
    }

    public void SoundOfCorrectAnswer()
    {
        _audio.PlayOneShot(soundOfCorrectAnswer);
    }

    public void SoundOfWrongAnswer()
    {
        _audio.PlayOneShot(soundOfWrongAnswer);
    }

    public void SoundOfLoss()
    {
        _audio.PlayOneShot(soundOfLoss);
    }

    public void SoundOfWin()
    {
        _audio.PlayOneShot(soundOfWin);
    }
}