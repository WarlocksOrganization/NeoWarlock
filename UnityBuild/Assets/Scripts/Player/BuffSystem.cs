using System.Collections;
using System.Collections.Generic;
using DataSystem;
using Mirror;
using Player;
using Player.Combat;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class BuffSystem : NetworkBehaviour
{
    private Dictionary<Constants.BuffType, Coroutine> activeBuffs = new Dictionary<Constants.BuffType, Coroutine>();
    private Dictionary<Constants.BuffType, Coroutine> activeTickDamage = new Dictionary<Constants.BuffType, Coroutine>();
    private Dictionary<string, float> activeBuffValues = new Dictionary<string, float>();
    
    private PlayerCharacter playerCharacter;
    private EffectSystem effectSystem;

    private void Start()
    {
        playerCharacter = GetComponent<PlayerCharacter>();
        effectSystem = GetComponent<EffectSystem>();
    }

    [Command(requiresAuthority = false)]
    public void CmdApplyBuff(BuffData buffData, int attackPlayerId, int attackskillid)
    {
        if (activeBuffs.ContainsKey(buffData.BuffType))
        { 
            StopCoroutine(activeBuffs[buffData.BuffType]);
            RemoveBuffEffect(buffData.BuffType);
            activeBuffs.Remove(buffData.BuffType); 
        }

        RpcPlayBuffEffect(buffData.BuffType, true);

        Coroutine buffCoroutine = StartCoroutine(ApplyBuff(buffData, attackPlayerId, attackskillid));
        activeBuffs[buffData.BuffType] = buffCoroutine;
    }

    private IEnumerator ApplyBuff(BuffData buffData, int attackPlayerId, int attackskillid)
    {
        ApplyBuffEffect(buffData);

        Coroutine tickDamageCoroutine = null;
        if (buffData.tickDamage != 0)
        {
            tickDamageCoroutine = StartCoroutine(TickDamage(buffData, attackPlayerId, attackskillid));
            activeTickDamage[buffData.BuffType] = tickDamageCoroutine;
        }

        if (buffData.moveDirection != Vector3.zero)
        {
            RpcForcedMove(buffData);
        }
        

        RpcSideEffect(buffData);

        yield return new WaitForSeconds(buffData.duration);

        RemoveBuffEffect(buffData.BuffType);
        RpcPlayBuffEffect(buffData.BuffType, false);
        activeBuffs.Remove(buffData.BuffType);

        if (tickDamageCoroutine != null)
        {
            StopCoroutine(tickDamageCoroutine);
            activeTickDamage.Remove(buffData.BuffType);
        }
    }

    private void ApplyBuffEffect(BuffData buffData)
    {
        Constants.BuffType buffType = buffData.BuffType;

        if (buffData.defenseModifier != 0)
        {
            playerCharacter.defense += (int)buffData.defenseModifier;
            activeBuffValues[BuffKey(buffType, "def")] = buffData.defenseModifier;
        }

        if (buffData.knonkbackModifier != 0)
        {
            playerCharacter.KnockbackFactor += buffData.knonkbackModifier;
            activeBuffValues[BuffKey(buffType, "knock")] = buffData.knonkbackModifier;
        }

        if (buffData.moveSpeedModifier != 0)
        {
            playerCharacter.MoveSpeed += buffData.moveSpeedModifier;
            activeBuffValues[BuffKey(buffType, "move")] = buffData.moveSpeedModifier;
        }

        if (buffData.attackDamageModifier != 0f)
        {
            playerCharacter.AttackPower += buffData.attackDamageModifier;
            activeBuffValues[BuffKey(buffType, "atk")] = buffData.attackDamageModifier;
        }
        playerCharacter.NotifyStatChanged();
    }

    private void RemoveBuffEffect(Constants.BuffType buffType)
    {
        var suffixes = new[] { "def", "knock", "move", "atk" };

        foreach (var suffix in suffixes)
        {
            string key = BuffKey(buffType, suffix);
            if (activeBuffValues.TryGetValue(key, out float value))
            {
                switch (suffix)
                {
                    case "def":
                        playerCharacter.defense -= (int)value;
                        break;
                    case "knock":
                        playerCharacter.KnockbackFactor -= value;
                        break;
                    case "move":
                        playerCharacter.MoveSpeed -= value;
                        break;
                    case "atk":
                        playerCharacter.AttackPower -= value;
                        break;
                }
                activeBuffValues.Remove(key);
                playerCharacter.NotifyStatChanged();
            }
        }
    }

    private IEnumerator TickDamage(BuffData buffData, int attackPlayerId, int attackskillid)
    {
        float elapsedTime = 0f;

        while (elapsedTime < buffData.duration)
        {
            yield return new WaitForSeconds(0.5f);

            if (playerCharacter != null)
            {
                playerCharacter.DecreaseHp(buffData.tickDamage, attackPlayerId, attackskillid);
            }

            elapsedTime += 0.5f;
        }

        activeTickDamage.Remove(buffData.BuffType);
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

        if (characterController == null)
        {
            Debug.LogError("[BuffSystem] CharacterController가 없음! 이동 불가능!");
            yield break;
        }

        while (elapsedTime < buffData.duration)
        {
            yield return null; // ✅ 매 프레임 실행

            if (playerCharacter != null)
            {
                Quaternion quaternion = playerCharacter.gameObject.transform.GetChild(2).rotation;
                Vector3 moveDirection = quaternion * buffData.moveDirection;
                moveDirection.y = 0f;
                
                characterController.Move(moveDirection * Time.deltaTime);
            }

            elapsedTime += Time.deltaTime; // ✅ Time.deltaTime을 사용하여 정확한 시간 측정
        }
    }


    [Command(requiresAuthority = false)]
    public void CmdClearAllBuffs()
    {
        RpcClearAllBuffs();
    }

    [ClientRpc]
    private void RpcClearAllBuffs()
    {
        ClearAllBuffs();
    }

    private void ClearAllBuffs()
    {
        foreach (var buffKey in new List<string>(activeBuffValues.Keys))
        {
            if (buffKey.EndsWith("_atk"))
            {
                playerCharacter.AttackPower -= activeBuffValues[buffKey];
            }
            else if (buffKey.EndsWith("_move"))
            {
                playerCharacter.MoveSpeed -= activeBuffValues[buffKey];
            }
            else if (buffKey.EndsWith("_def"))
            {
                playerCharacter.defense -= (int)activeBuffValues[buffKey];
            }
            else if (buffKey.EndsWith("_knock"))
            {
                playerCharacter.KnockbackFactor -= activeBuffValues[buffKey];
            }
        }
        activeBuffValues.Clear();

        foreach (var buffType in new List<Constants.BuffType>(activeBuffs.Keys))
        {
            StopCoroutine(activeBuffs[buffType]);
        }
        activeBuffs.Clear();

        foreach (var buffType in new List<Constants.BuffType>(activeTickDamage.Keys))
        {
            StopCoroutine(activeTickDamage[buffType]);
        }
        activeTickDamage.Clear();
    }
    
    public void ServerApplyBuff(BuffData buffData, int attackPlayerId, int attackskillid)
    {
        if (!isServer)
        {
            return;
        }

        if (activeBuffs.ContainsKey(buffData.BuffType))
        {
            StopCoroutine(activeBuffs[buffData.BuffType]);
            RemoveBuffEffect(buffData.BuffType); 
            activeBuffs.Remove(buffData.BuffType);
        }

        // ✅ 클라이언트에서 버프 이펙트 실행
        RpcPlayBuffEffect(buffData.BuffType, true);

        Coroutine buffCoroutine = StartCoroutine(ApplyBuff(buffData, attackPlayerId, attackskillid));
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

    [ClientRpc]
    private void RpcSideEffect(BuffData buffData)
    {
        switch (buffData.BuffType)
        {
            case Constants.BuffType.Charge:
            case Constants.BuffType.PowerCharge:
                StartCoroutine(Charge(buffData));
                break;
        }
    }

    private IEnumerator Charge(BuffData buffData)
    {
        float elapsedTime = 0f;
        int tick = 0;

        while (elapsedTime < buffData.duration)
        {
            yield return new WaitForSeconds(0.016f);
            if (++tick % 5 == 0)
            {
                playerCharacter.CmdCertainAttack(playerCharacter.transform.position, 230, true);
            }
            elapsedTime += 0.016f;
        }
    }
    
    private string BuffKey(Constants.BuffType type, string stat)
    {
        return $"{type}_{stat}";
    }
}
