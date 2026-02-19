using UnityEngine;
using TMPro;

public class PointsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text pointsText;

    void OnEnable()
    {
        PointsManager.OnPointsChanged += UpdateText;
        UpdateText(PointsManager.GetTotalPoints());
    }

    void OnDisable()
    {
        PointsManager.OnPointsChanged -= UpdateText;
    }

    void UpdateText(int total)
    {
        pointsText.text = total.ToString();
    }
}
