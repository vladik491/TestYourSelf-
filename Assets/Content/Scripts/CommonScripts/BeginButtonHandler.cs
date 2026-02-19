using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BeginButtonHandler : MonoBehaviour
{
    public enum DifficultyType { Easy, Medium, Hard }

    [SerializeField] private SoundController soundController;
    [Header("UI")]
    public GameObject readinessPanel;

    [Header("Panels (assign the panel for each difficulty)")]
    public GameObject easyPanel;
    public GameObject mediumPanel;
    public GameObject hardPanel;

    [Header("Behaviour")]
    public DifficultyType difficulty = DifficultyType.Easy;
    public Button beginButton;
    public float fadeDuration = 0.18f;

    RectTransform _buttonRect;

    void Awake()
    {
        _buttonRect = beginButton.GetComponent<RectTransform>();
    }

    public void OnBeginPointerClick()
    {
        beginButton.interactable = false;
        EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(BeginSequence());
    }

    public void OnBeginButtonDown()
    {
        _buttonRect.localScale = new Vector3(0.95f, 0.95f, 1f);
    }

    public void OnBeginButtonUp()
    {
        _buttonRect.localScale = Vector3.one;
    }

    IEnumerator BeginSequence()
    {
        if (soundController != null)
            soundController.SelectionOfCategories();

        float wait = 0.05f;
        if (soundController != null && soundController.clickSound != null)
            wait = soundController.clickSound.length;

        yield return new WaitForSecondsRealtime(wait);

        CanvasGroup cg = readinessPanel.GetComponent<CanvasGroup>();
        yield return StartCoroutine(FadeOutAndActivate(cg));
    }

    IEnumerator FadeOutAndActivate(CanvasGroup cg)
    {
        float t = 0f;
        float start = cg.alpha;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, 0f, t / Mathf.Max(0.0001f, fadeDuration));
            yield return null;
        }

        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        cg.gameObject.SetActive(false);
        cg.alpha = start;

        // активируем нужную панель и деактивируем прочие
        GameObject target = GetPanelByDifficulty(difficulty);
        if (easyPanel != null && easyPanel != target) easyPanel.SetActive(false);
        if (mediumPanel != null && mediumPanel != target) mediumPanel.SetActive(false);
        if (hardPanel != null && hardPanel != target) hardPanel.SetActive(false);

        if (target != null)
        {
            target.SetActive(true);

            // пытаемс€ проставить difficulty в LevelDifficultyController (если он есть)
            if (LevelDifficultyController.Instance != null)
            {
                switch (difficulty)
                {
                    case DifficultyType.Easy:
                        LevelDifficultyController.Instance.CurrentDifficulty = LevelDifficultyController.Difficulty.Easy;
                        break;
                    case DifficultyType.Medium:
                        LevelDifficultyController.Instance.CurrentDifficulty = LevelDifficultyController.Difficulty.Medium;
                        break;
                    case DifficultyType.Hard:
                        LevelDifficultyController.Instance.CurrentDifficulty = LevelDifficultyController.Difficulty.Hard;
                        break;
                }
            }

            // »щем в дочерних компонентах любой MonoBehaviour, у которого есть метод StartLevel() и вызываем его
            var behaviours = target.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var b in behaviours)
            {
                var m = b.GetType().GetMethod("StartLevel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (m != null)
                {
                    m.Invoke(b, null);
                    break;
                }
            }
        }

        yield break;
    }

    private GameObject GetPanelByDifficulty(DifficultyType d)
    {
        switch (d)
        {
            case DifficultyType.Medium: return mediumPanel;
            case DifficultyType.Hard: return hardPanel;
            case DifficultyType.Easy:
            default: return easyPanel;
        }
    }
}
