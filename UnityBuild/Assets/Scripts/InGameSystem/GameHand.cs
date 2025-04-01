using System.Collections;
using System.Collections.Generic;
using Mirror;
using Player;
using System.Linq;
using Player.Combat;
using UnityEngine;
using Cinemachine;
using DataSystem;

public class GameHand : NetworkBehaviour
{
    public static GameHand Instance;
    
    
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject explosionPrefab;
    
    List<Animator> Screenanimators = new List<Animator>();


    private Transform target;
    private bool isAttacking = false;
    
    [SerializeField] private float attackRadius = 15f;
    [SerializeField] private float attackKnockback = 3f;
    [SerializeField] private float Speed = 0.25f;
    
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeIntensity = 2f;
    [SerializeField] private float frequencyIntensity = 10f;
    
    [SerializeField] private GameObject skillItemPickupPrefab;
    
    [SerializeField] private GameObject GhostEnemyPrefab;
    
    private CinemachineVirtualCamera virtualCamera;
    private Coroutine shakeCoroutine;
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject); // ✅ 중복된 Instance 제거
    }

    [ServerCallback]
    private void Start()
    {
        isAttacking = false;
        animator.SetBool("isReady", true);
        SwitchTarget();
    }

    [ServerCallback]
    private void Update()
    {
        if (target == null || isAttacking) return;

        Vector3 targetPos = target.position;
        targetPos.y = 0;

        Vector3 newPos = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * Speed);
        newPos.y = 0;
        transform.position = newPos;
    }

    [Server]
    public void SwitchTarget()
    {
        isAttacking = false;
        var allPlayers = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
            .Where(p => !p.isDead)
            .ToList();

        if (allPlayers.Count == 0) return;

        var newTarget = allPlayers[Random.Range(0, allPlayers.Count)];
        target = newTarget.transform;
    }

    /// <summary>
    /// 외부에서 호출 시 공격 시작
    /// </summary>
    [Server]
    public void Initialize()
    {
        if (isAttacking) return;
        
        animator.SetBool("isReady", false);
        isAttacking = true;

        RpcTriggerAttackAnim();

        Invoke(nameof(Attack1), 1f);
        Invoke(nameof(Attack2), 2f);
        Invoke(nameof(Attack3), 4f);

        Invoke(nameof(SwitchTarget), 5f);
    }

    [Server] private void Attack1() => PerformAttack();
    [Server] private void Attack2() => PerformAttack();
    [Server] private void Attack3() => PerformAttack(true); // 마지막 공격

    [ClientRpc]
    private void RpcTriggerAttackAnim()
    {
        if (animator != null)
        {
            animator.SetTrigger("isAttack");
        }
        
        if (Screenanimators.Count == 0)
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(sortMode: FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "ComputerScreen")
                {
                    Animator animator = obj.GetComponent<Animator>();
                    if (animator != null)
                    {
                        Screenanimators.Add(animator);
                    }
                }
            }
        }

        foreach (var animator in Screenanimators)
        {
            animator.SetTrigger("isError");
        }
    }
    
    [Server]
    private void PerformAttack(bool isFinal = false)
    {
        float radius = attackRadius;
        float knockback = attackKnockback;
        float shake = shakeIntensity;
        float freq = frequencyIntensity;

        if (isFinal)
        {
            radius *= 2f;
            knockback *= 2f;
            shake *= 2f;
            freq *= 2f;
        }

        Vector3 position = transform.position;
        GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
    
        Explosion explosionComp = explosion.GetComponent<Explosion>();
        if (explosionComp != null)
        {
            explosionComp.Initialize(0, radius, knockback, null, gameObject, -1, -1);
        }

        NetworkServer.Spawn(explosion);
        RpcShakeCamera(shake, freq, isFinal);
        
        if (isFinal && Random.value <= 0.5f)
        {
            int itemCount = Random.Range(1, 4); // 🔹 1~3 사이의 랜덤한 개수

            for (int i = 0; i < itemCount; i++)
            {
                Vector3 spawnPosition = new Vector3(
                    Random.Range(-15f, 15f),
                    Random.Range(30f, 40f),
                    Random.Range(-15f, 15f)
                );

                GameObject pickup = Instantiate(skillItemPickupPrefab, transform.position + spawnPosition, Quaternion.identity);

                NetworkServer.Spawn(pickup);
            }
        }
        // 정전 확률 10%
        else if (isFinal && Random.value <= 0.2f)
        {
            RpcTriggerBlackout();
            
            // 🎯 정전 시 GameHand 스폰
            var players = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None)
                .Where(p => !p.isDead)
                .ToList();

            foreach (var player in players)
            {
                Vector3 randomOffset = Random.onUnitSphere; // 반경 5미터 내 랜덤 위치
                randomOffset.y = 0f; // 평면상
                randomOffset = randomOffset.normalized;
                randomOffset *= 10f;
                Vector3 spawnPos = player.transform.position + randomOffset;

                GameObject newHand = Instantiate(GhostEnemyPrefab, spawnPos, Quaternion.identity);
                NetworkServer.Spawn(newHand);
            }
        }
    }
    
    [ClientRpc]
    private void RpcShakeCamera(float amplitude, float frequency, bool isFinal)
    {
        if (virtualCamera == null)
            virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
        if (virtualCamera == null) return;

        var noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise == null) return;

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCameraCoroutine(noise, amplitude, frequency));
        
        if (!isFinal)
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_HandAttack, gameObject);
        }
        else
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_HandEndAttack, gameObject);
        }
    }

    private IEnumerator ShakeCameraCoroutine(CinemachineBasicMultiChannelPerlin noise, float amp, float freq)
    {
        float elapsed = 0f;
        float startAmplitude = amp;
        float startFrequency = freq;

        noise.m_AmplitudeGain = startAmplitude;
        noise.m_FrequencyGain = startFrequency;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shakeDuration;

            noise.m_AmplitudeGain = Mathf.Lerp(startAmplitude, 0f, t);
            noise.m_FrequencyGain = Mathf.Lerp(startFrequency, 0f, t);

            yield return null;
        }

        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;
    }
    
    [ClientRpc]
    private void RpcTriggerBlackout()
    {
        var dirLight = FindObjectsByType<Light>(FindObjectsSortMode.None)
            .FirstOrDefault(l => l.type == LightType.Directional);

        if (dirLight != null)
        {
            StartCoroutine(BlackoutCoroutine(dirLight));
        }
    }

    private IEnumerator BlackoutCoroutine(Light dirLight)
    {
        float duration = 0.3f;

        // 빠르게 Intensity 올렸다가 깎기
        dirLight.intensity = 5f;
        yield return new WaitForSeconds(0.1f);
        
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.reflectionIntensity = 0f;

        float t = 0f;
        float start = dirLight.intensity;
        float end = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            dirLight.intensity = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }

        dirLight.intensity = 0f;
        
        foreach (var anim in Screenanimators)
        {
            anim.SetBool("isBlackout", true);
        }
        
        AudioManager.Instance.SetBGMPitch(0.5f);

        // 10초 후 복구
        yield return new WaitForSeconds(10f);
        
        // 🌅 천천히 빛 복구
        float recoveryDuration = 3f;
        float recoveryTime = 0f;

        while (recoveryTime < recoveryDuration)
        {
            recoveryTime += Time.deltaTime;
            float recoveryT = recoveryTime / recoveryDuration;

            dirLight.intensity = Mathf.Lerp(0f, 1f, recoveryT);
            RenderSettings.ambientIntensity = Mathf.Lerp(0f, 1f, recoveryT);
            RenderSettings.reflectionIntensity = Mathf.Lerp(0f, 1f, recoveryT);
            RenderSettings.ambientLight = Color.Lerp(Color.black, Color.white, recoveryT);

            yield return null;
        }
        
        AudioManager.Instance.ApplyBGMVolumeToMixer(1);

        // 보정
        dirLight.intensity = 1f;
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.reflectionIntensity = 1f;
        RenderSettings.ambientLight = Color.white;
        
        foreach (var anim in Screenanimators)
        {
            anim.SetBool("isBlackout", true);
        }
        
        AudioManager.Instance.SetBGMPitch(1f);
    }

}