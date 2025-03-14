using UnityEngine;
using UnityEngine.UI;

public class SettingModal : MonoBehaviour
{
    public Slider bgmSlider; // 배경음 슬라이더
    public Slider sfxSlider; // 효과음 슬라이더
    public Button cancelButton; // 취소 버튼
    public Button confirmButton; // 확인 버튼

    private float initialBgmVolume; // 초기 배경음 크기
    private float initialSfxVolume; // 초기 효과음 크기

    void Start()
    {
        // AudioManager에서 현재 저장된 볼륨 값을 불러와 초기화
        //initialBgmVolume = AudioManager.Instance.GetBgmVolume();
        //initialSfxVolume = AudioManager.Instance.GetSfxVolume();

        // 슬라이더 초기값 설정
        bgmSlider.value = initialBgmVolume;
        sfxSlider.value = initialSfxVolume;

        // 버튼 이벤트 등록
        cancelButton.onClick.AddListener(CancelSettings);
        confirmButton.onClick.AddListener(ConfirmSettings);
    }

    // 변경된 설정을 저장하고, 초기값을 업데이트
    public void ConfirmSettings()
    {
        // 현재 슬라이더 값 저장
        initialBgmVolume = bgmSlider.value;
        initialSfxVolume = sfxSlider.value;

        // AudioManager에 볼륨 값 저장
        //AudioManager.Instance.SetBgmVolume(initialBgmVolume);
        //AudioManager.Instance.SetSfxVolume(initialSfxVolume);
        //AudioManager.Instance.SaveVolumeSettings();

        // 모달 창 닫기
        gameObject.SetActive(false);
    }

    // 마지막으로 저장된 값으로 복원 (초기값 대신)
    public void CancelSettings()
    {
        // 저장된 볼륨 값으로 슬라이더 복원
        bgmSlider.value = initialBgmVolume;
        sfxSlider.value = initialSfxVolume;

        // AudioManager 볼륨도 초기값으로 복원
        //AudioManager.Instance.SetBgmVolume(initialBgmVolume);
        //AudioManager.Instance.SetSfxVolume(initialSfxVolume);

        // 모달 창 닫기
        gameObject.SetActive(false);
    }
}
