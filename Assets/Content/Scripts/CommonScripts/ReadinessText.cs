using TMPro;
using UnityEngine;

public class ReadinessText : MonoBehaviour
{
    [SerializeField] private TMP_Text readinessText;
    [Min(1)][SerializeField] private int timeInSeconds;
    [SerializeField] private HPOption hpOption = HPOption.Three;
    [SerializeField] private Difficulty difficulty = Difficulty.Easy;

    public enum HPOption { Three = 3, Two = 2, One = 1 }
    public enum Difficulty { Easy, Medium, Hard }

    private void Start() => UpdateText();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (timeInSeconds < 1) timeInSeconds = 1;

        if (!Application.isPlaying && readinessText != null)
            UpdateText();
    }
#endif

    private void UpdateText()
    {
        if (readinessText == null)
        {
            Debug.LogWarning("ReadinessText: поле readinessText не назначено в инспекторе.");
            return;
        }

        int hp = (int)hpOption;
        string difficultyWord = GetDifficultyPrepositional(difficulty);
        string lifeWord = GetLifeForm(hp);

        readinessText.text =
            $"Вы находитесь на\n<b>{difficultyWord}</b> уровне сложности.\n\n" +
            $"У вас <b>{hp} {lifeWord}</b> на всю викторину\nи <b>{timeInSeconds} секунд</b> на каждый вопрос.\n\n" +
            "Готовы проверить свои знания? \nЖмите <b>Начать!</b>";
    }

    private string GetDifficultyPrepositional(Difficulty d)
    {
        return d switch
        {
            Difficulty.Easy => "лёгком",
            Difficulty.Medium => "среднем",
            Difficulty.Hard => "сложном",
            _ => "лёгком"
        };
    }

    private string GetLifeForm(int hp)
    {
        return hp == 1 ? "жизнь" : "жизни";
    }
}
