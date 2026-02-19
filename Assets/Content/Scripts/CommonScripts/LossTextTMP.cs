using UnityEngine;
using TMPro;

public class LossTextTMP : MonoBehaviour
{
    public TMP_Text lossText;

    void Start()
    {
        lossText.text = "<b> Вы проиграли.</b>\n\n" +
                        "Не переживайте — вы можете продолжить попытку, посмотрев рекламу (кнопка справа), " +
                        "или вернуться на экран выбора контента и начать тест заново (кнопка слева). " +
                        "Что выберете?";
    }
}
