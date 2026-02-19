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
public class CharacterDataHard
{
    // Данные персонажа: имя, портрет, пол и возможные фамилии
    public string characterName;
    public Sprite portrait;
    public Gender gender = Gender.Other;
    public List<string> surnames = new List<string>();
}


public class HardLevelManager : MonoBehaviour
{
    [Header("Данные персонажей (заполнить в инспекторе)")]
    public List<CharacterDataHard> allCharacters = new List<CharacterDataHard>();

    [Header("Настройки")]
    public int questionsPerRun = 10;
    public float secondsPerQuestion = 10f;

    [Header("UI - ссылки")]
    public Image PhotoImage;
    public CanvasGroup PhotoCanvasGroup;
    public Button AnswerLeftDownButton;
    public Button AnswerRightDownButton;
    public Button AnswerLeftUpButton;
    public Button AnswerRightUpButton;
    private TMP_Text[] answerTexts = new TMP_Text[4];
    private Button[] answerButtons = new Button[4];
    private Image[] answerButtonImages = new Image[4];

    [Header("HP (жизни)")]
    public GameObject HPPlayer1;
    public GameObject HPPlayer2;
    public GameObject HPPlayer3;

    [Header("Подсказки / реклама")]
    public TMP_Text ClueLimitText;
    public Button AdvertisingButton;

    [Header("Таймер")]
    public Slider SliderComp;
    public Image SliderFillImage;
    public TMP_Text StopwatchText;
    public Color timeNormalColor = Color.white;
    public Color timeDangerColor = Color.red;
    public float pulseInterval = 0.45f;
    public float pulseStartThreshold = 6f;

    [Header("Номер вопроса")]
    public TMP_Text QuestionNumberText;

    [Header("Кнопка убрать -1")]
    public Button MinusOneButton;

    [Header("Модалки")]
    public GameObject ModalBlockerLossPanel;
    public GameObject ModalBlockerWinPanel;

    [Header("Звуки")]
    public SoundController soundController;

    [Header("Анимация портрета")]
    public float portraitPopDuration = 0.5f;
    public float portraitScaleFrom = 0.9f;
    public float portraitScaleOver = 1.05f;

    [Header("Цвета кнопок")]
    public Color correctBgColor = new Color(0.15f, 0.7f, 0.15f);
    public Color wrongBgColor = new Color(0.9f, 0.2f, 0.2f);
    public Color normalTextColor = Color.black;
    public Color correctTextColor = Color.white;
    public Color wrongTextColor = Color.white;

    private const float HINT_BUTTON_ALPHA = 0.95f;
    private const float HINT_TEXT_ALPHA_MULTIPLIER = 0.95f;

    private List<CharacterDataHard> currentQuestions;
    private int currentQuestionIndex = 0;
    private int lives;
    private int hintUsesLeft = 1;
    private float timer;
    private bool questionActive = false;
    private string correctAnswer;
    private Gender correctGender;
    private Coroutine pulseCoroutine;
    private Coroutine portraitAnimCoroutine;

    private Color[] originalButtonBgColors = new Color[4];
    private Color[] originalTextColors = new Color[4];

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

    public static HardLevelManager Instance;

    // === СТАТИЧЕСКИЕ ПОЛЯ ДЛЯ СОХРАНЕНИЯ СОСТОЯНИЯ ===
    private static List<int> usedIndices = new List<int>();
    private static List<int> newUsedThisRun = new List<int>();
    private static List<int> previouslyUsedThisRun = new List<int>();

    private int initialAvailableThisRun;

    // Awake: привязывает UI элементы и подписывает колбэки кнопок
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
        if (allCharacters == null) allCharacters = new List<CharacterDataHard>();

        // ЧИСТКА ПАМЯТИ: Удаляем из использованных те индексы, которых больше не существует
        if (usedIndices != null) usedIndices.RemoveAll(idx => idx < 0 || idx >= allCharacters.Count);
        if (previouslyUsedThisRun != null) previouslyUsedThisRun.RemoveAll(idx => idx < 0 || idx >= allCharacters.Count);

        answerButtons[0] = AnswerLeftDownButton;

        answerButtons[0] = AnswerLeftDownButton;
        answerButtons[1] = AnswerRightDownButton;
        answerButtons[2] = AnswerLeftUpButton;
        answerButtons[3] = AnswerRightUpButton;

        for (int i = 0; i < 4; i++)
        {
            if (answerButtons[i] == null) continue;
            var txt = answerButtons[i].GetComponentInChildren<TMP_Text>();
            answerTexts[i] = txt;
            var img = answerButtons[i].GetComponent<Image>();
            answerButtonImages[i] = img;
            if (img != null) originalButtonBgColors[i] = img.color;
            if (txt != null) originalTextColors[i] = txt.color;
            disabledThisQuestion[i] = false;
            int idx = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(idx));
        }

        if (MinusOneButton != null)
        {
            MinusOneButton.onClick.AddListener(OnMinusOneClicked);
            minusOneImage = MinusOneButton.GetComponent<Image>();
            minusRect = MinusOneButton.GetComponent<RectTransform>();
            if (minusOneImage != null) minusOriginalColor = minusOneImage.color;
            if (minusRect != null) minusOriginalAnchoredPos = minusRect.anchoredPosition;
        }

        if (MinusOneButton != null)
        {
            minusPulser = MinusOneButton.GetComponent<ButtonPulser>();
        }

        if (AdvertisingButton != null) AdvertisingButton.onClick.AddListener(OnAdvertisingClicked);

        if (PhotoImage != null)
        {
            PhotoImage.color = Color.white;
            PhotoImage.canvasRenderer.SetAlpha(1f);
            PhotoImage.transform.localScale = Vector3.one;
        }
        if (PhotoCanvasGroup != null) PhotoCanvasGroup.alpha = 1f;

        questionActive = false;

        // === Инициализация static ===
        if (usedIndices == null) usedIndices = new List<int>();
        if (newUsedThisRun == null) newUsedThisRun = new List<int>();
        if (previouslyUsedThisRun == null) previouslyUsedThisRun = new List<int>();

        initialAvailableThisRun = 0;  // Сброс обычного int
    }

    // StartLevel: стартует уровень, сбрасывает жизни и подсказки, генерирует вопросы
    public void StartLevel()
    {
        lives = 1;
        hintUsesLeft = 1;
        UpdateClueUI();
        if (minusPulser != null && hintUsesLeft > 0)
        {
            minusPulser.TriggerPulse(2, 1f);
        }
        CreateQuestionSet();
        currentQuestionIndex = 0;
        SetupQuestion();

        if (LevelDifficultyController.Instance != null)
            LevelDifficultyController.Instance.CurrentDifficulty = LevelDifficultyController.Difficulty.Hard;
    }

    // Создаёт перемешанный список вопросов-символов и урезает до количества вопросов за раунд
    void CreateQuestionSet()
    {
        if (allCharacters.Count == 0)
        {
            Debug.LogWarning("[EasyLevelManager] allCharacters пуст!");
            return;
        }

        List<int> allIndices = Enumerable.Range(0, allCharacters.Count).ToList();
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

        newUsedThisRun = selected.Where(idx => allIndices.Except(usedIndices).Contains(idx)).ToList();
        initialAvailableThisRun = available.Count;

        currentQuestions = new List<CharacterDataHard>();
        foreach (int idx in selected)
        {
            // Проверяем, существует ли этот персонаж прямо сейчас
            if (idx >= 0 && idx < allCharacters.Count)
            {
                currentQuestions.Add(allCharacters[idx]);
            }
        }

        // Если вдруг после чистки список пуст (экстренная защита)
        if (currentQuestions.Count == 0 && allCharacters.Count > 0)
        {
            currentQuestions.Add(allCharacters[0]);
        }

        Debug.Log("Selected (в порядке игры): " + string.Join(", ", selected));
    }

    // Общая функция для перемешивания списка (Fisher-Yates)
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

    public void HandleLoss()
    {
        if (newUsedThisRun == null || newUsedThisRun.Count == 0) return;

        int toLeave = 5;

        ShuffleList(newUsedThisRun);

        List<int> toKeep = newUsedThisRun.Take(toLeave).ToList();
        List<int> toExclude = newUsedThisRun.Skip(toLeave).ToList();

        usedIndices.AddRange(toExclude);
        usedIndices = usedIndices.Distinct().ToList();

        newUsedThisRun = toKeep;

        if (newUsedThisRun.Count < toLeave)
        {
            int toRestore = toLeave - newUsedThisRun.Count;
            if (previouslyUsedThisRun.Count > 0)
            {
                ShuffleList(previouslyUsedThisRun);
                var toRestoreList = previouslyUsedThisRun.Take(toRestore).ToList();
                foreach (int idx in toRestoreList)
                {
                    usedIndices.Remove(idx);
                    newUsedThisRun.Add(idx);
                }
            }
        }
    }

    // Настройка текущего вопроса: наполнить ответы, обновить таймер и UI
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

        var charData = currentQuestions[currentQuestionIndex];
        if (PhotoImage != null) PhotoImage.sprite = charData.portrait;
        NormalizePhotoImage();

        correctAnswer = charData.characterName;
        correctGender = charData.gender;

        List<string> options = new List<string> { correctAnswer };
        HashSet<string> used = new HashSet<string> { correctAnswer };

        // 1) пытаемся добавить персонажей с совпадающей фамилией
        if (charData.surnames != null && charData.surnames.Count > 0)
        {
            var famMatches = allCharacters
                .Where(c => c.characterName != correctAnswer && c.surnames != null
                            && c.surnames.Any(s => charData.surnames.Contains(s)))
                .Select(c => c.characterName)
                .ToList();
            ShuffleList(famMatches);
            foreach (var n in famMatches)
            {
                if (options.Count >= 4) break;
                if (!used.Contains(n)) { options.Add(n); used.Add(n); }
            }
        }

        // 2) добавляем персонажей того же пола
        var poolSameGender = allCharacters
            .Where(c => c.characterName != correctAnswer && c.gender == correctGender && !used.Contains(c.characterName))
            .Select(c => c.characterName)
            .ToList();
        ShuffleList(poolSameGender);
        foreach (var n in poolSameGender)
        {
            if (options.Count >= 4) break;
            options.Add(n); used.Add(n);
        }

        // 3) добавляем любых оставшихся
        var poolAny = allCharacters.Select(c => c.characterName).Where(n => !used.Contains(n)).ToList();
        ShuffleList(poolAny);
        foreach (var n in poolAny)
        {
            if (options.Count >= 4) break;
            options.Add(n); used.Add(n);
        }

        // подстраховка, если вдруг меньше 4 опций
        int pi = 0;
        while (options.Count < 4)
        {
            options.Add(options[pi % options.Count]);
            pi++;
        }

        ShuffleList(options);
        for (int i = 0; i < 4; i++)
        {
            if (answerTexts[i] != null) answerTexts[i].text = options[i];
        }

        timer = secondsPerQuestion;
        questionActive = false;
        UpdateQuestionNumberUI();
        UpdateLivesUI();

        if (SliderComp != null) { SliderComp.maxValue = secondsPerQuestion; SliderComp.value = secondsPerQuestion; }
        if (SliderFillImage != null) SliderFillImage.fillAmount = 1f;

        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        StopwatchText.color = timeNormalColor;

        // --- Поведение с анимацией портрета (копия логики из MediumLevelManager) ---
        if (currentQuestionIndex == 0)
        {
            if (PhotoImage != null) PhotoImage.transform.localScale = Vector3.one;
            if (PhotoCanvasGroup != null) PhotoCanvasGroup.alpha = 1f;
            if (PhotoImage != null) { PhotoImage.color = Color.white; PhotoImage.canvasRenderer.SetAlpha(1f); }
            portraitAnimCoroutine = null;

            for (int i = 0; i < 4; i++)
            {
                if (answerButtons[i] != null) answerButtons[i].interactable = !disabledThisQuestion[i];
                if (disabledThisQuestion[i] && answerButtonImages[i] != null && answerTexts[i] != null)
                {
                    Color c = originalButtonBgColors[i]; c.a = HINT_BUTTON_ALPHA; answerButtonImages[i].color = c;
                    Color tc = originalTextColors[i]; tc.a = Mathf.Clamp(tc.a * HINT_TEXT_ALPHA_MULTIPLIER, 0.01f, 1f); answerTexts[i].color = tc;
                }
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

            if (portraitAnimCoroutine != null) StopCoroutine(portraitAnimCoroutine);
            portraitAnimCoroutine = StartCoroutine(AnimatePortraitIn());
        }

        UpdateClueUI();

        if (minusPulser != null && hintUsesLeft > 0)
        {
            minusPulser.TriggerPulse(2, 0.8f);
        }
    }

    // Сбрасывает визуальные параметры изображения (на всякий случай)
    void NormalizePhotoImage()
    {
        if (PhotoImage == null) return;
        PhotoImage.color = Color.white;
        PhotoImage.canvasRenderer.SetAlpha(1f);
        PhotoImage.material = null;
        PhotoImage.SetVerticesDirty();
        PhotoImage.SetMaterialDirty();
        if (PhotoCanvasGroup != null) PhotoCanvasGroup.alpha = 1f;
    }

    // Основной игровой цикл: обновляет таймер и триггерит тайм-аут
    void Update()
    {
        if (!questionActive) return;

        timer -= Time.deltaTime;
        if (timer < 0) timer = 0;

        if (StopwatchText != null) StopwatchText.text = timer.ToString("F2", CultureInfo.InvariantCulture);
        if (SliderComp != null) SliderComp.value = timer;
        else if (SliderFillImage != null) SliderFillImage.fillAmount = timer / secondsPerQuestion;

        if (timer <= pulseStartThreshold && pulseCoroutine == null)
            pulseCoroutine = StartCoroutine(PulseTimeText());

        if (timer <= 0f)
        {
            questionActive = false;
            StartCoroutine(HandleWrongAndProceedTimeout());
        }
    }

    // Мигание текста таймера при опасном времени
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

    // Корутина: анимация появления портрета (копия из MediumLevelManager)
    IEnumerator AnimatePortraitIn()
    {
        float t = 0f;
        float dur = portraitPopDuration;
        Vector3 start = Vector3.one * portraitScaleFrom;
        Vector3 over = Vector3.one * portraitScaleOver;
        Vector3 end = Vector3.one;

        if (PhotoImage != null) PhotoImage.transform.localScale = start;
        if (PhotoCanvasGroup != null) { PhotoCanvasGroup.alpha = 0f; PhotoCanvasGroup.gameObject.SetActive(true); }

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            if (p < 0.6f)
            {
                if (PhotoImage != null) PhotoImage.transform.localScale = Vector3.Lerp(start, over, p / 0.6f);
            }
            else
            {
                if (PhotoImage != null) PhotoImage.transform.localScale = Vector3.Lerp(over, end, (p - 0.6f) / 0.4f);
            }
            if (PhotoCanvasGroup != null) PhotoCanvasGroup.alpha = Mathf.Lerp(0f, 1f, p);
            yield return null;
        }

        if (PhotoImage != null) PhotoImage.transform.localScale = end;
        if (PhotoCanvasGroup != null) PhotoCanvasGroup.alpha = 1f;
        if (PhotoImage != null) { PhotoImage.color = Color.white; PhotoImage.canvasRenderer.SetAlpha(1f); }
        portraitAnimCoroutine = null;

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

        UpdateClueUI();
    }

    // Анимация тряски портрета при неверном ответе
    IEnumerator AnimatePortraitWrongShake()
    {
        if (PhotoImage == null) yield break;
        Vector3 orig = PhotoImage.transform.localPosition;
        float dur = 0.28f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float dam = Mathf.Sin(t * 50f) * (1f - t / dur) * 6f;
            PhotoImage.transform.localPosition = orig + new Vector3(dam, 0f, 0f);
            yield return null;
        }
        PhotoImage.transform.localPosition = orig;
    }

    // Обработчик выбора ответа: проверяет правильность и стартует соответствующую логику
    void OnAnswerSelected(int index)
    {
        if (!questionActive) return;
        questionActive = false;

        if (index < 0 || index >= 4) return;
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
            StartCoroutine(AnimatePortraitWrongShake());
        }
    }

    // Меняет цвета кнопки (фон и текст)
    void SetButtonColors(int idx, Color bg, Color text)
    {
        if (answerButtonImages[idx] != null) answerButtonImages[idx].color = bg;
        if (answerTexts[idx] != null) answerTexts[idx].color = text;
    }

    // Корутин для правильного ответа: пауза, затем следующий вопрос
    IEnumerator HandleCorrectAndProceed(int chosenIdx)
    {
        yield return new WaitForSeconds(0.45f);
        NextQuestion();
    }

    // Корутин для неверного ответа: снимает жизнь, затем либо блокирует вариант, либо заканчивает игру
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

        if (wrongIdx >= 0 && wrongIdx < 4)
        {
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
        }

        questionActive = true;

        if (timer <= pulseStartThreshold && pulseCoroutine == null)
            pulseCoroutine = StartCoroutine(PulseTimeText());
    }

    // Корутин для тайм-аута: снимает жизнь и либо следующий вопрос, либо конец
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

    // Уменьшает число жизней на одну (с защитой от отрицательных значений)
    void LoseOneLife()
    {
        lives--;
        if (lives < 0) lives = 0;
        UpdateLivesUI();
    }

    // Обновляет UI жизней (включает/выключает иконки)
    void UpdateLivesUI()
    {
        if (HPPlayer1 != null) HPPlayer1.SetActive(lives >= 1);
        if (HPPlayer2 != null) HPPlayer2.SetActive(lives >= 2);
        if (HPPlayer3 != null) HPPlayer3.SetActive(lives >= 3);
    }

    // Переход к следующему вопросу или показ панели победы
    void NextQuestion()
    {
        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        StopwatchText.color = timeNormalColor;

        currentQuestionIndex++;
        if (currentQuestionIndex >= currentQuestions.Count)
        {
            if (soundController != null) soundController.SoundOfWin();

            if (LevelDifficultyController.Instance != null)
                LevelDifficultyController.Instance.AwardPointsForWin();

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

            if (minusOneImage != null && !minusLockedRed)
                minusOneImage.color = minusOriginalColor;

            return;
        }

        SetupQuestion();
    }

    // Обновляет номер текущего вопроса в UI
    void UpdateQuestionNumberUI()
    {
        if (QuestionNumberText != null)
            QuestionNumberText.text = $"{currentQuestionIndex + 1}/{questionsPerRun}";
    }

    // Обновляет UI подсказок: количество и состояние кнопки "-1"
    void UpdateClueUI()
    {
        if (ClueLimitText != null) ClueLimitText.text = hintUsesLeft.ToString();

        if (MinusOneButton != null)
        {
            // оставляем кнопку видимой — интеракт может контролироваться отдельно
            MinusOneButton.gameObject.SetActive(true);

            if (minusOneImage != null)
            {
                if (hintUsesLeft > 0)
                {
                    // подсказки есть — оригинальный цвет
                    minusOneImage.color = minusOriginalColor;
                    minusLockedRed = false;
                }
                else
                {
                    // подсказок нет — если уже в "заблокированном" красном состоянии,
                    // показываем красный, иначе показываем полупрозрачный оригинал
                    if (minusLockedRed)
                        minusOneImage.color = minusEmptyColor;
                    else
                    {
                        Color c = minusOriginalColor;
                        c.a = HINT_BUTTON_ALPHA; // полупрозрачный
                        minusOneImage.color = c;
                    }
                }
            }
        }
    }


    // Нажатие на кнопку -1: убирает один неверный вариант или показывает анимацию, если подсказок нет
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
                if (answerTexts[i] != null && answerTexts[i].text != correctAnswer) bad.Add(i);
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

    // Мигание кнопки -1, затем установка её в "заблокированное" состояние
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

    // Запустить одноразовую тряску кнопки "-1"
    void StartShakeMinusOnce()
    {
        if (minusRect == null) return;
        minusRect.anchoredPosition = minusOriginalAnchoredPos;
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRect(minusRect, 0.28f, 6f));
    }

    // Корутина: тряска RectTransform
    IEnumerator ShakeRect(RectTransform rt, float duration, float magnitude)
    {
        Vector2 orig = minusOriginalAnchoredPos;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float damper = 1f - (elapsed / duration);
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude * damper;
            rt.anchoredPosition = orig + new Vector2(x, y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = orig;
        shakeCoroutine = null;
    }

    // Нажатие на рекламу: показывает рекламный ролик и даёт подсказку в колбэке
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

    // Продолжить после просмотра рекламы: восстановление жизни и разблокировка варианта
    public void ContinueFromAdReward()
    {
        lives = Mathf.Min(1, lives + 1);
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
        if (SliderFillImage != null) SliderFillImage.fillAmount = 1f;
        if (StopwatchText != null) StopwatchText.text = timer.ToString("F2", CultureInfo.InvariantCulture);

        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        if (StopwatchText != null) StopwatchText.color = timeNormalColor;

        if (minusPulser != null)
        {
            minusPulser.StopAllCoroutines();
            minusPulser.ResetScale();
        }

        if (PhotoImage != null) PhotoImage.transform.localScale = Vector3.one;
        if (PhotoCanvasGroup != null) PhotoCanvasGroup.alpha = 1f;
        if (PhotoImage != null) { PhotoImage.color = Color.white; PhotoImage.canvasRenderer.SetAlpha(1f); }
        portraitAnimCoroutine = null;

        ResumeFromModal();
        lastWrongIndex = -1;
    }

    // Останавливает игру (Time.timeScale = 0) и показывает модалку
    void FreezeAndShowModal(GameObject modal)
    {
        questionActive = false;

        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        if (minusPulser != null)
        {
            minusPulser.StopAllCoroutines();
            minusPulser.ResetScale();
        }
        if (portraitAnimCoroutine != null) { StopCoroutine(portraitAnimCoroutine); portraitAnimCoroutine = null; }

        if (PhotoImage != null) { PhotoImage.transform.localScale = Vector3.one; PhotoImage.color = Color.white; PhotoImage.canvasRenderer.SetAlpha(1f); }
        if (PhotoCanvasGroup != null) PhotoCanvasGroup.alpha = 1f;

        for (int i = 0; i < 4; i++) if (answerButtons[i] != null) answerButtons[i].interactable = false;
        if (MinusOneButton != null) MinusOneButton.interactable = false;
        if (AdvertisingButton != null) AdvertisingButton.interactable = false;

        if (modal != null) modal.SetActive(true);

        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    // Возобновляет игру и скрывает модалки
    void ResumeFromModal()
    {
        Time.timeScale = savedTimeScale;
        questionActive = true;

        for (int i = 0; i < 4; i++)
            if (!disabledThisQuestion[i] && answerButtons[i] != null) answerButtons[i].interactable = true;

        if (MinusOneButton != null) MinusOneButton.interactable = true;
        if (AdvertisingButton != null) AdvertisingButton.interactable = true;

        if (ModalBlockerLossPanel != null) ModalBlockerLossPanel.SetActive(false);
        if (ModalBlockerWinPanel != null) ModalBlockerWinPanel.SetActive(false);

        if (timer <= pulseStartThreshold && pulseCoroutine == null)
            pulseCoroutine = StartCoroutine(PulseTimeText());
    }
}
