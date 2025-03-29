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
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, mouseTargetLayer))
            {
                Vector3 mousePosition = hitInfo.point;
                Vector3 playerPosition = playerModel.transform.position;

                Vector3 targetPosition = Vector3.Lerp(playerPosition, mousePosition, 0.5f);
                targetPosition.y = playerPosition.y;

                Vector3 offset = targetPosition - playerPosition;

                // 🔹 아래쪽(-Z)에 있을 경우 최대 거리 증가
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

                // 🔹 위치 보간 이동
                CinemachineCameraTarget.transform.position = Vector3.Lerp(
                    CinemachineCameraTarget.transform.position,
                    limitedTargetPosition,
                    Time.deltaTime * cameraOffset
                );

                // 🔹 기울기 조정 (X축 회전)
                float zOffset = offset.z;
                float tiltFactor = Mathf.Clamp01(-zOffset / 10f); // -10 이하에서 최대
                float targetTilt = Mathf.Lerp(baseTilt, maxTilt, tiltFactor);

                Quaternion targetRotation = Quaternion.Euler(targetTilt, 0f, 0f); // X축만 회전
                CinemachineCameraTarget.transform.rotation = Quaternion.Slerp(
                    CinemachineCameraTarget.transform.rotation,
                    targetRotation,
                    Time.deltaTime * tiltLerpSpeed
                );
            }
        }
    }
}