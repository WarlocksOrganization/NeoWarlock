using System.Linq;
using Cinemachine;
using UnityEngine;

namespace Player
{
    public partial class PlayerCharacter
    {
        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float cameraOffset = 5f;
        public float maxCameraDistance = 5f;
        
        [SerializeField] private float baseTilt = 45f;
        [SerializeField] private float maxTilt = 60f;
        [SerializeField] private float tiltLerpSpeed = 5f;

        [SerializeField] private GameObject playerModel;

        private void UpdateCameraTarget()
        {
            if (cameraTargetGroupTransform == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, mouseTargetLayer))
            {
                Vector3 mousePosition = hitInfo.point;
                Vector3 playerPosition = playerModel.transform.position;

                Vector3 targetPosition = Vector3.Lerp(playerPosition, mousePosition, 0.5f);
                targetPosition.y = playerPosition.y;

                Vector3 offset = targetPosition - playerPosition;

                float dynamicMaxDistance = maxCameraDistance;
                if (offset.z < 0)
                {
                    dynamicMaxDistance *= 3f;
                }
                else
                {
                    dynamicMaxDistance = 0;
                }

                if (offset.magnitude > dynamicMaxDistance)
                {
                    offset = offset.normalized * dynamicMaxDistance;
                }

                Vector3 limitedTargetPosition = playerPosition + offset;

                // ✅ TargetGroup의 위치 보간 이동
                CinemachineCameraTarget.transform.position = Vector3.Lerp(
                    CinemachineCameraTarget.transform.position,
                    limitedTargetPosition,
                    Time.deltaTime * cameraOffset
                );

                // ✅ TargetGroup의 X축 회전 조절
                float zOffset = offset.z;
                float tiltFactor = Mathf.Clamp01(-zOffset / 10f);
                float targetTilt = Mathf.Lerp(baseTilt, maxTilt, tiltFactor);

                Quaternion targetRotation = Quaternion.Euler(targetTilt, 0f, 0f);
                cameraTargetGroupTransform.rotation = Quaternion.Slerp(
                    cameraTargetGroupTransform.rotation,
                    targetRotation,
                    Time.deltaTime * tiltLerpSpeed
                );
            }
        }
        
        public void AddTargetToCamera(Transform target)
        {
            var group = FindFirstObjectByType<Cinemachine.CinemachineTargetGroup>();
            if (group == null) return;

            var targets = group.m_Targets.ToList();
            if (targets.Any(t => t.target == target)) return;

            targets.Add(new CinemachineTargetGroup.Target
            {
                target = target,
                weight = 0.5f,
                radius = 3f
            });

            group.m_Targets = targets.ToArray();
        }

    }
}