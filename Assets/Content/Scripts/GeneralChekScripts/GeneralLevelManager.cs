using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Globalization;
using YG;
using System;

[System.Serializable]
public class QuestionData
{
    public string questionText;
    public List<string> answers = new List<string>(4); 
    public int correctIndex = 0;
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}

public class GeneralLevelManager : MonoBehaviour
{
    [Header("Данные вопросов")]
    public List<QuestionData> allQuestions = new List<QuestionData>();

    [Header("Выбор сложности / дефолты")]
    public Difficulty difficulty = Difficulty.Easy;
    public bool useDifficultyDefaults = true;

    [Header("Настройки")]
    public int questionsPerRun = 10;
    public float secondsPerQuestion = 30f;
    public int initialLives = 3;
    public int initialHints = 3;

    [Header("UI - ссылки")]
    public TMP_Text QuestionText;
    public CanvasGroup QuestionCanvasGroup;

    public Button AnswerButton0;
    public Button AnswerButton1;
    public Button AnswerButton2;
    public Button AnswerButton3;

    private TMP_Text[] answerTexts = new TMP_Text[4];
    private Button[] answerButtons = new Button[4];
    private Image[] answerButtonImages = new Image[4];

    [Header("HP")]
    public GameObject HPPlayer1;
    public GameObject HPPlayer2;
    public GameObject HPPlayer3;

    [Header("Подсказки / реклама")]
    public TMP_Text ClueLimitText;
    public Button AdvertisingButton;
    public Button MinusOneButton;

    [Header("Таймер")]
    public Slider SliderComp;
    public TMP_Text StopwatchText;
    public Color timeNormalColor = Color.white;
    public Color timeDangerColor = Color.red;
    public float pulseInterval = 0.45f;

    public float pulseStartThreshold = 6f;

    [Header("Номер вопроса")]
    public TMP_Text QuestionNumberText;

    [Header("Модалки")]
    public GameObject ModalBlockerLossPanel;
    public GameObject ModalBlockerWinPanel;

    [Header("Звуки")]
    public SoundController soundController;

    [Header("Анимация вопроса")]
    public float questionPopDuration = 0.5f;
    public float questionScaleFrom = 0.9f;
    public float questionScaleOver = 1.05f;

    [Header("Цвета кнопок")]
    public Color correctBgColor = new Color(0.15f, 0.7f, 0.15f);
    public Color wrongBgColor = new Color(0.9f, 0.2f, 0.2f);
    public Color normalTextColor = Color.black;
    public Color correctTextColor = Color.white;
    public Color wrongTextColor = Color.white;

    private const float HINT_BUTTON_ALPHA = 0.95f;
    private const float HINT_TEXT_ALPHA_MULTIPLIER = 0.95f;

    private List<QuestionData> currentQuestions;
    private int currentQuestionIndex = 0;
    private int lives;
    private int hintUsesLeft = 1;
    private float timer;
    private bool questionActive = false;
    private string correctAnswer;
    private Coroutine pulseCoroutine;
    private Coroutine questionAnimCoroutine;

    private Color[] originalButtonBgColors = new Color[4];
    private Color[] originalTextColors = new Color[4];

    private Color _originalQuestionColor = Color.white;

    private int lastWrongIndex = -1;
    private bool[] disabledThisQuestion = new bool[4];

    public string rewardID;
    private float savedTimeScale = 1f;

    private Image minusOneImage;
    private Color minusOriginalColor = Color.white;
    [SerializeField] private Color minusEmptyColor = new Color(0.9f, 0.2f, 0.2f);
    private bool minusLockedRed = false;
    private Coroutine blinkCoroutine = null;

    private Coroutine shakeCoroutine = null;
    private RectTransform minusRect;
    private ButtonPulser minusPulser;
    private Vector2 minusOriginalAnchoredPos;

    // сериализуемый чтобы значение сохранялось между изменениями
    private Difficulty _lastAppliedDifficulty = Difficulty.Easy;

    public static GeneralLevelManager Instance;

    private static List<int> usedIndices = new List<int>();
    private static List<int> newUsedThisRun = new List<int>();
    private static List<int> previouslyUsedThisRun = new List<int>();

    private int initialAvailableThisRun;

    void Awake()
    {
        // Singleton: уничтожаем дубликаты, если несколько экземпляров
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;  // Выходим, чтобы не инициализировать дубликат
        }

        // Если список пуст, создаем новый, чтобы не было null-ошибок
        if (allQuestions == null) allQuestions = new List<QuestionData>();

        // Удаляем мертвые индексы вопросов, которые были удалены из инспектора
        if (usedIndices != null) usedIndices.RemoveAll(idx => idx < 0 || idx >= allQuestions.Count);
        if (previouslyUsedThisRun != null) previouslyUsedThisRun.RemoveAll(idx => idx < 0 || idx >= allQuestions.Count);

        // кнопки — присваиваем в массивы
        answerButtons[0] = AnswerButton0;
        answerButtons[1] = AnswerButton1;
        answerButtons[2] = AnswerButton2;
        answerButtons[3] = AnswerButton3;

        for (int i = 0; i < 4; i++)
        {
            if (answerButtons[i] != null)
            {
                var txt = answerButtons[i].GetComponentInChildren<TMP_Text>();
                answerTexts[i] = txt;
                var img = answerButtons[i].GetComponent<Image>();
                answerButtonImages[i] = img;
                originalButtonBgColors[i] = img != null ? img.color : Color.white;
                originalTextColors[i] = txt != null ? txt.color : Color.black;
                disabledThisQuestion[i] = false;
                int idx = i;
                answerButtons[i].onClick.AddListener(() => OnAnswerSelected(idx));
            }
        }

        if (MinusOneButton != null)
        {
            MinusOneButton.gameObject.SetActive(true);
            minusOneImage = MinusOneButton.GetComponent<Image>();
            minusRect = MinusOneButton.GetComponent<RectTransform>();
            if (minusOneImage != null) minusOriginalColor = minusOneImage.color;
            if (minusRect != null) minusOriginalAnchoredPos = minusRect.anchoredPosition;
            MinusOneButton.onClick.AddListener(OnMinusOneClicked);
        }

        if (MinusOneButton != null)
        {
            minusPulser = MinusOneButton.GetComponent<ButtonPulser>();
        }

        if (AdvertisingButton != null)
            AdvertisingButton.onClick.AddListener(OnAdvertisingClicked);

        // сохраняем цвет вопроса, выставленный в инспекторе, чтобы не перезаписывать его
        if (QuestionText != null)
        {
            _originalQuestionColor = QuestionText.color;
            QuestionText.transform.localScale = Vector3.one;
        }
        if (QuestionCanvasGroup != null) QuestionCanvasGroup.alpha = 1f;

        questionActive = false;

        // Инициализация состояния (static для сохранения в сессии)
        if (usedIndices == null) usedIndices = new List<int>();
        if (newUsedThisRun == null) newUsedThisRun = new List<int>();
        if (previouslyUsedThisRun == null) previouslyUsedThisRun = new List<int>();

        initialAvailableThisRun = 0;  // Сброс обычного int
    }

    void Start()
    {
        // Применим дефолты сложности один раз в старте (если включено)
        ApplyDifficultyDefaultsIfNeeded();
    }

    // Устанавливает значения по выбранной сложности (без проверки useDifficultyDefaults)
    void ApplyDifficultyDefaults()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                initialLives = 3;
                secondsPerQuestion = 30f;
                initialHints = 3;
                pulseStartThreshold = 6f; 
                break;
            case Difficulty.Medium:
                initialLives = 2;
                secondsPerQuestion = 25f;
                initialHints = 2;
                pulseStartThreshold = 6f;
                break;
            case Difficulty.Hard:
                initialLives = 1;
                secondsPerQuestion = 20f;
                initialHints = 1;
                pulseStartThreshold = 6; 
                break;
        }
    }

    // Вызывается из Start() и при необходимости — из OnValidate()
    void ApplyDifficultyDefaultsIfNeeded()
    {
        if (!useDifficultyDefaults) return;
        ApplyDifficultyDefaults();
    }

    // В редакторе Unity: при изменении полей в инспекторе OnValidate вызывается автоматически.
    // Здесь мы применяем дефолты сразу, когда включён useDifficultyDefaults и меняется сложность
    void OnValidate()
    {
        // Защита от вызова вне редактора не нужна — OnValidate вызывается только в редакторе.
        if (useDifficultyDefaults)
        {
            if (_lastAppliedDifficulty != difficulty)
            {
                ApplyDifficultyDefaults();
                _lastAppliedDifficulty = difficulty;
            }
        }
    }

    public void StartLevel()
    {
        // если в инспекторе включён useDifficultyDefaults — применим прямо перед стартом
        ApplyDifficultyDefaultsIfNeeded();

        lives = initialLives;
        hintUsesLeft = initialHints;
        UpdateClueUI();
        if (minusPulser != null && hintUsesLeft > 0)
        {
            minusPulser.TriggerPulse(2, 1f);
        }
        CreateQuestionSet();
        currentQuestionIndex = 0;
        SetupQuestion();
    }

    void CreateQuestionSet()
    {
        if (allQuestions.Count == 0)
        {
            Debug.LogWarning("[GeneralLevelManager] allQuestions is empty!");
            return;
        }

        List<int> allIndices = Enumerable.Range(0, allQuestions.Count).ToList();
        List<int> available = allIndices.Except(usedIndices).ToList();

        if (available.Count == 0)
        {
            usedIndices.Clear();
            available = new List<int>(allIndices);
        }

        previouslyUsedThisRun = new List<int>(usedIndices);
        List<int> selected = new List<int>();

        if (available.Count >= questionsPerRun)
        {
            ShuffleList(available);
            selected = available.Take(questionsPerRun).ToList();
        }
        else
        {
            List<int> supplementPool = previouslyUsedThisRun.Count > 0 ? previouslyUsedThisRun : allIndices;
            List<int> supplement = new List<int>();
            int needed = questionsPerRun - available.Count;

            var uniqueSupplement = supplementPool.Where(idx => !available.Contains(idx)).ToList();
            ShuffleList(uniqueSupplement);
            supplement.AddRange(uniqueSupplement.Take(Math.Min(needed, uniqueSupplement.Count)));

            while (supplement.Count < needed)
            {
                supplement.Add(supplementPool[UnityEngine.Random.Range(0, supplementPool.Count)]);
            }

            List<int> fullPool = new List<int>(available);
            fullPool.AddRange(supplement);
            ShuffleList(fullPool);
            selected = fullPool.Take(questionsPerRun).ToList();
        }

        newUsedThisRun = selected.Where(idx => !usedIndices.Contains(idx)).ToList();
        initialAvailableThisRun = available.Count;

        currentQuestions = new List<QuestionData>();
        foreach (int idx in selected)
        {
            // Проверяем, существует ли этот персонаж прямо сейчас
            if (idx >= 0 && idx < allQuestions.Count)
            {
                currentQuestions.Add(allQuestions[idx]);
            }
        }

        // Если вдруг после чистки список пуст (экстренная защита)
        if (currentQuestions.Count == 0 && allQuestions.Count > 0)
        {
            currentQuestions.Add(allQuestions[0]);
        }

        Debug.Log("Selected (в порядке игры): " + string.Join(", ", selected));
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, list.Count);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }

    // Метод для обработки проигрыша
    private void HandleLoss()
    {
        if (initialAvailableThisRun <= questionsPerRun && newUsedThisRun != null && newUsedThisRun.Count > 0)
        {
            int toLeave = 5;
            int newCount = newUsedThisRun.Count;
            List<int> toAddToUsed = new List<int>();

            // Перемешиваем, чтобы 5 оставшихся были рандомными
            ShuffleList(newUsedThisRun);

            if (newCount >= toLeave)
            {
                toAddToUsed = newUsedThisRun.Skip(toLeave).ToList();
            }
            else
            {
                int toRestore = toLeave - newCount;
                if (previouslyUsedThisRun.Count > 0)
                {
                    ShuffleList(previouslyUsedThisRun);
                    List<int> toRestoreList = previouslyUsedThisRun.Take(toRestore).ToList();
                    foreach (int idx in toRestoreList)
                    {
                        usedIndices.Remove(idx);
                    }
                }
            }

            usedIndices.AddRange(toAddToUsed);
            usedIndices = usedIndices.Distinct().ToList();
        }
    }

    void SetupQuestion()
    {
        for (int i = 0; i < 4; i++) disabledThisQuestion[i] = false;

        for (int i = 0; i < 4; i++)
        {
            if (answerButtons[i] == null) continue;
            answerButtons[i].gameObject.SetActive(true);
            if (answerButtonImages[i] != null) answerButtonImages[i].color = originalButtonBgColors[i];
            if (answerTexts[i] != null) answerTexts[i].color = originalTextColors[i];

            var animator = answerButtons[i].GetComponent<AnswerButtonAnimator>();
            if (animator != null) animator.ResetScaleImmediate();
        }

        if (currentQuestions == null || currentQuestions.Count == 0)
        {
            Debug.LogWarning("[GeneralLevelManager] currentQuestions is empty — заполните allQuestions в инспекторе.");
            return;
        }

        var qData = currentQuestions[currentQuestionIndex];

        // Заполняем текст вопроса
        if (QuestionText != null)
            QuestionText.text = qData.questionText ?? "";

        // Подготовка вариантов и правильного ответа
        List<string> options = new List<string>();
        if (qData.answers != null && qData.answers.Count > 0)
            options.AddRange(qData.answers);

        while (options.Count < 4)
            options.Add(options.Count > 0 ? options[0] : "—");

        int corrIdx = Mathf.Clamp(qData.correctIndex, 0, options.Count - 1);
        correctAnswer = options[corrIdx];

        ShuffleList(options);

        if (!options.Contains(correctAnswer))
        {
            options[0] = correctAnswer;
            ShuffleList(options);
        }

        for (int i = 0; i < 4; i++)
        {
            if (answerTexts[i] != null) answerTexts[i].text = options[i];
        }

        timer = secondsPerQuestion;
        questionActive = false;
        UpdateQuestionNumberUI();
        UpdateLivesUI();

        if (SliderComp != null) { SliderComp.maxValue = secondsPerQuestion; SliderComp.value = secondsPerQuestion; }

        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        if (StopwatchText != null) StopwatchText.color = timeNormalColor;

        if (currentQuestionIndex == 0)
        {
            if (QuestionText != null) QuestionText.color = _originalQuestionColor;
            if (QuestionText != null) QuestionText.transform.localScale = Vector3.one;
            if (QuestionCanvasGroup != null) QuestionCanvasGroup.alpha = 1f;

            questionAnimCoroutine = null;

            for (int i = 0; i < 4; i++)
            {
                if (answerButtons[i] != null) answerButtons[i].interactable = !disabledThisQuestion[i];
            }

            if (MinusOneButton != null) MinusOneButton.interactable = true;
            if (AdvertisingButton != null) AdvertisingButton.interactable = true;

            questionActive = true;
        }
        else
        {
            for (int i = 0; i < 4; i++) if (answerButtons[i] != null) answerButtons[i].interactable = false;
            if (MinusOneButton != null) MinusOneButton.interactable = false;
            if (AdvertisingButton != null) AdvertisingButton.interactable = false;

            if (questionAnimCoroutine != null) StopCoroutine(questionAnimCoroutine);
            questionAnimCoroutine = StartCoroutine(AnimateQuestionIn());
        }

        UpdateClueUI();
    }

    void NormalizeQuestionDisplay()
    {
        if (QuestionText != null)
        {
            // восстанавливаем цвет, который выставлен в инспекторе
            QuestionText.color = _originalQuestionColor;
            QuestionText.transform.localScale = Vector3.one;
        }
        if (QuestionCanvasGroup != null) QuestionCanvasGroup.alpha = 1f;
    }

    void Update()
    {
        if (!questionActive) return;

        timer -= Time.deltaTime;
        if (timer < 0) timer = 0;

        if (StopwatchText != null) StopwatchText.text = timer.ToString("F2", CultureInfo.InvariantCulture);
        if (SliderComp != null) SliderComp.value = timer;

        // Используем настраиваемый порог вместо захардкоженного 10f
        if (timer <= pulseStartThreshold && pulseCoroutine == null)
            pulseCoroutine = StartCoroutine(PulseTimeText());

        if (timer <= 0f)
        {
            questionActive = false;
            StartCoroutine(HandleWrongAndProceedTimeout());
        }
    }

    IEnumerator PulseTimeText()
    {
        bool toggle = false;
        while (true)
        {
            if (StopwatchText != null) StopwatchText.color = toggle ? timeDangerColor : timeNormalColor;
            toggle = !toggle;
            yield return new WaitForSeconds(pulseInterval);
        }
    }

    IEnumerator AnimateQuestionIn()
    {
        float t = 0f;
        float dur = questionPopDuration;
        Vector3 start = Vector3.one * questionScaleFrom;
        Vector3 over = Vector3.one * questionScaleOver;
        Vector3 end = Vector3.one;
        if (QuestionText != null) QuestionText.transform.localScale = start;
        if (QuestionCanvasGroup != null) { QuestionCanvasGroup.alpha = 0f; QuestionCanvasGroup.gameObject.SetActive(true); }

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            if (p < 0.6f)
            {
                if (QuestionText != null) QuestionText.transform.localScale = Vector3.Lerp(start, over, p / 0.6f);
            }
            else
            {
                if (QuestionText != null) QuestionText.transform.localScale = Vector3.Lerp(over, end, (p - 0.6f) / 0.4f);
            }
            if (QuestionCanvasGroup != null) QuestionCanvasGroup.alpha = Mathf.Lerp(0f, 1f, p);
            yield return null;
        }

        if (QuestionText != null) QuestionText.transform.localScale = end;
        if (QuestionCanvasGroup != null) QuestionCanvasGroup.alpha = 1f;
        if (QuestionText != null) QuestionText.color = _originalQuestionColor;
        questionAnimCoroutine = null;

        for (int i = 0; i < 4; i++)
        {
            if (answerButtons[i] != null) answerButtons[i].interactable = !disabledThisQuestion[i];
            if (disabledThisQuestion[i] && answerButtonImages[i] != null)
            {
                Color c = originalButtonBgColors[i]; c.a = HINT_BUTTON_ALPHA; answerButtonImages[i].color = c;
                Color tc = originalTextColors[i]; tc.a = Mathf.Clamp(tc.a * HINT_TEXT_ALPHA_MULTIPLIER, 0.01f, 1f); if (answerTexts[i] != null) answerTexts[i].color = tc;
            }
            else
            {
                if (answerButtonImages[i] != null) answerButtonImages[i].color = originalButtonBgColors[i];
                if (answerTexts[i] != null) answerTexts[i].color = originalTextColors[i];
            }
        }

        if (MinusOneButton != null) MinusOneButton.interactable = true;
        if (AdvertisingButton != null) AdvertisingButton.interactable = true;
        questionActive = true;

        UpdateClueUI();
    }

    IEnumerator AnimateQuestionWrongShake()
    {
        RectTransform rt = QuestionText != null ? QuestionText.GetComponent<RectTransform>() : null;
        if (rt == null) yield break;

        Vector3 orig = rt.localPosition;
        float dur = 0.28f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float dam = Mathf.Sin(t * 50f) * (1f - t / dur) * 6f;
            rt.localPosition = orig + new Vector3(dam, 0f, 0f);
            yield return null;
        }
        rt.localPosition = orig;
    }

    void OnAnswerSelected(int index)
    {
        if (!questionActive) return;
        questionActive = false;

        string chosen = answerTexts[index] != null ? answerTexts[index].text : "";
        if (chosen == correctAnswer)
        {
            if (soundController != null) soundController.SoundOfCorrectAnswer();
            SetButtonColors(index, correctBgColor, correctTextColor);

            if (minusPulser != null)
            {
                minusPulser.StopAllCoroutines();
                minusPulser.ResetScale();
            }

            StartCoroutine(HandleCorrectAndProceed(index));
        }
        else
        {
            lastWrongIndex = index;
            if (soundController != null) soundController.SoundOfWrongAnswer();
            SetButtonColors(index, wrongBgColor, wrongTextColor);
            StartCoroutine(HandleWrongAndStay(index));
            StartCoroutine(AnimateQuestionWrongShake());
        }
    }

    void SetButtonColors(int idx, Color bg, Color text)
    {
        if (answerButtonImages[idx] != null) answerButtonImages[idx].color = bg;
        if (answerTexts[idx] != null) answerTexts[idx].color = text;
    }

    IEnumerator HandleCorrectAndProceed(int chosenIdx)
    {
        yield return new WaitForSeconds(0.45f);
        NextQuestion();
    }

    IEnumerator HandleWrongAndStay(int wrongIdx)
    {
        LoseOneLife();

        if (lives <= 0)
        {
            if (soundController != null) soundController.SoundOfLoss();
            HandleLoss();
            FreezeAndShowModal(ModalBlockerLossPanel);
            yield break;
        }

        yield return new WaitForSeconds(0.6f);

        disabledThisQuestion[wrongIdx] = true;
        if (answerButtons[wrongIdx] != null) answerButtons[wrongIdx].interactable = false;

        if (answerButtonImages[wrongIdx] != null)
        {
            Color c = originalButtonBgColors[wrongIdx];
            c.a = HINT_BUTTON_ALPHA;
            answerButtonImages[wrongIdx].color = c;
        }

        if (answerTexts[wrongIdx] != null)
        {
            Color tc = originalTextColors[wrongIdx];
            tc.a = Mathf.Clamp(tc.a * HINT_TEXT_ALPHA_MULTIPLIER, 0.01f, 1f);
            answerTexts[wrongIdx].color = tc;
        }

        questionActive = true;

        if (timer <= pulseStartThreshold && pulseCoroutine == null)
            pulseCoroutine = StartCoroutine(PulseTimeText());
    }

    IEnumerator HandleWrongAndProceedTimeout()
    {
        LoseOneLife();

        if (soundController != null) soundController.SoundOfWrongAnswer();

        float wait = 0.05f;
        if (soundController != null && soundController.soundOfWrongAnswer != null)
            wait = soundController.soundOfWrongAnswer.length;

        yield return new WaitForSecondsRealtime(wait);

        if (lives <= 0)
        {
            if (soundController != null) soundController.SoundOfLoss();
            HandleLoss();
            FreezeAndShowModal(ModalBlockerLossPanel);
            yield break;
        }

        yield return new WaitForSecondsRealtime(0.6f);

        NextQuestion();
    }

    void LoseOneLife()
    {
        lives--;
        if (lives < 0) lives = 0;
        UpdateLivesUI();
    }

    void UpdateLivesUI()
    {
        if (HPPlayer1 != null) HPPlayer1.SetActive(lives >= 1);
        if (HPPlayer2 != null) HPPlayer2.SetActive(lives >= 2);
        if (HPPlayer3 != null) HPPlayer3.SetActive(lives >= 3);
    }

    void NextQuestion()
    {
        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        if (StopwatchText != null) StopwatchText.color = timeNormalColor;

        currentQuestionIndex++;
        if (currentQuestionIndex >= currentQuestions.Count)
        {
            if (soundController != null) soundController.SoundOfWin();

            if (LevelDifficultyController.Instance != null)
                LevelDifficultyController.Instance.AwardPointsForWin();

            if (minusPulser != null)
            {
                minusPulser.StopAllCoroutines();
                minusPulser.ResetScale();
            }

            // Добавляем новые использованные индексы при win
            if (newUsedThisRun != null)
            {
                usedIndices.AddRange(newUsedThisRun);
                usedIndices = usedIndices.Distinct().ToList();
            }

            FreezeAndShowModal(ModalBlockerWinPanel);
            questionActive = false;
            for (int i = 0; i < 4; i++) if (answerButtons[i] != null) answerButtons[i].interactable = false;
            if (MinusOneButton != null) MinusOneButton.interactable = false;
            if (minusOneImage != null && !minusLockedRed) minusOneImage.color = minusOriginalColor;
            if (MinusOneButton != null) MinusOneButton.interactable = false;
            return;
        }

        SetupQuestion();

        if (minusPulser != null && hintUsesLeft > 0)
        {
            minusPulser.TriggerPulse(2, 0.8f);
        }
    }

    void UpdateQuestionNumberUI()
    {
        if (QuestionNumberText != null)
            QuestionNumberText.text = $"{currentQuestionIndex + 1}/{questionsPerRun}";
    }

    void UpdateClueUI()
    {
        if (ClueLimitText != null) ClueLimitText.text = hintUsesLeft.ToString();

        if (MinusOneButton != null)
        {
            MinusOneButton.gameObject.SetActive(true);
            if (minusOneImage != null)
            {
                if (hintUsesLeft > 0)
                {
                    minusOneImage.color = minusOriginalColor;
                    minusLockedRed = false;
                }
                else
                {
                    if (minusLockedRed)
                        minusOneImage.color = minusEmptyColor;
                    else
                        minusOneImage.color = minusOriginalColor;
                }
            }
        }
    }

    void OnMinusOneClicked()
    {
        if (!questionActive) return;

        if (hintUsesLeft > 0)
        {
            List<int> bad = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                if (answerButtons[i] == null) continue;
                if (!answerButtons[i].gameObject.activeSelf) continue;
                if (!answerButtons[i].interactable) continue;
                if (answerTexts[i] == null) continue;
                if (answerTexts[i].text != correctAnswer) bad.Add(i);
            }

            if (bad.Count == 0) return;
            int pick = bad[UnityEngine.Random.Range(0, bad.Count)];

            disabledThisQuestion[pick] = true;
            if (answerButtons[pick] != null) answerButtons[pick].interactable = false;

            if (answerButtonImages[pick] != null)
            {
                Color c = originalButtonBgColors[pick];
                c.a = HINT_BUTTON_ALPHA;
                answerButtonImages[pick].color = c;
            }

            if (answerTexts[pick] != null)
            {
                Color tc = originalTextColors[pick];
                tc.a = Mathf.Clamp(tc.a * HINT_TEXT_ALPHA_MULTIPLIER, 0.01f, 1f);
                answerTexts[pick].color = tc;
            }

            hintUsesLeft--;
            UpdateClueUI();

            if (hintUsesLeft == 0 && blinkCoroutine == null)
            {
                blinkCoroutine = StartCoroutine(BlinkMinusThenLock());
            }
        }
        else
        {
            StartShakeMinusOnce();

            if (!minusLockedRed && blinkCoroutine == null)
            {
                blinkCoroutine = StartCoroutine(BlinkMinusThenLock());
            }
            else
            {
                if (minusOneImage != null) minusOneImage.color = minusEmptyColor;
            }
        }
    }

    IEnumerator BlinkMinusThenLock()
    {
        if (minusOneImage == null)
        {
            minusLockedRed = true;
            blinkCoroutine = null;
            yield break;
        }

        int flashes = 3;
        float interval = 0.2f;
        for (int i = 0; i < flashes; i++)
        {
            minusOneImage.color = minusEmptyColor;
            yield return new WaitForSeconds(interval);
            minusOneImage.color = minusOriginalColor;
            yield return new WaitForSeconds(interval);
        }

        minusOneImage.color = minusEmptyColor;
        minusLockedRed = true;
        blinkCoroutine = null;
    }

    void StartShakeMinusOnce()
    {
        if (minusRect == null) return;
        minusRect.anchoredPosition = minusOriginalAnchoredPos;
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRect(minusRect, 0.28f, 6f));
    }

    IEnumerator ShakeRect(RectTransform rt, float duration, float magnitude)
    {
        Vector2 orig = minusOriginalAnchoredPos;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude * (1f - elapsed / duration);
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude * (1f - elapsed / duration);
            rt.anchoredPosition = orig + new Vector2(x, y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = orig;
        shakeCoroutine = null;
    }

    void OnAdvertisingClicked()
    {
        AdvertisingButton.interactable = false;

        if (minusPulser != null)
        {
            minusPulser.StopAllCoroutines();
            minusPulser.ResetScale();
        }

        YG2.RewardedAdvShow(rewardID, () =>
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }

            hintUsesLeft += 2;
            minusLockedRed = false;
            if (minusOneImage != null) minusOneImage.color = minusOriginalColor;
            UpdateClueUI();
            AdvertisingButton.interactable = true;
        });
    }

    public void ContinueFromAdReward()
    {
        lives = Mathf.Min(initialLives, lives + 1);
        UpdateLivesUI();

        if (ModalBlockerLossPanel != null && ModalBlockerLossPanel.activeSelf)
            ModalBlockerLossPanel.SetActive(false);

        if (lastWrongIndex >= 0 && lastWrongIndex < 4)
        {
            disabledThisQuestion[lastWrongIndex] = true;
            if (answerButtons[lastWrongIndex] != null) answerButtons[lastWrongIndex].interactable = false;

            if (answerButtonImages[lastWrongIndex] != null)
            {
                Color c = originalButtonBgColors[lastWrongIndex];
                c.a = HINT_BUTTON_ALPHA;
                answerButtonImages[lastWrongIndex].color = c;
            }

            if (answerTexts[lastWrongIndex] != null)
            {
                Color tc = originalTextColors[lastWrongIndex];
                tc.a = Mathf.Clamp(tc.a * HINT_TEXT_ALPHA_MULTIPLIER, 0.01f, 1f);
                answerTexts[lastWrongIndex].color = tc;
            }
        }

        timer = secondsPerQuestion;
        if (SliderComp != null) SliderComp.value = timer;

        if (StopwatchText != null)
        {
            StopwatchText.text = timer.ToString("F2", CultureInfo.InvariantCulture);
            StopwatchText.color = timeNormalColor;
        }

        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }

        if (QuestionText != null) QuestionText.transform.localScale = Vector3.one;
        if (QuestionCanvasGroup != null) QuestionCanvasGroup.alpha = 1f;
        if (QuestionText != null) QuestionText.color = _originalQuestionColor;
        questionAnimCoroutine = null;

        ResumeFromModal();
        lastWrongIndex = -1;
    }

    void FreezeAndShowModal(GameObject modal)
    {
        questionActive = false;

        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        if (questionAnimCoroutine != null) { StopCoroutine(questionAnimCoroutine); questionAnimCoroutine = null; }

        if (minusPulser != null)
        {
            minusPulser.StopAllCoroutines();
            minusPulser.ResetScale();
        }

        if (QuestionText != null) QuestionText.transform.localScale = Vector3.one;
        if (QuestionCanvasGroup != null) QuestionCanvasGroup.alpha = 1f;
        if (QuestionText != null) QuestionText.color = _originalQuestionColor;

        for (int i = 0; i < 4; i++) if (answerButtons[i] != null) answerButtons[i].interactable = false;
        if (MinusOneButton != null) MinusOneButton.interactable = false;

        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (modal != null) modal.SetActive(true);
        if (AdvertisingButton != null) AdvertisingButton.interactable = true;
    }

    public void ResumeFromModal()
    {
        Time.timeScale = savedTimeScale;

        for (int i = 0; i < 4; i++)
        {
            if (answerButtons[i] != null) answerButtons[i].interactable = !disabledThisQuestion[i];

            if (disabledThisQuestion[i])
            {
                if (answerButtonImages[i] != null)
                {
                    Color c = originalButtonBgColors[i]; c.a = HINT_BUTTON_ALPHA; answerButtonImages[i].color = c;
                }
                if (answerTexts[i] != null)
                {
                    Color tc = originalTextColors[i]; tc.a = Mathf.Clamp(tc.a * HINT_TEXT_ALPHA_MULTIPLIER, 0.01f, 1f); answerTexts[i].color = tc;
                }
            }
            else
            {
                if (answerButtonImages[i] != null) answerButtonImages[i].color = originalButtonBgColors[i];
                if (answerTexts[i] != null) answerTexts[i].color = originalTextColors[i];
            }
        }

        if (MinusOneButton != null) MinusOneButton.interactable = true;
        if (AdvertisingButton != null) AdvertisingButton.interactable = true;

        questionActive = true;

        if (timer <= pulseStartThreshold && pulseCoroutine == null)
            pulseCoroutine = StartCoroutine(PulseTimeText());

        UpdateClueUI();
    }
}
