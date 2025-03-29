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

        StartCoroutine(LavaShakeCoroutine(noise, amplitude, frequency, duration));
    }

    protected IEnumerator LavaShakeCoroutine(Cinemachine.CinemachineBasicMultiChannelPerlin noise, float amp, float freq, float duration)
    {
        float elapsed = 0f;

        noise.m_AmplitudeGain = amp;
        noise.m_FrequencyGain = freq;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;
    }

    
    [ClientRpc]
    public void PlaySFX(Constants.SoundType soundType)
    {
        AudioManager.Instance.PlaySFX(soundType);
    }
}
