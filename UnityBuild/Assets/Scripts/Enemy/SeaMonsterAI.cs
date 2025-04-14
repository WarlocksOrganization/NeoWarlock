using System;
using System.Collections;
using DataSystem;
using GameManagement;
using Mirror;
using Player;
using UnityEngine;

public partial class SeaMonsterAI : DragonAI
{
    [SerializeField] private Transform floatingTransform;
    
    private void Start()
    {
        transform.position = new Vector3(0, -40, 30);
    }

    public override void Init()
    {
        var gameRoomData = FindFirstObjectByType<GameRoomData>();
        if (gameRoomData != null && isServer)
        {
            gameRoomData.SetRoomType(Constants.RoomType.Raid);

            // ✅ 팀 설정을 Coroutine으로 분리
            StartCoroutine(DelaySetTeam());
        }

        int baseHp = 0;
        int bonusPerPlayer = 1000;
        int playerCount = FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None).Length;

        int newMaxHp = baseHp + bonusPerPlayer * playerCount;

        if (maxHp < newMaxHp)
        {
            maxHp = newMaxHp;
            curHp = maxHp;
        }
        
        StartCoroutine(StartLandingSequence());
    }
    
    public override IEnumerator StartLandingSequence()
    {
        SelectRandomTarget();
        
        animator.SetTrigger("isStarting");
        RpcPlayAnimation("isStarting");

        yield return new WaitForSeconds(3f); // 착지 애니메이션 시간
        SetCollider(true);
        RpcPlaySound(Constants.SoundType.SFX_MonsterStart);
        yield return new WaitForSeconds(2f); // 착지 애니메이션 시간
        isFlying = false;

        isLanded = true;
    }
    
    [Server]
    protected override void Die()
    {
        animator.SetTrigger("isDead");
        RpcPlayAnimation("isDead");

        
        var gameRoomData = FindFirstObjectByType<GameRoomData>();
        if (gameRoomData != null && isServer)
        {
            gameRoomData.SetRoomType(Constants.RoomType.Solo);

            RpcResetTeamsToClientLocal();
        }
        RpcHideHealthUI(); // 체력바 숨기기 호출
        RpcPlaySound(Constants.SoundType.SFX_MonsterDead);
        
        StartCoroutine(DelayGameOverCheckAfterDeath());
    }
    
    [ClientRpc]
    protected override void RpcPlaySound(Constants.SoundType soundType)
    {
        AudioManager.Instance.PlaySFX(soundType);
    }
    
    [ClientRpc]
    protected override void RpcPlaySound(Constants.SkillType soundType)
    {
        AudioManager.Instance.PlaySFX(soundType);
    }
    
    [ClientRpc]
    protected override void RpcShowFloatingDamage(int damage)
    {
        if (floatingDamageTextPrefab == null) return;

        GameObject instance = Instantiate(floatingDamageTextPrefab, floatingTransform.position, Quaternion.identity);
        instance.GetComponent<FloatingDamageText>().SetDamageText(damage);
    }
}
