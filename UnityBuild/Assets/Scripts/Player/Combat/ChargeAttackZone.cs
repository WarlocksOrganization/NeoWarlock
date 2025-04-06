using System.Collections;
using System.Collections.Generic;
using DataSystem.Database;
using Interfaces;
using Mirror;
using Player;
using UnityEngine;

public class ChargeAttackZone : NetworkBehaviour
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private float height = 2f;
    [SerializeField] private int damage = 5;
    [SerializeField] private float attackInterval = 0.1f;
    [SerializeField] private float duration = 1.5f;
    [SerializeField] private float knockbackForce = 3f;

    private int attackerId = -1;
    private int skillId = 0;
    private GameObject owner;

    // ✅ 이미 공격한 대상 저장
    private readonly Dictionary<GameObject, float> lastHitTime = new();
    private float rehitDelay = 0.5f; // 같은 대상에게 다시 때릴 수 있는 시간

    public void Initialize(GameObject owner, int attackerId, int skillId)
    {
        this.owner = owner;
        this.attackerId = attackerId;
        this.skillId = skillId;
    }


    public void StartAttack()
    {
        StartCoroutine(PerformRepeatedAttack());
    }

    private IEnumerator PerformRepeatedAttack()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector3 center = transform.position;
            Vector3 top = center + Vector3.up * (height / 2);
            Vector3 bottom = center + Vector3.down * (height / 2);

            Collider[] hits = Physics.OverlapCapsule(bottom, top, radius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == owner) continue;

                float lastTime;
                lastHitTime.TryGetValue(hit.gameObject, out lastTime);

                if (Time.time - lastTime < rehitDelay) continue; // 아직 쿨타임 안 지났으면 스킵

                var damagable = hit.GetComponent<IDamagable>();
                if (damagable != null)
                {
                    // ✅ 같은 팀이면 무시
                    var hitPlayer = hit.GetComponent<PlayerCharacter>();
                    var ownerPlayer = owner != null ? owner.GetComponent<PlayerCharacter>() : null;

                    if (hitPlayer != null && ownerPlayer != null &&
                        hitPlayer.team != DataSystem.Constants.TeamType.None &&
                        hitPlayer.team == ownerPlayer.team)
                    {
                        continue; // 같은 팀이면 패스
                    }
                    
                    damagable.takeDamage(damage, transform.position, knockbackForce, null, attackerId, skillId);
                    lastHitTime[hit.gameObject] = Time.time; // 마지막 공격 시간 갱신
                }
            }

            yield return new WaitForSeconds(attackInterval);
            elapsed += attackInterval;
        }

        Destroy(gameObject);
    }
}
