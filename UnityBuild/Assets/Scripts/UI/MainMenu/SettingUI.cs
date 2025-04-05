using System;
using DataSystem;
using UnityEngine;
using UnityEngine.UI;
using GameManagement;
using UI; // PlayerSetting 접근을 위해 추가

public class SettingUI : MonoBehaviour
{
    [SerializeField] private Button classicKeyButton;
    [SerializeField] private Image classicKeyFrame;

    [SerializeField] private Button aosKeyButton;
    [SerializeField] private Image aosKeyFrame;

    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private Color activeColor = Color.gray;
    private Color inactiveColor = Color.white;

    private void OnEnable()
    {
        // 슬라이더 초기값 설정
        float bgmVolume = AudioManager.Instance.GetBGMVolume();
        bgmSlider.value = bgmVolume;

        float sfxVolume = AudioManager.Instance.GetSFXVolume();
        sfxSlider.value = sfxVolume;

        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // 버튼 클릭 이벤트 등록
        classicKeyButton.onClick.AddListener(OnClickClassicKey);
        aosKeyButton.onClick.AddListener(OnClickAOSKey);

        // 현재 선택된 키 타입 표시
        UpdateKeyFrameUI();
    }

    public void SetBGMVolume(float volume)
    {
        AudioManager.Instance.SetBGMVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        AudioManager.Instance.SetSFXVolume(volume);
    }

    private void OnClickClassicKey()
    {
        PlayerSetting.PlayerKeyType = Constants.KeyType.Classic;
        UpdateKeyFrameUI();
        FindFirstObjectByType<PlayerCharacterUI>()?.UpdateQuickSlotKeyLabels();
    }

    private void OnClickAOSKey()
    {
        PlayerSetting.PlayerKeyType = Constants.KeyType.AOS;
        UpdateKeyFrameUI();
        FindFirstObjectByType<PlayerCharacterUI>()?.UpdateQuickSlotKeyLabels();
    }

    private void UpdateKeyFrameUI()
    {
        if (PlayerSetting.PlayerKeyType == Constants.KeyType.Classic)
        {
            classicKeyFrame.color = activeColor;
            aosKeyFrame.color = inactiveColor;
        }
        else
        {
            classicKeyFrame.color = inactiveColor;
            aosKeyFrame.color = activeColor;
        }
    }
}
