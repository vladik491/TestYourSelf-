using UnityEngine;
using System.Collections;

public class DisclaimerController : MonoBehaviour
{
    public static bool hasPlayerClicked = false;

    [Header("Настройки объектов")]
    public Transform panelToAnimate;

    [Header("Настройки анимации")]
    public float bumpScale = 1.15f;
    public float bumpDuration = 0.2f;
    public float shrinkDuration = 0.2f;

    private bool isClosing = false;

    void Awake()
    {
        if (hasPlayerClicked)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Update()
    {
        if (isClosing) return;

        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(CloseSequence());
        }
    }

    private IEnumerator CloseSequence()
    {
        isClosing = true;

        hasPlayerClicked = true;

        Vector3 originalScale = panelToAnimate.localScale;
        Vector3 maxScale = originalScale * bumpScale;

        // 1. Увеличение
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime / bumpDuration;
            panelToAnimate.localScale = Vector3.Lerp(originalScale, maxScale, timer);
            yield return null;
        }

        // 2. Уменьшение и исчезновение
        timer = 0f;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime / shrinkDuration;
            panelToAnimate.localScale = Vector3.Lerp(maxScale, Vector3.zero, timer);
            yield return null;
        }

        panelToAnimate.localScale = Vector3.zero;
        Destroy(gameObject);
    }
}