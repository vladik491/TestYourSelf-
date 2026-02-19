using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Globalization;
using YG;

[System.Serializable]
public class BunchQuestionData
{
    public string firstElement;
    public string secondElement;
    public List<string> answers = new List<string>(4);
    public int correctIndex = 0;
}

public enum BunchDifficulty
{
    Easy,
    Medium,
    Hard
}

public class BunchLevelManager : MonoBehaviour
{
    [Header("Данные вопросов")]
    public List<BunchQuestionData> allQuestions = new List<BunchQuestionData>();

    [Header("Выбор сложности / дефолты")]
    public BunchDifficulty difficulty = BunchDifficulty.Easy;
    public bool useDifficultyDefaults = true;

    [Header("Настройки")]
    public int questionsPerRun = 10;
    public float secondsPerQuestion = 20f;
    public int initialLives = 3;
    public int initialHints = 3;

    [Header("UI - связующие элементы")]
    public TMP_Text BunchText1;
    public TMP_Text BunchText2; // Средний элемент с "?"
    public TMP_Text BunchText3;

    [Header("UI - ответы")]
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

    [Header("Анимация связки")]
    public float elementPopDuration = 0.5f;
    public float elementScaleFrom = 0.9f;
    public float elementScaleOver = 1.05f;

    [Header("Цвета кнопок")]
    public Color correctBgColor = new Color(0.15f, 0.7f, 0.15f);
    public Color wrongBgColor = new Color(0.9f, 0.2f, 0.2f);
    public Color normalTextColor = Color.black;
    public Color correctTextColor = Color.white;
    public Color wrongTextColor = Color.white;

    private const float HINT_BUTTON_ALPHA = 0.95f;
    private const float HINT_TEXT_ALPHA_MULTIPLIER = 0.95f;

    private List<BunchQuestionData> currentQuestions;
    private int currentQuestionIndex = 0;
    private int lives;
    private int hintUsesLeft = 1;
    private float timer;
    private bool questionActive = false;
    private string correctAnswer;
    private Coroutine pulseCoroutine;
    private Coroutine elementAnimCoroutine;

    private Color[] originalButtonBgColors = new Color[4];
    private Color[] originalTextColors = new Color[4];

    private Color _originalBunch1Color = Color.white;
    private Color _originalBunch3Color = Color.white;

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

    private BunchDifficulty _lastAppliedDifficulty = BunchDifficulty.Easy;

    public static BunchLevelManager Instance;

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
        if (allQuestions == null) allQuestions = new List<BunchQuestionData>();

        // Чистим статику от удаленных вопросов
        if (usedIndices != null) usedIndices.RemoveAll(idx => idx < 0 || idx >= allQuestions.Count);
        if (previouslyUsedThisRun != null) previouslyUsedThisRun.RemoveAll(idx => idx < 0 || idx >= allQuestions.Count);

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
        if (BunchText1 != null) _originalBunch1Color = BunchText1.color;
        if (BunchText3 != null) _originalBunch3Color = BunchText3.color;
        if (BunchText2 != null) BunchText2.text = "............?";
        questionActive = false;

        // Инициализация состояния (static для сохранения в сессии)
        if (usedIndices == null) usedIndices = new List<int>();
        if (newUsedThisRun == null) newUsedThisRun = new List<int>();
        if (previouslyUsedThisRun == null) previouslyUsedThisRun = new List<int>();

        initialAvailableThisRun = 0;  // Сброс обычного int

    }

    void Start()
    {
        ApplyDifficultyDefaultsIfNeeded();
    }

    void ApplyDifficultyDefaults()
    {
        switch (difficulty)
        {
            case BunchDifficulty.Easy:
                initialLives = 3;
                secondsPerQuestion = 25f;
                initialHints = 3;
                pulseStartThreshold = 6f;
                break;
            case BunchDifficulty.Medium:
                initialLives = 2;
                secondsPerQuestion = 20f;
                initialHints = 2;
                pulseStartThreshold = 6f;
                break;
            case BunchDifficulty.Hard:
                initialLives = 1;
                secondsPerQuestion = 15f;
                initialHints = 1;
                pulseStartThreshold = 6f;
                break;
        }
    }

    void ApplyDifficultyDefaultsIfNeeded()
    {
        if (!useDifficultyDefaults) return;
        ApplyDifficultyDefaults();
    }

    void OnValidate()
    {
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
            Debug.LogWarning("[BunchLevelManager] allQuestions is empty — заполните в инспекторе.");
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
            // Этап 1 или 2: просто рандомные 10 из доступных
            ShuffleList(available);
            selected = available.Take(questionsPerRun).ToList();
        }
        else
        {
            // === КЛЮЧЕВОЙ ФИКС: ВСЁ В ОДИН СПИСОК И ПЕРЕМЕШАТЬ ===
            List<int> supplementPool = previouslyUsedThisRun.Count > 0 ? previouslyUsedThisRun : allIndices;

            // Берём нужное количество из used (без повторов, если возможно)
            List<int> supplement = new List<int>();
            int needed = questionsPerRun - available.Count;

            // Уникальные из used
            var uniqueSupplement = supplementPool.Where(idx => !available.Contains(idx)).ToList();
            ShuffleList(uniqueSupplement);
            supplement.AddRange(uniqueSupplement.Take(Math.Min(needed, uniqueSupplement.Count)).ToList());

            // Если не хватает — с повторениями
            while (supplement.Count < needed)
            {
                supplement.Add(supplementPool[UnityEngine.Random.Range(0, supplementPool.Count)]);
            }

            // === ВСЁ ВМЕСТЕ: available + supplement ===
            List<int> fullPool = new List<int>(available);
            fullPool.AddRange(supplement);

            // === ПЕРЕМЕШАТЬ ВСЁ ВМЕСТЕ ===
            ShuffleList(fullPool);

            // Берём первые 10 после перемешивания
            selected = fullPool.Take(questionsPerRun).ToList();
        }

        newUsedThisRun = selected.Where(idx => allIndices.Except(usedIndices).Contains(idx)).ToList();
        initialAvailableThisRun = available.Count;

        currentQuestions = new List<BunchQuestionData>();
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
            Debug.LogWarning("[BunchLevelManager] currentQuestions is empty — заполните allQuestions в инспекторе.");
            return;
        }

        var qData = currentQuestions[currentQuestionIndex];

        // Устанавливаем элементы связки
        if (BunchText1 != null)
            BunchText1.text = qData.firstElement ?? "";
        if (BunchText3 != null)
            BunchText3.text = qData.secondElement ?? "";

        // Подготовка вариантов ответов
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
            NormalizeBunchDisplay();
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

            if (elementAnimCoroutine != null) StopCoroutine(elementAnimCoroutine);
            elementAnimCoroutine = StartCoroutine(AnimateElementsIn());
        }

        UpdateClueUI();
    }

    void NormalizeBunchDisplay()
    {
        if (BunchText1 != null)
        {
            BunchText1.color = _originalBunch1Color;
            BunchText1.transform.localScale = Vector3.one;
        }
        if (BunchText3 != null)
        {
            BunchText3.color = _originalBunch3Color;
            BunchText3.transform.localScale = Vector3.one;
        }
    }

    void Update()
    {
        if (!questionActive) return;

        timer -= Time.deltaTime;
        if (timer < 0) timer = 0;

        if (StopwatchText != null) StopwatchText.text = timer.ToString("F2", CultureInfo.InvariantCulture);
        if (SliderComp != null) SliderComp.value = timer;

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

    IEnumerator AnimateElementsIn()
    {
        float t = 0f;
        float dur = elementPopDuration;
        Vector3 start = Vector3.one * elementScaleFrom;
        Vector3 over = Vector3.one * elementScaleOver;
        Vector3 end = Vector3.one;

        if (BunchText1 != null) BunchText1.transform.localScale = start;
        if (BunchText3 != null) BunchText3.transform.localScale = start;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            if (p < 0.6f)
            {
                if (BunchText1 != null) BunchText1.transform.localScale = Vector3.Lerp(start, over, p / 0.6f);
                if (BunchText3 != null) BunchText3.transform.localScale = Vector3.Lerp(start, over, p / 0.6f);
            }
            else
            {
                if (BunchText1 != null) BunchText1.transform.localScale = Vector3.Lerp(over, end, (p - 0.6f) / 0.4f);
                if (BunchText3 != null) BunchText3.transform.localScale = Vector3.Lerp(over, end, (p - 0.6f) / 0.4f);
            }
            yield return null;
        }

        if (BunchText1 != null) BunchText1.transform.localScale = end;
        if (BunchText3 != null) BunchText3.transform.localScale = end;
        NormalizeBunchDisplay();
        elementAnimCoroutine = null;

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

    IEnumerator AnimateBunchWrongShake()
    {
        RectTransform rt1 = BunchText1 != null ? BunchText1.GetComponent<RectTransform>() : null;
        RectTransform rt3 = BunchText3 != null ? BunchText3.GetComponent<RectTransform>() : null;

        Vector3 orig1 = rt1 != null ? rt1.localPosition : Vector3.zero;
        Vector3 orig3 = rt3 != null ? rt3.localPosition : Vector3.zero;

        float dur = 0.28f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float dam = Mathf.Sin(t * 50f) * (1f - t / dur) * 6f;
            if (rt1 != null) rt1.localPosition = orig1 + new Vector3(dam, 0f, 0f);
            if (rt3 != null) rt3.localPosition = orig3 + new Vector3(dam, 0f, 0f);
            yield return null;
        }
        if (rt1 != null) rt1.localPosition = orig1;
        if (rt3 != null) rt3.localPosition = orig3;
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
            StartCoroutine(AnimateBunchWrongShake());
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
            // Здесь логика тряски:
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

        NormalizeBunchDisplay();
        elementAnimCoroutine = null;

        ResumeFromModal();
        lastWrongIndex = -1;
    }

    void FreezeAndShowModal(GameObject modal)
    {
        questionActive = false;

        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        if (elementAnimCoroutine != null) { StopCoroutine(elementAnimCoroutine); elementAnimCoroutine = null; }

        if (minusPulser != null)
        {
            minusPulser.StopAllCoroutines();
            minusPulser.ResetScale();
        }

        NormalizeBunchDisplay();

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