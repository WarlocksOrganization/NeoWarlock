using System;
using System.Collections;
using System.Linq;
using DataSystem;
using Mirror;
using Player;
using UnityEngine;

public class GameSystemManager : NetworkBehaviour
{
    public static GameSystemManager Instance;
    
    protected int eventnum = 0;

    public MapConfig mapConfig;

    protected void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // ✅ 중복된 Instance 제거
    }

    protected void Start()
    {
        RenderSettings.skybox = mapConfig.skyboxMaterial;
    }

    public virtual void StartEvent()
    {
       
    }
    
    public virtual void NetEvent()
    {
   
    }
    
    // GameSystemManager.cs

    public void EndEventAndStartNextTimer()
    {
        var timer = FindFirstObjectByType<NetworkTimer>();
        if (timer != null && NetworkServer.active)
        {
            timer.StartPhase2(Constants.MaxGameEventTime);
        }
    }
    
    [ClientRpc]
    protected void RpcShakeCameraWhileLavaRises(float amplitude, float frequency, float duration)
    {
        var virtualCamera = FindFirstObjectByType<Cinemachine.CinemachineVirtualCamera>();
        if (virtualCamera == null) return;

        var noise = virtualCamera.GetCinemachineComponent<Cinemachine.CinemachineBasicMultiChannelPerlin>();
        if (noise == null) return;

        StartCoroutine(CameraShakeCoroutine(noise, amplitude, frequency, duration));
    }

    protected IEnumerator CameraShakeCoroutine(Cinemachine.CinemachineBasicMultiChannelPerlin noise, float amp, float freq, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            noise.m_AmplitudeGain = Mathf.Lerp(amp, 0f, t);
            noise.m_FrequencyGain = Mathf.Lerp(freq, 0f, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 마지막 보정
        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;
    }


    
    [ClientRpc]
    public void PlaySFX(Constants.SoundType soundType)
    {
        AudioManager.Instance.PlaySFX(soundType);
    }
}
