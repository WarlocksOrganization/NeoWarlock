using UnityEngine;
using System.Collections.Generic;
using DataSystem;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Data")]
    public List<Constants.SoundData> soundList;
    private Dictionary<Constants.SoundType, AudioClip> soundDict = new();

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxPrefab;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSounds();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSounds()
    {
        foreach (var sound in soundList)
        {
            if (!soundDict.ContainsKey(sound.type))
            {
                soundDict.Add(sound.type, sound.clip);
            }
        }

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
    }

    // 🎵 BGM 재생
    public void PlayBGM(Constants.SoundType type)
    {
        if (soundDict.TryGetValue(type, out AudioClip clip))
        {
            if (bgmSource.clip == clip && bgmSource.isPlaying)
                return;

            bgmSource.clip = clip;   
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"[AudioManager] BGM SoundType {type} not found.");
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // 🔊 SFX 재생 (3D)
    public void PlaySFX(Constants.SoundType type, GameObject parent = null)
    {
        if (soundDict.TryGetValue(type, out AudioClip clip))
        {
            AudioSource sfx = Instantiate(sfxPrefab, parent != null ? parent.transform : null);
            sfx.clip = clip;
            sfx.volume = sfxVolume;
            sfx.spatialBlend = 1f;
            sfx.rolloffMode = AudioRolloffMode.Linear;
            sfx.minDistance = 1f;
            sfx.maxDistance = 20f;

            sfx.Play();
            Destroy(sfx.gameObject, clip.length + 0.5f);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX SoundType {type} not found.");
        }
    }

    // 🎚 볼륨 설정
    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
    }
}
