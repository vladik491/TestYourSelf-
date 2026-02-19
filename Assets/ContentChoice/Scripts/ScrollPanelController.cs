using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ScrollPanelController : MonoBehaviour
{
    public Button ScrollUpButton;
    public Button ScrollDownButton;
    public RectTransform ContentPanel;
    public TextMeshProUGUI PageCounterText;
    public float pageHeight = 320f;
    public float spacing = 120f;
    public float dropDuration = 0.5f;
    public float settlePause = 0.1f;
    public AnimationCurve dropCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float counterDuration = 0.2f;
    public AnimationCurve counterCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [HideInInspector] public int currentPage = 0;
    private int totalPages;
    private float step;
    private bool isAnimating = false;
    private CanvasGroup contentCanvasGroup;

    // Индекс категории, устанавливается из DropdownPanelController.
    public int CategoryIndex { get; set; } = -1;

    void Awake()
    {
        step = pageHeight + spacing;
        totalPages = ContentPanel != null ? ContentPanel.childCount : 0;
        contentCanvasGroup = ContentPanel.GetComponent<CanvasGroup>();
        if (contentCanvasGroup == null) contentCanvasGroup = ContentPanel.gameObject.AddComponent<CanvasGroup>();

        ScrollUpButton.onClick.AddListener(OnScrollUp);
        ScrollDownButton.onClick.AddListener(OnScrollDown);

        UpdatePageCounter();
        UpdateScrollButtonsInteractable();
    }

    void OnScrollUp()
    {
        if (isAnimating) return;
        int old = currentPage;
        currentPage = currentPage == 0 ? totalPages - 1 : currentPage - 1;

        if (ContentStateManager.Instance != null && CategoryIndex >= 0)
        {
            ContentStateManager.Instance.SetPage(CategoryIndex, currentPage);
        }

        StartCoroutine(AnimateToPage(currentPage));
        StartCoroutine(AnimateCounter(old, currentPage));
    }

    void OnScrollDown()
    {
        if (isAnimating) return;
        int old = currentPage;
        currentPage = currentPage == totalPages - 1 ? 0 : currentPage + 1;

        if (ContentStateManager.Instance != null && CategoryIndex >= 0)
        {
            ContentStateManager.Instance.SetPage(CategoryIndex, currentPage);
        }

        StartCoroutine(AnimateToPage(currentPage));
        StartCoroutine(AnimateCounter(old, currentPage));
    }

    IEnumerator AnimateToPage(int page)
    {
        isAnimating = true;
        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.interactable = false;
            contentCanvasGroup.blocksRaycasts = false;
        }
        ScrollUpButton.interactable = false;
        ScrollDownButton.interactable = false;

        float targetY = page * step;
        Vector2 startPos = ContentPanel.anchoredPosition;
        Vector2 targetPos = new Vector2(0, targetY);
        float t = 0f;

        while (t < dropDuration)
        {
            float norm = t / dropDuration;
            float eval = dropCurve.Evaluate(norm);
            ContentPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, eval);
            t += Time.deltaTime;
            yield return null;
        }

        ContentPanel.anchoredPosition = targetPos;
        yield return new WaitForSeconds(settlePause);

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.interactable = true;
            contentCanvasGroup.blocksRaycasts = true;
        }
        isAnimating = false;
        UpdateScrollButtonsInteractable();
        UpdatePageCounter();
    }

    IEnumerator AnimateCounter(int fromPage, int toPage)
    {
        float t = 0f;
        while (t < counterDuration)
        {
            float norm = t / counterDuration;
            float eval = counterCurve.Evaluate(norm);
            int displayedPage = Mathf.RoundToInt(Mathf.Lerp(fromPage + 1, toPage + 1, eval));
            PageCounterText.text = $"{displayedPage}/{totalPages}";
            t += Time.deltaTime;
            yield return null;
        }
        PageCounterText.text = $"{toPage + 1}/{totalPages}";
    }

    void UpdatePageCounter()
    {
        PageCounterText.text = totalPages > 0 ? $"{currentPage + 1}/{totalPages}" : "0/0";
    }

    void UpdateScrollButtonsInteractable()
    {
        ScrollUpButton.interactable = !isAnimating;
        ScrollDownButton.interactable = !isAnimating;
    }

    // Вызывается из DropdownPanelController для загрузки сохраненной страницы.
    public void SnapToCurrentPage()
    {
        if (ContentStateManager.Instance != null && CategoryIndex >= 0 && CategoryIndex <= 2)
        {
            currentPage = ContentStateManager.Instance.GetPage(CategoryIndex);
        }
        else
        {
            // Если индекс не установлен, используем Page 1
            currentPage = 0;
        }

        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        StopAllCoroutines();
        ContentPanel.anchoredPosition = new Vector2(0, currentPage * step);
        isAnimating = false;

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.interactable = true;
            contentCanvasGroup.blocksRaycasts = true;
        }
        UpdateScrollButtonsInteractable();
        UpdatePageCounter();
    }
}