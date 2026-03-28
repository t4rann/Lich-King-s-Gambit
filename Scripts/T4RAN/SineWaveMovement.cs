using UnityEngine;

public class SineWaveMovement : MonoBehaviour
{
    [Header("Параметры волны")]
    public float amplitude = 0.5f; 
    public float frequency = 2f;   
    public Vector3 direction = Vector3.up; 

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.localPosition;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = startPos + direction * offset;
    }
}
