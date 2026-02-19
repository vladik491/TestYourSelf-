using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ArrowButton : MonoBehaviour
{
    public Button arrowButton;
    public Dropdown dropdown;

    [SerializeField] private SoundController soundController;

    void Start()
    {
        foreach (var image in dropdown.GetComponentsInChildren<Image>())
        {
            if (image.transform != arrowButton.transform)
            {
                image.raycastTarget = false;
            }
        }
        foreach (var text in dropdown.GetComponentsInChildren<Text>())
        {
            text.raycastTarget = false;
        }

        arrowButton.onClick.AddListener(() => dropdown.Show());
    }

    public void OnArrowButtonUp()
    {
        arrowButton.GetComponent<RectTransform>().localScale = Vector3.one;
    }

    public void OnArrowButtonDown()
    {
        arrowButton.GetComponent<RectTransform>().localScale = new Vector3(0.97f, 0.97f, 1f);
        StartCoroutine(PlaySound());
    }
    private IEnumerator PlaySound()
    {
        soundController.PlayClickSound();
        yield return new WaitForSeconds(soundController.clickSound.length);   
    }
}