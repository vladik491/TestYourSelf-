using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClueButton : MonoBehaviour
{
    [SerializeField] private SoundController soundController;

    public void OnButtonDown()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) rt.localScale = new Vector3(0.95f, 0.95f, 1f);
        StartCoroutine(PlaySoundThenLoadScene());
    }

    public void OnButtonUp()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) rt.localScale = Vector3.one;
    }

    private IEnumerator PlaySoundThenLoadScene()
    {
        // проигрываем звук и ждём его окончания
        soundController.SelectionOfCategories();
        yield return new WaitForSeconds(soundController.clickSound.length);
    }
}
