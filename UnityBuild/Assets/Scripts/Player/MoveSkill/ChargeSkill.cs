/*
using System.Collections;
using Player;
using UnityEngine;

public class ChargeSkill : IMovementSkill
{
    public void UseSkill(PlayerCharacter player)
    {
        Vector3 chargeDirection = player.transform.forward * 7f; // 7m 돌진
        player.StartCoroutine(ChargeCoroutine(player, chargeDirection));
    }

    private IEnumerator ChargeCoroutine(PlayerCharacter player, Vector3 chargeDirection)
    {
        float duration = 0.4f; // 0.4초 동안 돌진
        float time = 0;
        while (time < duration)
        {
            //player._characterController.Move(chargeDirection * (Time.deltaTime / duration)); // 부드러운 이동
            time += Time.deltaTime;
            yield return null;
        }
    }
}
*/
