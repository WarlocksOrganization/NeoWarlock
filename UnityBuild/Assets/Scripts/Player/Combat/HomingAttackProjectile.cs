using System.Collections;
using System.Collections.Generic;
using DataSystem;
using Interfaces;
using Mirror;
using Player.Combat;
using UnityEngine;

namespace Player
{
    public class HomingAttackProjectile : AttackProjectile
    {
        private Transform target;

        private void FindTarget()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius * 3, layerMask);
            float closestDistance = float.MaxValue;

            foreach (Collider col in hitColliders)
            {
                IDamagable player = col.GetComponent<IDamagable>();
                if (player != null && ((MonoBehaviour)player).gameObject != owner)
                {
                    float distance = Vector3.Distance(transform.position, ((MonoBehaviour)player).transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        target = ((MonoBehaviour)player).transform;
                    }
                }
            }
        }

        protected override IEnumerator MoveProjectile()
        {
            while (true)
            {
                FindTarget();
                if (target != null)
                {
                    Vector3 direction = (target.position - transform.position).normalized;
                    direction.y = 0; // Y축 이동 방지 (수평 이동 유지)
                    moveDirection = direction;

                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up); // Y축 회전 고정
                }

                rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);

                if (rb.transform.position.y <= 0)
                {
                    transform.position = new Vector3(transform.position.x, 0, transform.position.z);
                    Explode();
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }
        }
    }
}
