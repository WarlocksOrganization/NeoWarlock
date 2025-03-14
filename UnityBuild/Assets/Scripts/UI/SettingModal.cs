using UnityEngine;
using UnityEngine.UI;

public class SettingModal : MonoBehaviour
{
    public Slider bgmSlider; // ����� �����̴�
    public Slider sfxSlider; // ȿ���� �����̴�
    public Button cancelButton; // ��� ��ư
    public Button confirmButton; // Ȯ�� ��ư

    private float initialBgmVolume; // �ʱ� ����� ũ��
    private float initialSfxVolume; // �ʱ� ȿ���� ũ��

    void Start()
    {
        // AudioManager���� ���� ����� ���� ���� �ҷ��� �ʱ�ȭ
        //initialBgmVolume = AudioManager.Instance.GetBgmVolume();
        //initialSfxVolume = AudioManager.Instance.GetSfxVolume();

        // �����̴� �ʱⰪ ����
        bgmSlider.value = initialBgmVolume;
        sfxSlider.value = initialSfxVolume;

        // ��ư �̺�Ʈ ���
        cancelButton.onClick.AddListener(CancelSettings);
        confirmButton.onClick.AddListener(ConfirmSettings);
    }

    // ����� ������ �����ϰ�, �ʱⰪ�� ������Ʈ
    public void ConfirmSettings()
    {
        // ���� �����̴� �� ����
        initialBgmVolume = bgmSlider.value;
        initialSfxVolume = sfxSlider.value;

        // AudioManager�� ���� �� ����
        //AudioManager.Instance.SetBgmVolume(initialBgmVolume);
        //AudioManager.Instance.SetSfxVolume(initialSfxVolume);
        //AudioManager.Instance.SaveVolumeSettings();

        // ��� â �ݱ�
        gameObject.SetActive(false);
    }

    // ���������� ����� ������ ���� (�ʱⰪ ���)
    public void CancelSettings()
    {
        // ����� ���� ������ �����̴� ����
        bgmSlider.value = initialBgmVolume;
        sfxSlider.value = initialSfxVolume;

        // AudioManager ������ �ʱⰪ���� ����
        //AudioManager.Instance.SetBgmVolume(initialBgmVolume);
        //AudioManager.Instance.SetSfxVolume(initialSfxVolume);

        // ��� â �ݱ�
        gameObject.SetActive(false);
    }
}
