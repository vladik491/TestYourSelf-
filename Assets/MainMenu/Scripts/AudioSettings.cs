using UnityEngine;
using UnityEngine.UI;
using PlayerPrefs = RedefineYG.PlayerPrefs;

public class AudioSettings : MonoBehaviour
{
    [SerializeField] SoundController soundsController;
    [SerializeField] Slider musicSlider, soundSlider;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SoundVolumeKey = "SoundVolume";

    private void Start()
    {
        float savedMusicVolume = 0.3f; 
        float savedSoundVolume = 0.3f; 

        // Безопасная загрузка MusicVolume
        try
        {
            savedMusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.3f);
        }
        catch (System.FormatException e)
        {
            // Лог обновлен, чтобы отражать значение 0.3f
            Debug.LogError($"[AudioSettings] Ошибка формата при загрузке MusicVolume: {e.Message}. Установка значения по умолчанию (0.3f) и сохранение.");
            savedMusicVolume = 0.3f; // Фоллбэк 0.3f
            PlayerPrefs.SetFloat(MusicVolumeKey, savedMusicVolume);
            PlayerPrefs.Save();
        }

        // Безопасная загрузка SoundVolume
        try
        {
            savedSoundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 0.3f); 
        }
        catch (System.FormatException e)
        {
            // Лог обновлен, чтобы отражать значение 0.3f
            Debug.LogError($"[AudioSettings] Ошибка формата при загрузке SoundVolume: {e.Message}. Установка значения по умолчанию (0.3f) и сохранение.");
            savedSoundVolume = 0.3f; // Фоллбэк 0.3f
            PlayerPrefs.SetFloat(SoundVolumeKey, savedSoundVolume);
            PlayerPrefs.Save();
        }

        musicSlider.value = savedMusicVolume;
        soundSlider.value = savedSoundVolume;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetVolume(savedMusicVolume);
        }

        if (soundsController != null)
        {
            soundsController.SetVolume(savedSoundVolume);
        }

        musicSlider.onValueChanged.AddListener(v => {
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.SetVolume(v);
            }
            PlayerPrefs.SetFloat(MusicVolumeKey, v);
            PlayerPrefs.Save(); 
        });

        soundSlider.onValueChanged.AddListener(v => {
            if (soundsController != null)
            {
                soundsController.SetVolume(v);
            }
            PlayerPrefs.SetFloat(SoundVolumeKey, v);
            PlayerPrefs.Save();
        });
    }
}