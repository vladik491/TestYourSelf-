using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

[RequireComponent(typeof(Image))]
public class GifSpriteSheet : MonoBehaviour
{
    [Header("Имя папки в Resources/GifFrames/")]
    public string folderName = "";

    [Header("Настройки анимации")]
    public float fps = 12f;
    public bool playOnAwake = true;
    public bool loop = true;

    [Header("Фильтрация")]
    [Tooltip("Номера (индексы) кадров, которые нужно пропустить. Например: 59, 60, 120")]
    public List<int> framesToSkip = new List<int> { };

    private Image image;
    private Sprite[] frames;
    private int currentFrame = 0;
    private float startTime;

    private static Dictionary<string, float> gifStartTimes = new Dictionary<string, float>();

    void Awake()
    {
        image = GetComponent<Image>();
        Sprite[] allSprites = Resources.LoadAll<Sprite>($"GifFrames/{folderName}");
        if (allSprites == null || allSprites.Length == 0)
        {
            enabled = false;
            return;
        }

        var indexedSprites = allSprites
            .Where(s => s != null)
            .Select((sprite, index) => new { Sprite = sprite, Index = index })
            .ToList();

        frames = indexedSprites
            .Where(item => !framesToSkip.Contains(item.Index))
            .Select(item => item.Sprite)
            .ToArray();

        if (frames.Length == 0)
        {
            enabled = false;
            return;
        }

        image.sprite = frames[0];

        if (playOnAwake)
        {
            InitializeStartTime();
            enabled = true;
        }
    }

    private void InitializeStartTime()
    {
        if (!gifStartTimes.ContainsKey(folderName))
        {
            gifStartTimes[folderName] = Time.unscaledTime;
        }
        startTime = gifStartTimes[folderName];
    }

    public void Play()
    {
        InitializeStartTime();
        enabled = true;
    }

    void Update()
    {
        if (frames.Length == 0) return;

        float elapsed = Time.unscaledTime - startTime;
        float totalDuration = (float)frames.Length / fps;

        if (!loop && elapsed >= totalDuration)
        {
            image.sprite = frames[frames.Length - 1];
            enabled = false;
            return;
        }

        int newFrame;
        if (loop)
        {
            newFrame = Mathf.FloorToInt(elapsed * fps) % frames.Length;
        }
        else
        {
            newFrame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(elapsed * fps));
        }

        if (newFrame != currentFrame)
        {
            currentFrame = newFrame;
            image.sprite = frames[newFrame];
        }
    }
}