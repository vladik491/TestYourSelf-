using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [SerializeField] private float _amplitude = 0.5f;
    [SerializeField] private float _frequency = 1.0f;
    [SerializeField] private float _phase = 0.0f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float y = startPosition.y + _amplitude * Mathf.Sin(_frequency * Time.time + _phase);
        transform.position = new Vector3(startPosition.x, y, startPosition.z);
    }
}
