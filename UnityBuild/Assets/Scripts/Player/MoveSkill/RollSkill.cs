/*
using System.Collections;
using Player;
using UnityEngine;

public class RollSkill : IMovementSkill
{
    public void UseSkill(PlayerCharacter player)
    {
        Vector3 rollDirection = player.transform.forward * 3f; // 3m 앞으로 구르기
        player.StartCoroutine(RollCoroutine(player, rollDirection));
    }

    private IEnumerator RollCoroutine(PlayerCharacter player, Vector3 rollDirection)
    {
        float duration = 0.5f; // 0.5초 동안 구르기
        float time = 0;
        while (time < duration)
        {
            //player._characterController.Move(rollDirection * (Time.deltaTime / duration)); // 부드러운 이동
            time += Time.deltaTime;
            yield return null;
        }
    }
}
*/
