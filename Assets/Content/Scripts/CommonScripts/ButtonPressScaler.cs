using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ButtonPressScaler : MonoBehaviour
{
    RectTransform _rt;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    public void PressDown()
    {
        _rt.localScale = new Vector3(0.95f, 0.95f, 1f);
    }

    public void PressUp()
    {
        _rt.localScale = Vector3.one;
    }
}
