using DataSystem;
using UnityEngine;

/// <summary>
/// 공격 스킬의 동작, 이펙트, 버프 등을 정의하는 설정 데이터
/// ScriptableObject를 활용하여 각 스킬마다 개별 설정 가능
/// </summary>
[CreateAssetMenu(fileName = "NewAttackConfig", menuName = "Attack/AttackConfig")]
public class AttackConfig : ScriptableObject
{
    // 공격 타입 (예: 투사체, 범위, 근접 등)
    public Constants.AttackType attackType;

    // 공격이 발동되기까지의 지연 시간 (예: 캐스팅 시간)
    public float attackDelay;

    // 공격 후 이동이 가능해지기까지의 시간 (후딜레이)
    public float recoveryTime;

    // 공격이 유지되는 시간 (예: 빔 공격 지속 시간)
    public float attackDuration;

    // 공격이 반복되는 간격 (예: 지속 데미지)
    public float attackInterval;

    // 공격 시 실행할 애니메이션 파라미터 이름
    public string animParameter;

    // 공격 이펙트가 생성될 위치 (로컬 좌표 기준)
    public Vector3 attackTrans;

    // 공격 프리팹 (예: 투사체 오브젝트)
    public GameObject Prefab;

    // 폭발 효과 프리팹 (예: 범위 공격 시 사용)
    public GameObject explosionEffectPrefab;

    // 추가 파티클 이펙트 프리팹
    public GameObject particlePrefab;
    public GameObject particlePrefab2;

    // 스킬의 타입 (사운드, 이펙트 연동 등에 사용)
    public Constants.SkillType skillType;

    // 공격 시 적용할 버프 (예: 슬로우, 출혈 등)
    public BuffData appliedBuff;
}

