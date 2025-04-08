using System.Collections;
using System.Collections.Generic;
using DataSystem;
using DataSystem.Database;
using Mirror;
using Player;
using Player.Combat;
using UI;
using UnityEngine;

public class BuffSystem : NetworkBehaviour
{
    private Dictionary<string, Coroutine> activeBuffs = new();             // buffName → Coroutine
    private Dictionary<string, Coroutine> activeTickDamage = new();       // buffName → Coroutine
    private Dictionary<string, Dictionary<string, float>> activeBuffValues = new(); // buffName → (statName → value)

    private PlayerCharacter playerCharacter;
    private EffectSystem effectSystem;

    private void Start()
    {
        playerCharacter = GetComponent<PlayerCharacter>();
        effectSystem = GetComponent<EffectSystem>();
    }

    [Command(requiresAuthority = false)]
    public void CmdApplyBuff(BuffData buffData, int attackPlayerId, int attackSkillId)
    {
        if (!isServer) return;
        ApplyBuffServerSide(buffData, attackPlayerId, attackSkillId);
    }

    public void ServerApplyBuff(BuffData buffData, int attackPlayerId, int attackSkillId)
    {
        if (!isServer) return;
        ApplyBuffServerSide(buffData, attackPlayerId, attackSkillId);
    }

    private void ApplyBuffServerSide(BuffData buffData, int attackPlayerId, int attackSkillId)
    {
        if (activeBuffs.ContainsKey(buffData.buffName))
        {
            StopCoroutine(activeBuffs[buffData.buffName]);
            RemoveBuffEffect(buffData.buffName);
            activeBuffs.Remove(buffData.buffName);
        }

        RpcPlayBuffEffect(buffData.BuffType, true);
        Coroutine buffCoroutine = StartCoroutine(ApplyBuff(buffData, attackPlayerId, attackSkillId));
        activeBuffs[buffData.buffName] = buffCoroutine;
    }

    private IEnumerator ApplyBuff(BuffData buffData, int attackPlayerId, int attackSkillId)
    {
        ApplyBuffEffect(buffData);

        Coroutine tickCoroutine = null;
        if (buffData.tickDamage != 0)
        {
            tickCoroutine = StartCoroutine(TickDamage(buffData, attackPlayerId, attackSkillId));
            activeTickDamage[buffData.buffName] = tickCoroutine;
        }

        if (buffData.moveDirection != Vector3.zero)
            RpcForcedMove(buffData.moveDirection, buffData.duration);

        if (isServer && (buffData.BuffType == Constants.BuffType.Charge || buffData.BuffType == Constants.BuffType.PowerCharge))
            ApplyCharge(attackSkillId);

        RpcShowBuffUI(buffData.buffName, buffData.duration);

        yield return new WaitForSeconds(buffData.duration);

        RemoveBuffEffect(buffData.buffName);
        RpcPlayBuffEffect(buffData.BuffType, false);
        activeBuffs.Remove(buffData.buffName);

        if (tickCoroutine != null)
        {
            StopCoroutine(tickCoroutine);
            activeTickDamage.Remove(buffData.buffName);
        }
    }

    [ClientRpc]
    private void RpcShowBuffUI(string buffName, float duration)
    {
        if (!playerCharacter.isOwned) return;

        var ui = FindFirstObjectByType<PlayerCharacterUI>();
        if (ui == null) return;

        if (Database.buffDictionary.TryGetValue(buffName, out var buffData))
        {
            ui.ShowBuff(buffName, buffData.buffIcon, duration, buffData.description);
        }
        else
        {
            Debug.LogWarning($"[BuffSystem] buffName({buffName})에 해당하는 데이터가 Database에 없습니다.");
        }
    }

    private void ApplyBuffEffect(BuffData buffData)
    {
        var statMap = new Dictionary<string, float>();

        if (buffData.defenseModifier != 0)
        {
            playerCharacter.defense += (int)buffData.defenseModifier;
            statMap["def"] = buffData.defenseModifier;
        }

        if (buffData.knonkbackModifier != 0)
        {
            playerCharacter.KnockbackFactor += buffData.knonkbackModifier;
            statMap["knock"] = buffData.knonkbackModifier;
        }

        if (buffData.moveSpeedModifier != 0)
        {
            playerCharacter.MoveSpeed += buffData.moveSpeedModifier;
            statMap["move"] = buffData.moveSpeedModifier;
        }

        if (buffData.attackDamageModifier != 0)
        {
            playerCharacter.AttackPower += buffData.attackDamageModifier;
            statMap["atk"] = buffData.attackDamageModifier;
        }

        if (statMap.Count > 0)
        {
            activeBuffValues[buffData.buffName] = statMap;
            playerCharacter.NotifyStatChanged();
        }
    }

    private void RemoveBuffEffect(string buffName)
    {
        if (!activeBuffValues.TryGetValue(buffName, out var statMap)) return;

        foreach (var kv in statMap)
        {
            switch (kv.Key)
            {
                case "def":
                    playerCharacter.defense -= (int)kv.Value;
                    break;
                case "knock":
                    playerCharacter.KnockbackFactor -= kv.Value;
                    break;
                case "move":
                    playerCharacter.MoveSpeed -= kv.Value;
                    break;
                case "atk":
                    playerCharacter.AttackPower -= kv.Value;
                    break;
            }
        }

        activeBuffValues.Remove(buffName);
        playerCharacter.NotifyStatChanged();
    }

    private IEnumerator TickDamage(BuffData buffData, int attackPlayerId, int attackSkillId)
    {
        float elapsed = 0f;
        while (elapsed < buffData.duration)
        {
            yield return new WaitForSeconds(1f);
            playerCharacter.DecreaseHp(buffData.tickDamage, attackPlayerId, attackSkillId);
            elapsed += 1f;
        }
    }

    [ClientRpc]
    private void RpcForcedMove(Vector3 moveDirection, float duration)
    {
        StartCoroutine(ForcedMove(moveDirection, duration));
    }

    private IEnumerator ForcedMove(Vector3 moveDirection, float duration)
    {
        float elapsed = 0f;
        float syncTimer = 0f;
        CharacterController cc = playerCharacter.GetComponent<CharacterController>();

        if (cc == null)
        {
            Debug.LogError("[BuffSystem] CharacterController가 없습니다.");
            yield break;
        }

        while (elapsed < duration)
        {
            yield return null;
            Quaternion rot = playerCharacter.transform.GetChild(2).rotation;
            Vector3 moveDir = rot * moveDirection;
            moveDir.y = 0f;

            cc.Move(moveDir * Time.deltaTime);

            syncTimer += Time.deltaTime;
            if (syncTimer >= 0.1f)
            {
                playerCharacter.CmdUpdatePosition(playerCharacter.transform.position);
                syncTimer = 0f;
            }

            elapsed += Time.deltaTime;
        }
    }

    [ClientRpc]
    private void RpcPlayBuffEffect(Constants.BuffType buffType, bool isActive)
    {
        if (effectSystem != null && buffType != Constants.BuffType.None)
        {
            effectSystem.PlayBuffEffect(buffType, isActive);
        }
    }

    private void ApplyCharge(int skillId)
    {
        GameObject holder = new("ChargeAttackZone");
        holder.transform.SetParent(transform);
        holder.transform.localPosition = Vector3.zero;

        var zone = holder.AddComponent<ChargeAttackZone>();
        zone.Initialize(gameObject, playerCharacter.playerId, skillId);

        if (isServer)
            zone.StartAttack();
    }

    [Command(requiresAuthority = false)]
    public void CmdClearAllBuffs()
    {
        if (!isServer) return;
        
        RpcClearAllBuffEffects();
        RpcClearAllBuffUI();
    }

    public void ServerClearAllBuffs()
    {
        if (!isServer) return;

        // 실제 제거 로직
        ClearAllBuffs();

        // 클라이언트에 이펙트 종료 + UI 제거 명령
        RpcClearAllBuffEffects();
        RpcClearAllBuffUI();
    }
    
    [ClientRpc]
    private void RpcClearAllBuffEffects()
    {
        foreach (var buffName in activeBuffValues.Keys)
        {
            if (Database.buffDictionary.TryGetValue(buffName, out var buffData))
            {
                RpcPlayBuffEffect(buffData.BuffType, false);
            }
        }
    }


    private void ClearAllBuffs()
    {
        // 이펙트와 스탯 제거
        var buffNames = new List<string>(activeBuffValues.Keys);
        foreach (var buffName in buffNames)
        {
            if (Database.buffDictionary.TryGetValue(buffName, out var buffData))
            {
                RpcPlayBuffEffect(buffData.BuffType, false);  // 이펙트 끄기
            }
            RemoveBuffEffect(buffName);
        }

        activeBuffValues.Clear();

        foreach (var buff in activeBuffs.Values)
            StopCoroutine(buff);
        activeBuffs.Clear();

        foreach (var tick in activeTickDamage.Values)
            StopCoroutine(tick);
        activeTickDamage.Clear();

        // UI에서 모든 버프 제거 요청
        RpcClearAllBuffUI();
    }
    
    [ClientRpc]
    private void RpcClearAllBuffUI()
    {
        if (!playerCharacter.isOwned) return;

        var ui = FindFirstObjectByType<PlayerCharacterUI>();
        if (ui == null) return;

        ui.ClearAllBuffs(); // 이 함수는 모든 버프 UI를 제거하도록 구현해야 함
    }

}
