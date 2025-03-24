using System.Collections;
using Mirror;
using Player;
using System.Linq;
using Player.Combat;
using UnityEngine;
using Cinemachine;

public class GameHand : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject explosionPrefab;


    private Transform target;
    private bool isAttacking = false;
    
    [SerializeField] private float attackRadius = 15f;
    [SerializeField] private float attackKnockback = 3f;
    [SerializeField] private float Speed = 0.25f;
    
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeIntensity = 2f;
    [SerializeField] private float frequencyIntensity = 10f;
    
    private CinemachineVirtualCamera virtualCamera;
    private Coroutine shakeCoroutine;

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
    private void SwitchTarget()
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
        animator.SetBool("isReady", false);
        isAttacking = true;

        RpcTriggerAttackAnim();
        Invoke(nameof(PerformAttack), 1f);
        Invoke(nameof(PerformAttack), 2f);
        Invoke(nameof(PerformAttack), 4f);

        // 이후 다시 타겟 지정 (선택)
        Invoke("SwitchTarget", 5f);
    }

    [ClientRpc]
    private void RpcTriggerAttackAnim()
    {
        if (animator != null)
        {
            animator.SetTrigger("isAttack");
        }
    }
    
    [Server]
    private void PerformAttack()
    {
        if (explosionPrefab == null) return;

        Vector3 position = transform.position;
        GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        
        Explosion explosionComp = explosion.GetComponent<Explosion>();
        if (explosionComp != null)
        {
            explosionComp.Initialize(0, attackRadius, attackKnockback, null, gameObject, -1, -1); // config 등은 필요 시 전달
        }
        
        NetworkServer.Spawn(explosion);

        RpcShakeCamera(); // 👈 클라이언트에게 카메라 흔들기
    }
    
    [ClientRpc]
    private void RpcShakeCamera()
    {
        if (virtualCamera == null)
        {
            virtualCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
        }

        if (virtualCamera == null) return;

        var noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise == null) return;

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeCameraCoroutine(noise));
    }

    private IEnumerator ShakeCameraCoroutine(CinemachineBasicMultiChannelPerlin noise)
    {
        float elapsed = 0f;
        float startAmplitude = shakeIntensity;
        float startFrequency = frequencyIntensity;

        noise.m_AmplitudeGain = startAmplitude;
        noise.m_FrequencyGain = startFrequency;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shakeDuration;

            // 자연스럽게 0으로 감소 (0 ~ 1 → 1 ~ 0)
            noise.m_AmplitudeGain = Mathf.Lerp(startAmplitude, 0f, t);
            noise.m_FrequencyGain = Mathf.Lerp(startFrequency, 0f, t);

            yield return null;
        }

        // 마지막 보정
        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;
    }
}