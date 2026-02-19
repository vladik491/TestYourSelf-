using UnityEngine;
using UnityEngine.UI;

public class DropdownPanelController : MonoBehaviour
{
    [SerializeField] private SoundController soundController;
    public Dropdown contentSelection;
    public GameObject[] panels; // 0: Фильмы, 1: Сериалы, 2: Аниме

    void Start()
    {
        if (contentSelection == null || panels == null || panels.Length != 3)
        {
            Debug.LogError("DropdownPanelController: Проверьте настройки! panels должен содержать 3 элемента.");
            return;
        }

        // 1. Устанавливаем индексы категорий для контроллеров прокрутки.
        InitializeCategoryIndices();

        if (ContentStateManager.Instance == null)
        {
            Debug.LogError("ContentStateManager.Instance is NULL! Убедитесь, что он создан на предыдущей сцене.");
            return;
        }

        // 2. Загружаем последнюю выбранную категорию из Синглтона.
        int savedIndex = Mathf.Clamp(ContentStateManager.Instance.SelectedCategoryIndex, 0, 2);

        contentSelection.SetValueWithoutNotify(savedIndex);

        // 3. Активируем панель и загружаем сохраненную страницу.
        ShowPanel(savedIndex);

        contentSelection.onValueChanged.AddListener(OnSelectionChanged);
    }

    // Установка индекса для ScrollPanelController, независимо от того, где он находится.
    private void InitializeCategoryIndices()
    {
        for (int i = 0; i < 3; i++)
        {
            ScrollPanelController scroll = GetScrollController(panels[i]);
            if (scroll != null)
            {
                scroll.CategoryIndex = i;
            }
        }
    }

    private void OnSelectionChanged(int value)
    {
        value = Mathf.Clamp(value, 0, 2);

        if (ContentStateManager.Instance != null)
        {
            ContentStateManager.Instance.SelectedCategoryIndex = value;
        }

        if (soundController != null)
            soundController.SelectionOfCategories();

        ShowPanel(value);
    }

    void ShowPanel(int index)
    {
        for (int i = 0; i < 3; i++)
        {
            if (panels[i] != null)
            {
                panels[i].SetActive(i == index);

                if (i == index)
                {
                    ResetPanel(panels[i]);
                }
            }
        }
    }

    private void ResetPanel(GameObject panel)
    {
        // Сброс видимости контейнеров
        Transform container = panel.transform.Find("MoviesPanelsContainer")
                           ?? panel.transform.Find("SeriesPanelsContainer")
                           ?? panel.transform.Find("AnimePanelsContainer");
        if (container != null)
        {
            foreach (Transform child in container)
                child.gameObject.SetActive(false);
            container.gameObject.SetActive(true);
        }

        // Установка активных элементов UI
        var outer = panel.transform.Find("OuterPanel");
        if (outer != null) outer.gameObject.SetActive(true);

        var scroll = panel.transform.Find("ScrollAndPage");
        if (scroll != null) scroll.gameObject.SetActive(true);

        var back = panel.transform.Find("GoBackButton");
        if (back != null) back.gameObject.SetActive(false);

        // Получаем контроллер и загружаем сохраненную страницу.
        var ctrl = GetScrollController(panel);

        if (ctrl != null)
        {
            ctrl.SnapToCurrentPage();
        }
        else
        {
            Debug.LogError($"Не найден ScrollPanelController для панели {panel.name}. Проверьте иерархию.");
        }
    }

    // Гибкий поиск ScrollPanelController
    private ScrollPanelController GetScrollController(GameObject panel)
    {
        // 1. Ищем на объекте ScrollAndPage (наиболее вероятное место)
        var scrollObj = panel.transform.Find("ScrollAndPage");
        var ctrl = scrollObj?.GetComponent<ScrollPanelController>();

        // 2. Если не нашли, ищем на корневой панели
        if (ctrl == null)
        {
            ctrl = panel.GetComponent<ScrollPanelController>();
        }

        return ctrl;
    }
}