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

        [SerializeField] private float fieldOfView = 90f; // 시야각
        [SerializeField] private float rotationSpeed = 0.01f; // 회전 속도 (deg/sec)

        private void FindTarget()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius * 3, layerMask);
            float closestDistance = float.MaxValue;
            Transform newTarget = null;

            Vector3 forward = transform.forward;

            foreach (Collider col in hitColliders)
            {
                IDamagable player = col.GetComponent<IDamagable>();
                if (player != null && ((MonoBehaviour)player).gameObject != owner)
                {
                    Transform potentialTarget = ((MonoBehaviour)player).transform;
                    Vector3 dirToTarget = (potentialTarget.position - transform.position).normalized;
                    dirToTarget.y = 0;

                    float angle = Vector3.Angle(forward, dirToTarget);
                    if (angle <= fieldOfView * 0.5f)
                    {
                        float distance = Vector3.Distance(transform.position, potentialTarget.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            newTarget = potentialTarget;
                        }
                    }
                }
            }

            if (newTarget != null)
            {
                target = newTarget;
            }
        }

        protected override IEnumerator MoveProjectile()
        {
            while (true)
            {
                FindTarget();

                if (target != null)
                {
                    Vector3 dirToTarget = (target.position - transform.position).normalized;
                    dirToTarget.y = 0;

                    Quaternion targetRotation = Quaternion.LookRotation(dirToTarget);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

                    moveDirection = transform.forward.normalized;
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
