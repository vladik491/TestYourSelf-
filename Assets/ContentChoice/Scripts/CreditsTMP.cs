using UnityEngine;
using TMPro;

public class CreditsTMP : MonoBehaviour
{
    public TMP_Text infoText;

    void Start()
    {
        infoText.text =
            "<b>Разработчик:</b>\nКалинин Владислав\n\n" +

            "<b>Связь с Разработчиком:</b><i>\nTwoRings491@yandex.ru</i></size>\n\n" +

            "<b>Благодарность:</b>\n" +
            "Спасибо всем, кто был причастен к разработке!\n\n" +
            "Игра создана с любовью.";
    }
}