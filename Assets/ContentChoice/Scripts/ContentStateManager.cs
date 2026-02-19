using UnityEngine;

public class ContentStateManager : MonoBehaviour
{
    // Глобальный экземпляр Синглтона.
    public static ContentStateManager Instance { get; private set; }

    // Сохраненный индекс категории, которая была выбрана последней.
    public int SelectedCategoryIndex { get; set; } = 0;

    // Массив для хранения индекса страницы (0, 1, 2...) для каждой категории.
    // По умолчанию: {0: Фильмы, 1: Сериалы, 2: Аниме}
    private int[] categoryPages = new int[3] { 0, 0, 0 };

    void Awake()
    {
        if (Instance == null)
        {
            // Устанавливаем экземпляр и делаем его "бессмертным".
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // Уничтожаем дубликаты, которые могут появиться при загрузке сцены.
            Destroy(gameObject);
        }
    }

    public void SetPage(int categoryIndex, int pageIndex)
    {
        if (categoryIndex >= 0 && categoryIndex < categoryPages.Length)
        {
            categoryPages[categoryIndex] = pageIndex;
        }
    }

    public int GetPage(int categoryIndex)
    {
        if (categoryIndex >= 0 && categoryIndex < categoryPages.Length)
        {
            return categoryPages[categoryIndex];
        }
        return 0; // Возвращаем Page 1 (индекс 0) по умолчанию
    }
}