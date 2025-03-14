using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource bgmSource; // 배경음 오디오 소스
    public AudioSource sfxSource; // 효과음 오디오 소스

    private float bgmVolume = 1.0f; // 배경음 볼륨
    private float sfxVolume = 1.0f; // 효과음 볼륨

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 변경 시 유지

            // 저장된 볼륨 값 불러오기
            bgmVolume = PlayerPrefs.GetFloat("BgmVolume", 1.0f);
            sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1.0f);

            //ApplyVolume();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public float GetBgmVolume() { return bgmVolume; }
    public float GetSfxVolume() { return sfxVolume; }

    public void SetBgmVolume(float volume)
    {
        bgmVolume = volume;
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = volume;
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    // 변경된 볼륨 저장
    //public void SaveVolumeSettings()
    //{
    //    PlayerPrefs.SetFloat("BgmVolume", bgmVolume);
    //    PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
    //    PlayerPrefs.Save();
    //}
}
