using DataSystem.Database;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Player.Combat
{
    public class PlayerProjector : MonoBehaviour
    {
        [SerializeField] private DecalProjector decalProjector; // 현재 표시 중인 Decal Projector
        [SerializeField] private DecalProjector rangeDecalProjector; // 범위 표시용 Decal Projector

        [SerializeField] private Material arrowMaterial; // Projectile용 Material
        [SerializeField] private Material circleMaterial; // Area용 Material
        [SerializeField] private Material circleborderMaterial; // Range용 Material

        private LayerMask mouseTargetLayer;
        private IAttack currentAttack;
        private Database.AttackData attackData;
    
        private Vector3 aimPosition;
        private Transform fireTransform;

        private Vector3 startPosition;
        private Vector3 midPoint;
        private Vector3 endPosition;
        private Vector3 direction;
        private float attackRange;
        private float distance;

        void Start()
        {
            rangeDecalProjector.material = circleborderMaterial;
        }
    
        void Update()
        {
            if (currentAttack == null)
            {
                return;
            }
            UpdateDecalProjector();
        }

        public void CloseProjectile()
        {
            decalProjector.gameObject.SetActive(false);
            rangeDecalProjector.gameObject.SetActive(false);
        }

        public void SetDecalProjector(IAttack attack, LayerMask targetLayer, Transform fireTransform)
        {
            currentAttack = attack;
            mouseTargetLayer = targetLayer;
            this.fireTransform = fireTransform;
            decalProjector.gameObject.SetActive(true);
            if (currentAttack is ProjectileSkyAttack|| currentAttack is PointAttack || currentAttack is AreaAttack)
            {
                rangeDecalProjector.gameObject.SetActive(true);
            }
            else
            {
                rangeDecalProjector.gameObject.SetActive(false);
            }
            if (currentAttack == null)
            {
                CloseProjectile();
                return;
            }

            attackData = currentAttack.GetAttackData();
        
            if (currentAttack is ProjectileSkyAttack || currentAttack is PointAttack || currentAttack is AreaAttack)
            {
                attackRange = attackData.Range;
                
                decalProjector.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                decalProjector.material = circleMaterial;
                decalProjector.size = new Vector3(attackData.Radius*2, attackData.Radius*2, 1f);
                
                rangeDecalProjector.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                rangeDecalProjector.size = new Vector3(attackData.Range*2 + attackData.Radius*2, attackData.Range*2 + attackData.Radius*2, 1f);
            }
            else if (currentAttack is ProjectileAttack projectileAttack)
            {
                distance = projectileAttack.GetAttackData().Range;
                decalProjector.material = arrowMaterial;

                // Projector 크기 설정 (Y축 길이를 distance에 맞춰 동적으로 변경)
                decalProjector.size = new Vector3(1f, distance, 1f);

                if (arrowMaterial != null)
                {
                    arrowMaterial.mainTextureScale = new Vector2(1, distance / 10f);
                }
            }
        }
    
        private void UpdateDecalProjector()
        {
            // 마우스 위치 업데이트
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, mouseTargetLayer))
            {
                aimPosition = hitInfo.point;
            }

            if (currentAttack is ProjectileSkyAttack || currentAttack is PointAttack || currentAttack is AreaAttack)
            {
                startPosition = transform.position;
                
                rangeDecalProjector.transform.position = startPosition + Vector3.up * 0.1f;
                // ✅ 현재 마우스 위치와 플레이어(또는 fireTransform) 위치 간 거리 계산
                distance = Vector3.Distance(startPosition, aimPosition);
                
                // ✅ 거리가 공격 가능 거리(attackRange)보다 크면 제한
                if (distance > attackRange)
                {
                    Vector3 direction = (aimPosition - startPosition).normalized;
                    aimPosition = startPosition + direction * attackRange; // 최대 거리로 위치 고정
                }

                decalProjector.transform.position = aimPosition + Vector3.up * 0.1f;
            }
            else if (currentAttack is ProjectileAttack projectileAttack)
            {
                startPosition = fireTransform.position;
                
                endPosition = startPosition + (aimPosition - startPosition).normalized * projectileAttack.GetAttackData().Range;

                midPoint = (startPosition + endPosition) / 2;
                decalProjector.transform.position = new Vector3(midPoint.x, 0.5f, midPoint.z);

                direction = (endPosition - startPosition).normalized;
                direction.y = 0;
                decalProjector.transform.rotation = Quaternion.LookRotation(Vector3.down, direction);
                
            }
        }
    }
}
