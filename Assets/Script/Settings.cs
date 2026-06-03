using UnityEngine;
using UnityEngine.Audio;

public class Settings : MonoBehaviour
{
    public static Settings Instance { private set; get; }
    [SerializeField] private AudioMixer mainMixer;
    private const string VolumeKey = "MasterVolume";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        SetMasterVolume(savedVolume);
    }

    public void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat(VolumeKey, volume);
        mainMixer.SetFloat(VolumeKey, Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f);
        PlayerPrefs.Save();
    }
}