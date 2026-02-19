using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIFadeController : MonoBehaviour
{
    [Header("Настройки для всех объектов")]
    [SerializeField] private float delayBeforeFade = 0.6f;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("Перетащи сюда UI объекты")]
    [SerializeField] private GameObject[] uiObjects;

    void Start()
    {
        foreach (var obj in uiObjects)
        {
            var fader = obj.AddComponent<FadeInObject>();
            fader.Setup(delayBeforeFade, fadeDuration);
        }
    }
}

public class FadeInObject : MonoBehaviour
{
    private Image image;
    private TextMeshProUGUI text;
    private Color originalColor;
    private bool isFading;
    private bool isDone;
    private float startTime, fadeDuration;

    public void Setup(float delay, float duration)
    {
        image = GetComponent<Image>();
        text = GetComponent<TextMeshProUGUI>();

        if (image == null && text == null) { Destroy(this); return; }

        if (image != null) { originalColor = image.color; SetAlpha(0f); }
        else if (text != null) { originalColor = text.color; SetAlpha(0f); }

        Invoke("StartFadeIn", delay);
        this.fadeDuration = duration;
    }

    void Update()
    {
        if (!isFading || isDone) return;
        float progress = (Time.time - startTime) / fadeDuration;
        if (progress >= 1f) { SetAlpha(1f); isFading = false; isDone = true; enabled = false; return; }
        SetAlpha(Mathf.Lerp(0f, 1f, progress));
    }

    private void SetAlpha(float alpha)
    {
        Color color = originalColor; color.a = alpha;
        if (image != null) image.color = color;
        else if (text != null) text.color = color;
    }

    private void StartFadeIn()
    {
        if (!isFading && !isDone) { isFading = true; startTime = Time.time; }
    }
}