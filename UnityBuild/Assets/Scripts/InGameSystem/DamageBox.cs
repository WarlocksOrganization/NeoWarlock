using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;

public class DamageBox : MonoBehaviour
{
    [SerializeField] private int damagePerTick = 10; // ✅ 0.5초마다 줄 데미지
    [SerializeField] private float damageInterval = 0.5f; // ✅ 데미지 간격 (0.5초)
    private HashSet<PlayerCharacter> playersInRange = new HashSet<PlayerCharacter>(); // ✅ 감지된 플레이어 저장

    private void OnTriggerEnter(Collider other)
    {
        PlayerCharacter player = other.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            playersInRange.Add(player);
            if (playersInRange.Count == 1) // ✅ 첫 번째 플레이어가 들어오면 코루틴 시작
                StartCoroutine(DamageOverTime());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCharacter player = other.GetComponent<PlayerCharacter>();
        if (player != null)
        {
            playersInRange.Remove(player);
            if (playersInRange.Count == 0) // ✅ 플레이어가 없으면 코루틴 중지
                StopCoroutine(DamageOverTime());
        }
    }

    private IEnumerator DamageOverTime()
    {
        while (playersInRange.Count > 0)
        {
            foreach (var player in playersInRange)
            {
                player.takeDamage(damagePerTick, transform.position, 0, null, -1, -1); // ✅ 넉백 X, 버프 X
            }
            yield return new WaitForSeconds(damageInterval);
        }
    }
}