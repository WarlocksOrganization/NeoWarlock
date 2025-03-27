using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void OnEnable()
    {
       float bgmVolume = AudioManager.Instance.GetBGMVolume();
       bgmSlider.value = bgmVolume;
       
       float sfxVolume = AudioManager.Instance.GetSFXVolume();
       sfxSlider.value = sfxVolume;
    }

    public void SetBGMVolume(float volume)
    {
       AudioManager.Instance.SetBGMVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        AudioManager.Instance.SetSFXVolume(volume);
    }
}
