using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GoBackButtonScript : MonoBehaviour
{
    [SerializeField] private GameObject OuterPanel;    
    [SerializeField] private GameObject ScrollAndPage; 
    [SerializeField] private GameObject[] NewPanels;   
    [SerializeField] private GameObject GoBackButton;  
    [SerializeField] private SoundController soundController; 

    public void OnGoBackButtonDown()
    {
        GoBackButton.GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundAndActivate());
    }

    public void OnGoBackButtonUp()
    {
        GoBackButton.GetComponent<RectTransform>().localScale = Vector3.one;
    }

    private IEnumerator PlaySoundAndActivate()
    {
        soundController.PlayClickSound();
        yield return new WaitForSeconds(soundController.clickSound.length);

        foreach (GameObject panel in NewPanels)
        {
            if (panel.activeSelf)
            {
                panel.SetActive(false);
                break;
            }
        }

        GoBackButton.SetActive(false);
        OuterPanel.SetActive(true);
        ScrollAndPage.SetActive(true);
    }
}