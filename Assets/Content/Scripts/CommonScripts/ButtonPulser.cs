using System.Collections;
using UnityEngine;

public class ButtonPulser : MonoBehaviour
{
    [SerializeField] private float maxScaleFactor = 1.2f; // максимальный масштаб
    [SerializeField] private float durationPerStep = 0.6f; // время на up/down

    private Vector3 originalScale; // сохраняем исходный масштаб при старте

    private void Awake()
    {
        // Сохраняем исходный масштаб при инициализации
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) originalScale = rt.localScale;
    }

    // Публичный метод для запуска анимации с указанным количеством пульсаций и опциональной задержкой
    public void TriggerPulse(int numberOfPulses, float delay = 0.9f)
    {
        StopAllCoroutines(); // остановить предыдущие
        ResetScale(); // ← СБРОС МАСШТАБА перед стартом
        StartCoroutine(DelayedPulse(numberOfPulses, delay));
    }

    private IEnumerator DelayedPulse(int numberOfPulses, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(PulseAnimation(numberOfPulses));
    }

    private IEnumerator PulseAnimation(int numberOfPulses)
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) yield break;

        for (int i = 0; i < numberOfPulses; i++)
        {
            // Up: увеличение
            yield return StartCoroutine(LerpScale(rt, originalScale, originalScale * maxScaleFactor, durationPerStep));
            // Down: уменьшение
            yield return StartCoroutine(LerpScale(rt, originalScale * maxScaleFactor, originalScale, durationPerStep));
        }
    }

    private IEnumerator LerpScale(RectTransform rt, Vector3 startScale, Vector3 endScale, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float lerpFactor = time / duration;
            rt.localScale = Vector3.Lerp(startScale, endScale, lerpFactor);
            yield return null;
        }
        rt.localScale = endScale;
    }

    // Публичный метод для сброса масштаба (вызывай при остановке анимации)
    public void ResetScale()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) rt.localScale = originalScale; // или Vector3.one, если исходный = 1
    }
}