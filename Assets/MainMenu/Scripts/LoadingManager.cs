using System.Collections;
using System.Collections.Generic; // Добавлено для List<T>
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite[] loadingSprites;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float minDisplayTime = 0.7f;

    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private float textAnimationSpeed = 0.4f;

    // --- Логика рандомизации без повторений ---
    private List<Sprite> _allSprites = new List<Sprite>();
    private List<Sprite> _availableSprites = new List<Sprite>();
    // --- Конец логики рандомизации ---

    private Coroutine textAnimationCoroutine;
    private bool isReady = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Инициализация списков для рандомного показа без повторений
        if (loadingSprites != null && loadingSprites.Length > 0)
        {
            _allSprites.AddRange(loadingSprites);

            // Заполняем и сразу перемешиваем список доступных спрайтов для первого цикла
            _availableSprites.AddRange(_allSprites);
            ShuffleList(_availableSprites);
        }

        if (loadingScreen != null)
        {
            // ИСПРАВЛЕНИЕ DDOL ОШИБКИ: Отвязываем дочерний объект от родителя
            loadingScreen.transform.SetParent(null, false);
            DontDestroyOnLoad(loadingScreen);

            // Получаем или добавляем необходимые компоненты (если не назначены вручную)
            if (canvasGroup == null)
            {
                canvasGroup = loadingScreen.GetComponent<CanvasGroup>();
            }

            Canvas canvas = loadingScreen.GetComponent<Canvas>();
            if (canvas == null) canvas = loadingScreen.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            if (loadingScreen.GetComponent<GraphicRaycaster>() == null)
            {
                loadingScreen.AddComponent<GraphicRaycaster>();
            }
            if (canvasGroup == null)
            {
                canvasGroup = loadingScreen.AddComponent<CanvasGroup>();
            }
        }

        if (backgroundImage == null && loadingScreen != null)
        {
            backgroundImage = loadingScreen.GetComponentInChildren<Image>();
        }

        if (loadingText == null && loadingScreen != null)
        {
            loadingText = loadingScreen.GetComponentInChildren<TMP_Text>();
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        // Отключаем компонент TMP_Text в Awake.
        if (loadingText != null)
        {
            loadingText.enabled = false;
        }

        isReady = true;
    }

    // Реализация алгоритма Фишера-Йейтса для перемешивания списка
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            // Используем UnityEngine.Random.Range для совместимости с Unity
            int k = Random.Range(0, n + 1);

            // Обмен элементов
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // Получает следующий спрайт. Если список пуст, он перемешивается и наполняется заново.
    private Sprite GetNextLoadingSprite()
    {
        // Проверяем, есть ли что-то в исходном списке
        if (_allSprites.Count == 0) return null;

        // Если доступных спрайтов не осталось, перезаполняем и перемешиваем
        if (_availableSprites.Count == 0)
        {
            _availableSprites.AddRange(_allSprites);
            ShuffleList(_availableSprites);
        }

        // Берем последний элемент из доступных, удаляем его и возвращаем
        int lastIndex = _availableSprites.Count - 1;
        Sprite nextSprite = _availableSprites[lastIndex];
        _availableSprites.RemoveAt(lastIndex);

        return nextSprite;
    }


    public void LoadScene(string sceneName)
    {
        if (loadingScreen == null || !isReady) return;
        StartCoroutine(LoadCoroutine(sceneName));
    }

    public void LoadScene(int sceneIndex)
    {
        if (loadingScreen == null || !isReady) return;
        StartCoroutine(LoadCoroutine(sceneIndex));
    }

    private IEnumerator LoadCoroutine(object sceneIdentifier)
    {
        // 1. Подготовка и активация экрана
        if (backgroundImage != null && _allSprites.Count > 0)
        {
            // *** ИСПОЛЬЗУЕМ НОВУЮ ЛОГИКУ РАНДОМА БЕЗ ПОВТОРЕНИЙ ***
            backgroundImage.sprite = GetNextLoadingSprite();
            // *******************************************************
        }

        loadingScreen.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        if (textAnimationCoroutine != null) StopCoroutine(textAnimationCoroutine);

        // 2. УЛУЧШЕНИЕ СТАБИЛЬНОСТИ
        yield return null;

        // 3. Активируем TMP и запускаем анимацию
        if (loadingText != null)
        {
            loadingText.enabled = true;
            loadingText.text = "Загрузка";
            textAnimationCoroutine = StartCoroutine(AnimateLoadingText());
        }

        // 4. Появление экрана
        yield return StartCoroutine(Fade(1f));

        // 5. Асинхронная загрузка
        AsyncOperation op = null;

        if (sceneIdentifier is string sceneName)
            op = SceneManager.LoadSceneAsync(sceneName);
        else if (sceneIdentifier is int sceneIndex)
            op = SceneManager.LoadSceneAsync((int)sceneIndex);

        if (op == null)
        {
            Debug.LogError("LoadingManager: Не удалось запустить асинхронную загрузку сцены. Проверьте имя/индекс.");
            if (textAnimationCoroutine != null) StopCoroutine(textAnimationCoroutine);
            if (loadingText != null) loadingText.enabled = false;
            yield return StartCoroutine(Fade(0f));
            loadingScreen.SetActive(false);
            yield break;
        }

        op.allowSceneActivation = true;

        float displayTimer = 0f;

        // 6. УЛУЧШЕНИЕ ЛОГИКИ ОЖИДАНИЯ
        while (op.progress < 0.9f || displayTimer < minDisplayTime)
        {
            displayTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        while (!op.isDone)
            yield return null;

        // 7. Завершение
        if (textAnimationCoroutine != null) StopCoroutine(textAnimationCoroutine);

        if (loadingText != null)
        {
            loadingText.enabled = false;
        }

        yield return StartCoroutine(Fade(0f));

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false;
        }

        loadingScreen.SetActive(false);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(time / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator AnimateLoadingText()
    {
        string baseText = "Загрузка";
        int dotCount = 0;

        while (true)
        {
            string currentText = baseText;

            for (int i = 0; i < dotCount; i++)
            {
                currentText += ".";
            }

            if (loadingText != null)
            {
                loadingText.text = currentText;
            }

            dotCount = (dotCount + 1) % 4;

            yield return new WaitForSecondsRealtime(textAnimationSpeed);
        }
    }
}