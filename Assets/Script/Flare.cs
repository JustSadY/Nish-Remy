using UnityEngine;

public class Flare : MonoBehaviour
{
    private Light _light;
    [SerializeField] private float decreaseSpeed = 0.1f;
    private readonly float _maxIntensity = 10f;

    private void Awake()
    {
        _light = GetComponentInChildren<Light>();
    }

    private void Update()
    {
        if (_light == null || !(_light.intensity > 0)) return;
        _light.intensity -= decreaseSpeed * Time.deltaTime;
        _light.intensity = Mathf.Clamp(_light.intensity, 0, _maxIntensity);
        if (_light.intensity <= 0) GameManager.Instance.EndGame();
    }

    public void ResetLight()
    {
        _light.intensity = _maxIntensity;
    }
}