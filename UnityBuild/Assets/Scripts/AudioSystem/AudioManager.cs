using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using DataSystem;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Data")]
    public List<Constants.SoundData> bgmList;
    public List<Constants.SoundData> uiSFXList;
    public List<Constants.SoundData> gameSFXList;
    
    public List<Constants.SkillSoundData> skillSFXList;
    
    private Dictionary<Constants.SoundType, AudioClip> soundDict = new();
    private Dictionary<Constants.SkillType, Constants.SkillSoundData> skillSoundDict = new();

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxPrefab;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    
    [SerializeField] private AudioMixer mixer;

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
        SetBGMVolume(bgmVolume);
        SetSFXVolume(sfxVolume);

        AddSoundListToDict(bgmList);
        AddSoundListToDict(uiSFXList);
        AddSoundListToDict(gameSFXList);
        AddSkillSoundListToDict(skillSFXList);

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
    }
    
    private void AddSoundListToDict(List<Constants.SoundData> list)
    {
        foreach (var sound in list)
        {
            if (!soundDict.ContainsKey(sound.type))
            {
                soundDict.Add(sound.type, sound.clip);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Duplicate SoundType: {sound.type}");
            }
        }
    }
    
    private void AddSkillSoundListToDict(List<Constants.SkillSoundData> list)
    {
        foreach (var sound in list)
        {
            if (!skillSoundDict.ContainsKey(sound.skillType))
                skillSoundDict.Add(sound.skillType, sound);
            else
                Debug.LogWarning($"[AudioManager] Duplicate SkillSoundType: {sound.skillType}");
        }
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
            bgmSource.pitch = 1;
            bgmSource.Play();

            ApplyBGMVolumeToMixer(1);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] BGM SoundType {type} not found.");
        }
    }

    public void SetBGMPitch(float pitch)
    {
        bgmSource.pitch = pitch;
    }
    
    public void PlaySFX(Constants.SoundType type, GameObject parent = null)
    {
        if (soundDict.TryGetValue(type, out AudioClip clip))
        {
            PlayClip(clip, parent);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX SoundType {type} not found.");
        }
    }

    // 일반 스킬 사운드 재생
    public void PlaySFX(Constants.SkillType type, GameObject parent = null)
    {
        if (skillSoundDict.TryGetValue(type, out var soundData) && soundData.clip != null)
            PlayClip(soundData.clip, parent);
        else
            Debug.LogWarning($"[AudioManager] SkillType {type} clip not found.");
    }
    
    public void PlaySFX(Constants.SoundType type, Vector3 position)
    {
        if (soundDict.TryGetValue(type, out AudioClip clip))
        {
            PlayClipAtPosition(clip, position);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX SoundType {type} not found.");
        }
    }
    
    private void PlayClipAtPosition(AudioClip clip, Vector3 position)
    {
        AudioSource sfx = Instantiate(sfxPrefab);
        sfx.transform.position = position;

        sfx.clip = clip;
        sfx.volume = sfxVolume;
        sfx.spatialBlend = 1f; // 3D 사운드로 처리

        sfx.Play();
        Destroy(sfx.gameObject, clip.length + 0.5f);
    }


// 히트 시 사운드 재생
    public void PlayHitSFX(Constants.SkillType type, GameObject parent = null)
    {
        if (skillSoundDict.TryGetValue(type, out var soundData) && soundData.hitClip != null)
            PlayClip(soundData.hitClip, parent);
        else
            Debug.LogWarning($"[AudioManager] SkillType {type} hitClip not found.");
    }

    // 공통 클립 재생 함수
    private void PlayClip(AudioClip clip, GameObject parent)
    {
        AudioSource sfx = Instantiate(sfxPrefab, parent != null ? parent.transform : null);
        sfx.clip = clip;
        sfx.volume = sfxVolume;

        if (parent)
        {
            sfx.spatialBlend = 1f;
        }
        else
        {
            sfx.spatialBlend = 0f;
        }

        sfx.Play();
        Destroy(sfx.gameObject, clip.length + 0.5f);
    }

    // 🎚 볼륨 설정
    public float GetBGMVolume()
    {
        return bgmVolume;
    }
    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        float vol = volume * 40 - 20f;
        mixer.SetFloat("BGMVolume", vol <= -20f ? -80f : vol);
    }
    
    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        
        float vol = sfxVolume * 40 - 20f;
        if (vol <= -20)
        {
            mixer.SetFloat("SFXVolume", -80);
        }
        else
        {
            mixer.SetFloat("SFXVolume", vol);
        }
    }
    
    public void ApplyBGMVolumeToMixer(float volume)
    {
        StopAllCoroutines();
        StartCoroutine(FadeBGMVolumeCoroutine(volume, 0.5f));
    }

    private IEnumerator FadeBGMVolumeCoroutine(float targetMultiplier, float duration)
    {
        float startVol = bgmVolume * 40 - 20f;
        mixer.GetFloat("BGMVolume", out float currentVol);

        float targetVol = bgmVolume * targetMultiplier * 40 - 20f;
        if (targetVol <= -20f) targetVol = -80f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float interpolated = Mathf.Lerp(currentVol, targetVol, t);
            mixer.SetFloat("BGMVolume", interpolated);
            yield return null;
        }

        mixer.SetFloat("BGMVolume", targetVol); // 최종 값 보정
    }

}
