using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using YG;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(RectTransform))]
public class AssessmentButtonHandler : MonoBehaviour
{
    [Header("Настройки анимации тряски")]
    [SerializeField] private float shakeDuration = 0.28f;
    [SerializeField] private float shakeMagnitude = 6f;

    [Header("Настройки звука")]
    [SerializeField] private SoundController soundController;

    private const float PRESSED_SCALE = 0.95f;
    private static readonly Color LOCKED_COLOR = new Color(0.7f, 0.7f, 0.7f, 1f);

    private Button assessmentButton;
    private Image buttonImage;
    private Color originalColor;
    private RectTransform buttonRect;
    private Coroutine shakeCoroutine = null;

    private Vector3 originalScale;
    private Vector2 originalPosition;

    // Флаг, который мы будем хранить в течение сессии, чтобы знать, что отзыв уже оставлен
    private bool reviewAlreadyDone = false;

    void Awake()
    {
        assessmentButton = GetComponent<Button>();
        buttonRect = GetComponent<RectTransform>();
        buttonImage = GetComponent<Image>();

        if (buttonImage != null) originalColor = buttonImage.color;
        originalScale = transform.localScale;
        if (buttonRect != null) originalPosition = buttonRect.anchoredPosition;

        assessmentButton.onClick.AddListener(OnAssessmentClicked);
    }

    void Start()
    {
        // При старте проверяем SDK. Если уже оценивали ранее (в прошлых сессиях), 
        // SDK вернет reviewCanShow = false сразу.
        reviewAlreadyDone = !YG2.reviewCanShow;
        UpdateVisualState();
    }

    void OnEnable()
    {
        YG2.onReviewSent += OnReviewResult;
    }

    void OnDisable()
    {
        YG2.onReviewSent -= OnReviewResult;
    }

    private void OnAssessmentClicked()
    {
        // Если отзыв уже успешно отправлен — только трясем
        if (reviewAlreadyDone)
        {
            StartShake();
            return;
        }

        // Если еще не оценивали, пробуем вызвать окно
        if (YG2.reviewCanShow)
        {
            YG2.ReviewShow();
        }
        else
        {
            // Сюда попадем, если окно только что закрыли на крестик 
            // и Яндекс временно заблокировал вызов, либо нет интернета.
            StartShake();
        }
    }

    private void OnReviewResult(bool success)
    {
        if (success)
        {
            Debug.Log("[Review] Успешно оценено. Блокируем.");
            reviewAlreadyDone = true;
            UpdateVisualState();
        }
        else
        {
            Debug.Log("[Review] Окно закрыто без отзыва. Оставляем кнопку активной.");
            // Игнорируем то, что скажет SDK в UpdateVisualState, 
            // принудительно включаем визуал обратно.
            reviewAlreadyDone = false;
            SetButtonActive(true);
        }
    }

    public void UpdateVisualState()
    {
        // Кнопка серая, только если отзыв ТОЧНО отправлен
        SetButtonActive(!reviewAlreadyDone);
    }

    private void SetButtonActive(bool isActive)
    {
        if (buttonImage == null) return;
        buttonImage.color = isActive ? originalColor : LOCKED_COLOR;

        // Оставляем кнопку кликабельной для эффекта тряски
        assessmentButton.interactable = true;
    }

    // --- ЭФФЕКТЫ ---
    public void OnButtonPress()
    {
        transform.localScale = originalScale * PRESSED_SCALE;
        if (soundController != null) soundController.PlayClickSound();
    }

    public void OnButtonUp()
    {
        transform.localScale = originalScale;
    }

    public void StartShake()
    {
        if (buttonRect == null) return;
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRectCoroutine(buttonRect, shakeDuration, shakeMagnitude));
    }

    IEnumerator ShakeRectCoroutine(RectTransform rt, float duration, float magnitude)
    {
        Vector2 orig = originalPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float damper = 1f - (elapsed / duration);
            rt.anchoredPosition = orig + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * magnitude * damper;
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = orig;
        shakeCoroutine = null;
    }
}