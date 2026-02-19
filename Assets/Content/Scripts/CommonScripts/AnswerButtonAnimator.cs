using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class AnswerButtonAnimator : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
{

    readonly Vector3 PRESSED_SCALE = new Vector3(0.95f, 0.95f, 1f);
    Vector3 NORMAL_SCALE;
    const float CLICK_PUNCH_MULTIPLIER = 1.06f;

    const float PRESS_DURATION = 0.06f;
    const float RELEASE_DURATION = 0.12f;
    const float CLICK_PUNCH_DURATION = 0.14f;

    const bool USE_UNSCALED_TIME = true;

    // внутренние
    RectTransform rt;
    Coroutine scaleCoroutine;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        // фиксируем нормальный масштаб как текущее значение (чтобы не ломать дизайн)
        NORMAL_SCALE = rt != null ? rt.localScale : Vector3.one;
    }

    // Pointer down Ч плавно уменьшаем до PRESSED_SCALE
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        StartScaleTo(PRESSED_SCALE, PRESS_DURATION);
    }

    // Pointer up Ч плавно возвращаем к нормальному масштабу
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        StartScaleTo(NORMAL_SCALE, RELEASE_DURATION);
    }

    // если ушЄл курсор Ч вернуть в норму
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        StartScaleTo(NORMAL_SCALE, RELEASE_DURATION);
    }

    // Click Ч делаем короткий punch (overshoot -> normal)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ClickPunchRoutine());
    }

    IEnumerator ClickPunchRoutine()
    {
        Vector3 start = rt.localScale;
        Vector3 target = NORMAL_SCALE * CLICK_PUNCH_MULTIPLIER;
        float half = CLICK_PUNCH_DURATION * 0.5f;
        float t = 0f;

        // к overshoot
        while (t < half)
        {
            t += DeltaTime();
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / half));
            rt.localScale = Vector3.Lerp(start, target, p);
            yield return null;
        }

        // назад к норме
        t = 0f;
        Vector3 from = rt.localScale;
        while (t < half)
        {
            t += DeltaTime();
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / half));
            rt.localScale = Vector3.Lerp(from, NORMAL_SCALE, p);
            yield return null;
        }

        rt.localScale = NORMAL_SCALE;
        scaleCoroutine = null;
    }

    void StartScaleTo(Vector3 targetScale, float duration)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(targetScale, duration));
    }

    IEnumerator ScaleRoutine(Vector3 target, float duration)
    {
        Vector3 from = rt.localScale;
        float t = 0f;

        if (duration <= 0f)
        {
            rt.localScale = target;
            scaleCoroutine = null;
            yield break;
        }

        while (t < duration)
        {
            t += DeltaTime();
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            rt.localScale = Vector3.Lerp(from, target, p);
            yield return null;
        }

        rt.localScale = target;
        scaleCoroutine = null;
    }

    void OnDisable()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = null;
        if (rt != null) rt.localScale = NORMAL_SCALE;
    }

    float DeltaTime()
    {
        return USE_UNSCALED_TIME ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    bool IsInteractable()
    {
        var btn = GetComponent<Button>();
        if (btn != null) return btn.interactable;
        return true;
    }

    // публичный метод дл€ мгновенного сброса масштаба
    public void ResetScaleImmediate()
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = null;
        if (rt != null) rt.localScale = NORMAL_SCALE;
    }
}
