using System.Collections;
using System.Collections.Generic;
using DataSystem;
using Mirror;
//using Mono.Cecil;
using Player;
using Player.Combat;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class BuffSystem : NetworkBehaviour
{
    private Dictionary<Constants.BuffType, Coroutine> activeBuffs = new Dictionary<Constants.BuffType, Coroutine>();
    private Dictionary<Constants.BuffType, float> activeBuffValues = new Dictionary<Constants.BuffType, float>();

    private PlayerCharacter playerCharacter;
    private EffectSystem effectSystem;

    private void Start()
    {
        playerCharacter = GetComponent<PlayerCharacter>();
        effectSystem = GetComponent<EffectSystem>();
    }

    [Command(requiresAuthority = false)]
    public void CmdApplyBuff(BuffData buffData)
    {
        if (activeBuffs.ContainsKey(buffData.BuffType))
        {
            StopCoroutine(activeBuffs[buffData.BuffType]);
            activeBuffs.Remove(buffData.BuffType);
        }

        // ✅ 클라이언트에서 버프 이펙트 실행
        RpcPlayBuffEffect(buffData.BuffType, true);

        Coroutine buffCoroutine = StartCoroutine(ApplyBuff(buffData));
        activeBuffs[buffData.BuffType] = buffCoroutine;
    }

    public void ServerApplyBuff(BuffData buffData)
    {
        if (activeBuffs.ContainsKey(buffData.BuffType))
        {
            StopCoroutine(activeBuffs[buffData.BuffType]);
            activeBuffs.Remove(buffData.BuffType);
        }

        // ✅ 클라이언트에서 버프 이펙트 실행
        RpcPlayBuffEffect(buffData.BuffType, true);

        Coroutine buffCoroutine = StartCoroutine(ApplyBuff(buffData));
        activeBuffs[buffData.BuffType] = buffCoroutine;
    }

    [ClientRpc]
    private void RpcPlayBuffEffect(Constants.BuffType buffType, bool isActive)
    {
        if (effectSystem != null && buffType != Constants.BuffType.None)
        {
            effectSystem.PlayBuffEffect(buffType, isActive);
        }
    }

    private IEnumerator ApplyBuff(BuffData buffData)
    {
        ApplyBuffEffect(buffData);

        // ✅ 지속 피해(DoT) 실행
        if (buffData.tickDamage > 0)
        {
            StartCoroutine(TickDamage(buffData));
        }

        // ✅ 강제 이동 실행
        if (buffData.moveDirection != Vector3.zero)
        {
            RpcForcedMove(buffData);
        }

        // ✅ 부가 효과 실행
        RpcSideEffect(buffData);

        yield return new WaitForSeconds(buffData.duration);

        RemoveBuffEffect(buffData);

        // ✅ 클라이언트에서 버프 종료 이펙트 제거
        RpcPlayBuffEffect(buffData.BuffType, false);

        activeBuffs.Remove(buffData.BuffType);
    }

    private void ApplyBuffEffect(BuffData buffData)
    {
        float newBuffValue = buffData.moveSpeedModifier;
        Constants.BuffType buffType = buffData.BuffType;

        if (activeBuffValues.ContainsKey(buffType))
        {
            float existingBuffValue = activeBuffValues[buffType];

            // ✅ 더 강한 값이 적용되도록 보장
            if (Mathf.Abs(newBuffValue) > Mathf.Abs(existingBuffValue))
            {
                playerCharacter.MoveSpeed -= existingBuffValue; // 기존 값 제거
                playerCharacter.MoveSpeed += newBuffValue; // 새 값 적용
                activeBuffValues[buffType] = newBuffValue; // 업데이트
            }
        }
        else
        {
            playerCharacter.MoveSpeed += newBuffValue;
            activeBuffValues[buffType] = newBuffValue;
        }
    }

    private void RemoveBuffEffect(BuffData buffData)
    {
        Constants.BuffType buffType = buffData.BuffType;

        if (activeBuffValues.ContainsKey(buffType))
        {
            playerCharacter.MoveSpeed -= activeBuffValues[buffType]; // 버프 해제
            activeBuffValues.Remove(buffType);
        }
    }

    private IEnumerator TickDamage(BuffData buffData)
    {
        float elapsedTime = 0f;

        while (elapsedTime < buffData.duration)
        {
            yield return new WaitForSeconds(0.5f);

            if (playerCharacter != null)
            {
                playerCharacter.DecreaseHp(buffData.tickDamage); // ✅ 0.5초마다 지속 피해 적용
            }

            elapsedTime += 0.5f;
        }
    }
    
    [ClientRpc]
    private void RpcForcedMove(BuffData buffData)
    {
        StartCoroutine(ForcedMove(buffData));
    }

    private IEnumerator ForcedMove(BuffData buffData)
    {
        float elapsedTime = 0f;
        
        CharacterController characterController = playerCharacter.GetComponent<CharacterController>();
        while (elapsedTime < buffData.duration)
        {
            yield return new WaitForSeconds(0.016f);

            if (playerCharacter != null)
            {                
                Quaternion quaternion = playerCharacter.gameObject.transform.GetChild(2).rotation;
                Vector3 moveDirection = quaternion * buffData.moveDirection;
                moveDirection.y = 0f;
                characterController.Move(moveDirection * 0.1f); // ✅ 강제 이동 적용
            }

            elapsedTime += 0.016f;
        }
    }
    
    // 부가 효과 함수 구현
    [ClientRpc]
    private void RpcSideEffect(BuffData buffData)
    {
        switch (buffData.BuffType)
        {
            case Constants.BuffType.Charge:
                StartCoroutine(Charge(buffData));
                break;
            case Constants.BuffType.PowerBody:
                StartCoroutine(PowerBody(buffData));
                break;
        }
    }
    private IEnumerator Charge(BuffData buffData)
    {
        Debug.Log($"Charge: {buffData.duration}초 동안 부가 효과 적용");
        float elapsedTime = 0f;
        int tick = 0;
        while (elapsedTime < buffData.duration)
        {
            yield return new WaitForSeconds(0.016f);
            if (++tick % 5 == 0)
            {
                playerCharacter.CmdCertainAttack(playerCharacter.transform.position, 230, true); // ✅ 돌진 공격 실행
            }
            elapsedTime += 0.016f;
        }
    }
    private IEnumerator PowerBody(BuffData buffData)
    {
        Debug.Log($"PowerBody: {buffData.duration}초 동안 부가 효과과 적용");
        float elapsedTime = 0f;
        playerCharacter.KnockbackFactor -= 0.6f; // ✅ 넉백 감소

        while (elapsedTime < buffData.duration)
        {
            yield return new WaitForSeconds(0.5f);
            playerCharacter.DecreaseHp(buffData.tickDamage); // ✅ 0.5초마다 체력 회복
            elapsedTime += 0.5f;
        }

        playerCharacter.KnockbackFactor += 0.6f; // ✅ 넉백 감소 해제
    }
}
