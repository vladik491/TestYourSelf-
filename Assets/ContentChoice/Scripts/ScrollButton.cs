using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScrollButton : MonoBehaviour
{
    [SerializeField] private Button _scrollButtonUp;
    [SerializeField] private Button _scrollButtonDown;
    [SerializeField] private SoundController soundController;

    public void OnScrollButtonUpUp()
    {
        _scrollButtonUp.GetComponent<RectTransform>().localScale = Vector3.one;
    }

    public void OnScrollButtonUpDown()
    {
        _scrollButtonUp.GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySound());
    }

    public void OnScrollButtonDownUp()
    {
        _scrollButtonDown.GetComponent<RectTransform>().localScale = Vector3.one;
    }

    public void OnScrollButtonDownDown()
    {
        _scrollButtonDown.GetComponent<RectTransform>().localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySound());
    }

    private IEnumerator PlaySound()
    {
        soundController.PlayClickSound();
        yield return new WaitForSeconds(soundController.clickSound.length);
    }

}
