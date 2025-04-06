using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using Player;
using UnityEngine;

public class DamageBox : MonoBehaviour
{
    [SerializeField] private int damagePerTick = 10; // ✅ 0.5초마다 줄 데미지
    [SerializeField] private float damageInterval = 0.5f; // ✅ 데미지 간격 (0.5초)
    [SerializeField] private AttackConfig attackConfig;
    private HashSet<PlayerCharacter> playersInRange = new HashSet<PlayerCharacter>(); // ✅ 감지된 플레이어 저장
    private Coroutine damageCoroutine; // ✅ 개별 데미지 코루틴 추적

    private void OnTriggerEnter(Collider other)
    {
        PlayerCharacter player = other.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            playersInRange.Add(player);

            // ✅ 새로운 플레이어가 감지되면 즉시 데미지 적용
            player.takeDamage(damagePerTick, transform.position, 0, null, -1, 0);
           // Debug.Log($"[DamageBox] 플레이어 {player.playerId} 감지됨. 현재 감지된 플레이어 수: {playersInRange.Count}");

            // ✅ 기존 코루틴이 실행 중이 아니면 실행
            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(DamageOverTime());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCharacter player = other.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            playersInRange.Remove(player);

            // ✅ 모든 플레이어가 나가면 코루틴 종료
            if (playersInRange.Count == 0 && damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator DamageOverTime()
    {
        while (playersInRange.Count > 0) // ✅ 실시간 업데이트 반영
        {
            foreach (var player in playersInRange.ToList()) // 안전하게 복사
            {
                player.takeDamage(damagePerTick, transform.position, 0, attackConfig, -1, 0);

                if (player.curHp <= 0)
                {
                    playersInRange.Remove(player); // 원본에서 제거
                }
            }

            yield return new WaitForSeconds(damageInterval);
        }

        // ✅ 모든 플레이어가 나가면 코루틴 종료
        damageCoroutine = null;
    }
}
