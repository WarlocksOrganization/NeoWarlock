using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource bgmSource; // ����� ����� �ҽ�
    public AudioSource sfxSource; // ȿ���� ����� �ҽ�

    private float bgmVolume = 1.0f; // ����� ����
    private float sfxVolume = 1.0f; // ȿ���� ����

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �� ���� �� ����

            // ����� ���� �� �ҷ�����
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

    // ����� ���� ����
    //public void SaveVolumeSettings()
    //{
    //    PlayerPrefs.SetFloat("BgmVolume", bgmVolume);
    //    PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
    //    PlayerPrefs.Save();
    //}
}
