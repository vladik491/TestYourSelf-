using TMPro;
using UnityEngine;

public class WinTextTMP : MonoBehaviour
{
    [SerializeField] private TMP_Text winText;
    [SerializeField] private string categoryName;
    [SerializeField] private Gender categoryGender = Gender.Neuter;

    public enum Gender { Masculine, Neuter }

    void Start()
    {
        string demonstrative = GetThisForm(categoryGender);  // "этот/это"
        string otherAdj = GetOtherForm(categoryGender);      // "другой/другое"

        winText.text = $"<b>¬ы выиграли!</b>\n\n" +
                       $"ѕоздравл€ем Ч вы только что доказали, что отлично знаете {demonstrative} {categoryName}! " +
                       $"“еперь вы можете вернутьс€ и выбрать {otherAdj} {categoryName} дл€ проверки ваших знаний (кнопка по центру).";
    }

    private string GetThisForm(Gender g)
    {
        return g switch
        {
            Gender.Masculine => "этот",
            Gender.Neuter => "это",
            _ => "это"
        };
    }

    private string GetOtherForm(Gender g)
    {
        return g switch
        {
            Gender.Masculine => "другой",
            Gender.Neuter => "другое",
            _ => "другое"
        };
    }
}
