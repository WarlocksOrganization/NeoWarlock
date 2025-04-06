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

    protected static bool isStarted = false;

    protected void Awake()
    {
        Instance = this;
    }

    protected virtual void Start()
    {
        if (mapConfig != null && mapConfig.skyboxMaterial != null)
        {
            RenderSettings.skybox = mapConfig.skyboxMaterial;
        }
    }

    public virtual void StartEvent()
    {
       if(isStarted) return;
       isStarted = true;
       Invoke(nameof(StartTime), 1f);
    }

    protected void StartTime()
    {
        isStarted = false;
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

    private void OnDestroy()
    {
        var virtualCamera = FindFirstObjectByType<Cinemachine.CinemachineVirtualCamera>();
        if (virtualCamera == null) return;

        var noise = virtualCamera.GetCinemachineComponent<Cinemachine.CinemachineBasicMultiChannelPerlin>();
        if (noise == null) return;

        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;

        isStarted = false;
    }
}
